using UnityEngine;
using Unity.MLAgents;

public class TrainingArea : MonoBehaviour
{
    [Header("Training Area Configuration")]
    [SerializeField] private Transform agentSpawnPoint;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private ParkourAgent agent;
    
    [Header("Platform Generation")]
    [SerializeField] private bool generatePlatformsOnStart = true;
    [SerializeField] private int platformCount = 20;
    [SerializeField] private float platformSpacing = 15f;
    [SerializeField] private Vector3 platformSize = new Vector3(24f, 10f, 6f);
    [SerializeField] private float[] platformHeights;
    
    [Header("Randomization (keeps jumps feasible)")]
    [Tooltip("Randomize platform gaps and sizes each episode for better generalization")]
    [SerializeField] private bool randomizePlatforms = true;
    [Tooltip("Gap between platforms will vary between [min, max] units (edge-to-edge).")]
    [SerializeField] private Vector2 gapRange = new Vector2(2.5f, 4.5f);
    [Tooltip("Platform width will vary between [min, max] units (doubled: was 10-14, now 20-28)")]
    [SerializeField] private Vector2 platformWidthRange = new Vector2(20f, 28f);
    [Tooltip("Randomize platform heights (vertical position)")]
    [SerializeField] private bool randomizeHeights = true;
    [Tooltip("Max height difference between consecutive platforms (units).")]
    [SerializeField] private Vector2 heightChangeRange = new Vector2(-0.6f, 1.2f);
    [Tooltip("Absolute min/max heights to keep platforms within reasonable bounds")]
    [SerializeField] private Vector2 absoluteHeightRange = new Vector2(-0.5f, 5f);
    [Tooltip("Random seed for reproducible training (0 = random each time)")]
    [SerializeField] private int randomSeed = 0;
    
    [Header("Target Position")]
    [Tooltip("Offset from end of last platform to target position (in units). Target will be positioned at: lastPlatformEndX + targetOffset")]
    [SerializeField] private float targetOffset = 5f;
    
    private GameObject platformsContainer;
    private float lastPlatformEndX = 0f;
    
    void Start()
    {
        
        if (agentSpawnPoint == null)
        {
            Debug.LogError($"TrainingArea '{name}': agentSpawnPoint not assigned!");
        }
        
        if (targetPosition == null)
        {
            Debug.LogError($"TrainingArea '{name}': targetPosition not assigned!");
        }
        
        if (agent == null)
        {
            Debug.LogError($"TrainingArea '{name}': agent not assigned!");
        }
        else
        {

            agent.GetType().GetField("trainingArea", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance)?.SetValue(agent, this);
        }
        
        if (generatePlatformsOnStart)
        {
            GeneratePlatforms();
        }
    }
    
    void GeneratePlatforms()
    {

        platformsContainer = new GameObject("Platforms");
        platformsContainer.transform.SetParent(transform);
        platformsContainer.transform.localPosition = Vector3.zero;
        
        if (randomSeed > 0)
        {
            Random.InitState(randomSeed);
        }
        
        if (platformHeights == null || platformHeights.Length == 0)
        {
            platformHeights = new float[platformCount];
            
            platformHeights[0] = Mathf.Clamp(0f, absoluteHeightRange.x, absoluteHeightRange.y);
            
            for (int i = 1; i < platformCount; i++)
            {
                if (randomizeHeights && randomizePlatforms)
                {

                    float heightChange = Random.Range(heightChangeRange.x, heightChangeRange.y);
                    float newHeight = platformHeights[i - 1] + heightChange;
                    
                    platformHeights[i] = Mathf.Clamp(newHeight, absoluteHeightRange.x, absoluteHeightRange.y);
                }
                else
                {

                    platformHeights[i] = platformHeights[0];
                }
            }
        }
        
        float rightEdge = 0f;
        float previousRightEdge = 0f;
        
        for (int i = 0; i < platformCount && i < platformHeights.Length; i++)
        {

            float baseWidth = randomizePlatforms 
                ? Random.Range(platformWidthRange.x, platformWidthRange.y) 
                : platformSize.x;
            
            bool makeLongPlatform = Random.value < 0.8f;
            float platformWidth = makeLongPlatform ? baseWidth * 3f : baseWidth;
            
            float platformCenterX;
            float gap = 0f;
            
            if (i == 0)
            {

                platformCenterX = 0f;
                rightEdge = platformWidth / 2f;
            }
            else
            {

                gap = randomizePlatforms 
                    ? Random.Range(gapRange.x, gapRange.y) 
                    : gapRange.x;
                
                float leftEdge = previousRightEdge + gap;
                
                platformCenterX = leftEdge + (platformWidth / 2f);
                
                rightEdge = platformCenterX + (platformWidth / 2f);
                
                float actualGap = leftEdge - previousRightEdge;
                Debug.Log($"[TrainingArea] Platform {i}: gap={gap:F2}, actualGap={actualGap:F2}, " +
                    $"leftEdge={leftEdge:F2}, previousRightEdge={previousRightEdge:F2}, " +
                    $"platformWidth={platformWidth:F2}, gapRange=[{gapRange.x:F2}, {gapRange.y:F2}]");
                
                if (Mathf.Abs(actualGap - gap) > 0.01f)
                {
                    Debug.LogError($"[TrainingArea] Gap calculation mismatch! Expected {gap:F2}, got {actualGap:F2}");
                }
                
                if (gap > 2.5f)
                {
                    Debug.LogWarning($"[TrainingArea] WARNING: Gap {gap:F2} is larger than safe range! Platform {i}");
                }
            }
            
            previousRightEdge = rightEdge;
            
            float platformHeight = platformSize.y;
            float platformTopY = platformHeights[i];
            float platformCenterY = platformTopY - (platformHeight / 2f);
            
            Vector3 localPosition = new Vector3(platformCenterX, platformCenterY, 0f);
            Vector3 size = new Vector3(platformWidth, platformHeight, platformSize.z);
            CreatePlatform(i, localPosition, size);
            
            if (i == platformCount - 1)
            {
                lastPlatformEndX = rightEdge;
            }
            
            #if UNITY_EDITOR
            if (i > 0 && randomizePlatforms && randomizeHeights)
            {
                float heightDiff = platformHeights[i] - platformHeights[i - 1];
                float jumpDistance = gap + 1f;
                
                if (!IsJumpFeasible(jumpDistance, heightDiff))
                {
                    Debug.LogWarning($"Platform {i-1} -> {i}: Difficult jump! Gap: {gap:F1}u, Height diff: {heightDiff:F1}u");
                }
            }
            #endif
        }
        
        UpdateTargetPosition();
    }
    
    void CreatePlatform(int index, Vector3 localPosition, Vector3 size)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = $"Platform_{index}";
        platform.transform.SetParent(platformsContainer.transform);
        platform.transform.localPosition = localPosition;
        platform.transform.localScale = size;
        
        platform.layer = LayerMask.NameToLayer("Default");
    }
    
    bool IsJumpFeasible(float horizontalDist, float heightDiff)
    {

        float jumpForce = 8f;
        float gravity = -20f;
        float moveSpeed = 6f;
        float jumpForwardBoost = 10f;
        
        float maxJumpHeight = (jumpForce * jumpForce) / (2f * Mathf.Abs(gravity));
        
        if (heightDiff > 0 && heightDiff > maxJumpHeight * 0.9f)
        {
            return false;
        }
        
        float a = 0.5f * gravity;
        float b = jumpForce;
        float c = -heightDiff;
        
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {

            return false;
        }
        
        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        float timeToTarget = Mathf.Max(t1, t2);
        
        if (timeToTarget <= 0)
        {
            return false;
        }
        
        float effectiveHorizontalSpeed = moveSpeed + jumpForwardBoost;
        float maxHorizontalDist = effectiveHorizontalSpeed * timeToTarget;
        
        return horizontalDist <= maxHorizontalDist * 1.8f;
    }
    
    public Vector3 GetAgentSpawnPosition()
    {
        if (agentSpawnPoint != null)
        {
            return agentSpawnPoint.position;
        }
        
        return transform.position + new Vector3(0, 1.25f, 0);
    }
    
    public Transform GetTargetTransform()
    {
        return targetPosition;
    }
    
    public float GetTargetXPosition()
    {
        return lastPlatformEndX + targetOffset;
    }
    
    void UpdateTargetPosition()
    {
        if (targetPosition != null)
        {
            float targetX = GetTargetXPosition();

            float spawnY = agentSpawnPoint != null ? agentSpawnPoint.position.y : 1.25f;
            
            targetPosition.localPosition = new Vector3(targetX, spawnY, 0f);
            
            Debug.Log($"[TrainingArea] Target position updated: X={targetX:F1} (last platform end: {lastPlatformEndX:F1} + offset: {targetOffset:F1})");
        }
    }
    
    public void ResetArea()
    {

        if (randomizePlatforms && platformsContainer != null)
        {
            RegeneratePlatforms();
        }
    }
    
    void RegeneratePlatforms()
    {

        if (platformsContainer != null)
        {
            Destroy(platformsContainer);
        }
        
        GeneratePlatforms();
    }
}
