using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Self-contained training environment for a single agent.
/// Contains platforms, spawn point, and target position.
/// </summary>
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
    [SerializeField] private Vector3 platformSize = new Vector3(24f, 0.5f, 6f); // Doubled length: 12f → 24f
    [SerializeField] private float[] platformHeights; // Fixed heights for deterministic training
    
    [Header("Randomization (keeps jumps feasible)")]
    [Tooltip("Randomize platform gaps and sizes each episode for better generalization")]
    [SerializeField] private bool randomizePlatforms = true;
    [Tooltip("Gap between platforms will vary between [min, max] units (edge-to-edge).")]
    [SerializeField] private Vector2 gapRange = new Vector2(2f, 4f); // Moderate difficulty: 2-4 units
    [Tooltip("Platform width will vary between [min, max] units (doubled: was 10-14, now 20-28)")]
    [SerializeField] private Vector2 platformWidthRange = new Vector2(20f, 28f);
    [Tooltip("Randomize platform heights (vertical position)")]
    [SerializeField] private bool randomizeHeights = true;
    [Tooltip("Max height difference between consecutive platforms (units).")]
    [SerializeField] private Vector2 heightChangeRange = new Vector2(-0.5f, 1.0f); // Moderate vertical variation: max 1.0 units up, 0.5 units down
    [Tooltip("Absolute min/max heights to keep platforms within reasonable bounds")]
    [SerializeField] private Vector2 absoluteHeightRange = new Vector2(-0.5f, 5f);
    [Tooltip("Random seed for reproducible training (0 = random each time)")]
    [SerializeField] private int randomSeed = 0;
    
    [Header("Target Position")]
    [Tooltip("Offset from end of last platform to target position (in units). Target will be positioned at: lastPlatformEndX + targetOffset")]
    [SerializeField] private float targetOffset = 5f; // Distance beyond last platform
    
    private GameObject platformsContainer;
    private float lastPlatformEndX = 0f; // Track end position of last platform for target calculation
    
    void Start()
    {
        
        // Validate references
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
            // Auto-assign this TrainingArea to the agent
            agent.GetType().GetField("trainingArea", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance)?.SetValue(agent, this);
        }
        
        // Generate platforms if enabled
        if (generatePlatformsOnStart)
        {
            GeneratePlatforms();
        }
    }
    
    void GeneratePlatforms()
    {
        // Create container for organization
        platformsContainer = new GameObject("Platforms");
        platformsContainer.transform.SetParent(transform);
        platformsContainer.transform.localPosition = Vector3.zero;
        
        // Initialize random seed if specified
        if (randomSeed > 0)
        {
            Random.InitState(randomSeed);
        }
        
        // If no heights array provided, generate heights (with optional randomization)
        if (platformHeights == null || platformHeights.Length == 0)
        {
            platformHeights = new float[platformCount];
            
            // First platform at base height
            platformHeights[0] = Mathf.Clamp(0f, absoluteHeightRange.x, absoluteHeightRange.y);
            
            // Generate subsequent platform heights with randomization
            for (int i = 1; i < platformCount; i++)
            {
                if (randomizeHeights && randomizePlatforms)
                {
                    // Random height change from previous platform
                    float heightChange = Random.Range(heightChangeRange.x, heightChangeRange.y);
                    float newHeight = platformHeights[i - 1] + heightChange;
                    
                    // Clamp to absolute bounds
                    platformHeights[i] = Mathf.Clamp(newHeight, absoluteHeightRange.x, absoluteHeightRange.y);
                }
                else
                {
                    // Flat platforms
                    platformHeights[i] = platformHeights[0];
                }
            }
        }
        
        // Generate platforms with optional randomization
        // FIRST PRINCIPLES: Track edges, calculate gaps, position platforms
        
        float rightEdge = 0f; // Right edge of previous platform
        float previousRightEdge = 0f; // Track previous right edge for debug
        
        for (int i = 0; i < platformCount && i < platformHeights.Length; i++)
        {
            // STEP 1: Calculate this platform's width
            float baseWidth = randomizePlatforms 
                ? Random.Range(platformWidthRange.x, platformWidthRange.y) 
                : platformSize.x;
            
            // 80% chance to make platform 3x longer
            bool makeLongPlatform = Random.value < 0.8f;
            float platformWidth = makeLongPlatform ? baseWidth * 3f : baseWidth;
            
            // STEP 2: Calculate platform position
            float platformCenterX;
            float gap = 0f; // Store gap for debug/validation
            
            if (i == 0)
            {
                // First platform: center at 0
                platformCenterX = 0f;
                rightEdge = platformWidth / 2f; // Right edge = center + half width
            }
            else
            {
                // STEP 2a: Calculate gap (FIXED edge-to-edge, independent of platform size)
                gap = randomizePlatforms 
                    ? Random.Range(gapRange.x, gapRange.y) 
                    : gapRange.x;
                
                // STEP 2b: Calculate left edge = previous right edge + gap
                float leftEdge = previousRightEdge + gap;
                
                // STEP 2c: Calculate center = left edge + half width
                platformCenterX = leftEdge + (platformWidth / 2f);
                
                // STEP 2d: Update right edge for next platform
                rightEdge = platformCenterX + (platformWidth / 2f);
                
                // STEP 3: Verify gap is correct and log it
                float actualGap = leftEdge - previousRightEdge;
                Debug.Log($"[TrainingArea] Platform {i}: gap={gap:F2}, actualGap={actualGap:F2}, " +
                    $"leftEdge={leftEdge:F2}, previousRightEdge={previousRightEdge:F2}, " +
                    $"platformWidth={platformWidth:F2}, gapRange=[{gapRange.x:F2}, {gapRange.y:F2}]");
                
                if (Mathf.Abs(actualGap - gap) > 0.01f)
                {
                    Debug.LogError($"[TrainingArea] Gap calculation mismatch! Expected {gap:F2}, got {actualGap:F2}");
                }
                
                // Warn if gap is too large
                if (gap > 2.5f)
                {
                    Debug.LogWarning($"[TrainingArea] WARNING: Gap {gap:F2} is larger than safe range! Platform {i}");
                }
            }
            
            // Update previous right edge for next iteration (after creating current platform)
            previousRightEdge = rightEdge;
            
            // STEP 4: Create the platform
            Vector3 localPosition = new Vector3(platformCenterX, platformHeights[i], 0f);
            Vector3 size = new Vector3(platformWidth, platformSize.y, platformSize.z);
            CreatePlatform(i, localPosition, size);
            
            // STEP 5: Track last platform end for target position
            if (i == platformCount - 1)
            {
                lastPlatformEndX = rightEdge;
            }
            
            // STEP 6: Validate jump feasibility
            #if UNITY_EDITOR
            if (i > 0 && randomizePlatforms && randomizeHeights)
            {
                float heightDiff = platformHeights[i] - platformHeights[i - 1];
                float jumpDistance = gap + 1f; // Small buffer
                
                if (!IsJumpFeasible(jumpDistance, heightDiff))
                {
                    Debug.LogWarning($"Platform {i-1} -> {i}: Difficult jump! Gap: {gap:F1}u, Height diff: {heightDiff:F1}u");
                }
            }
            #endif
        }
        
        // Update target position based on last platform end position
        UpdateTargetPosition();
    }
    
    void CreatePlatform(int index, Vector3 localPosition, Vector3 size)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = $"Platform_{index}";
        platform.transform.SetParent(platformsContainer.transform);
        platform.transform.localPosition = localPosition;
        platform.transform.localScale = size;
        
        // Ensure collider exists (primitives come with BoxCollider)
        // Add layer or tag if needed for identification
        platform.layer = LayerMask.NameToLayer("Default");
    }
    
    /// <summary>
    /// Checks if a jump between two platforms is physically feasible.
    /// Uses projectile motion physics with agent's movement parameters.
    /// </summary>
    /// <param name="horizontalDist">Horizontal gap distance</param>
    /// <param name="heightDiff">Height difference (positive = jumping up, negative = jumping down)</param>
    /// <returns>True if jump is feasible</returns>
    bool IsJumpFeasible(float horizontalDist, float heightDiff)
    {
        // Agent physics parameters (from CharacterConfig defaults)
        float jumpForce = 8f;   // Initial upward velocity (reduced for more horizontal jumps)
        float gravity = -20f;   // Downward acceleration
        float moveSpeed = 6f;   // Horizontal movement speed
        float jumpForwardBoost = 10f; // Horizontal boost when jumping
        
        // Maximum jump height (at peak of arc): h_max = v² / (2 * |g|)
        float maxJumpHeight = (jumpForce * jumpForce) / (2f * Mathf.Abs(gravity));
        
        // For jumping UP: check if height difference exceeds max jump height
        if (heightDiff > 0 && heightDiff > maxJumpHeight * 0.9f) // 90% safety margin
        {
            return false;
        }
        
        // Calculate time in air to reach target height
        // Using: y = v₀*t + 0.5*g*t²
        // Rearranged: 0.5*g*t² + v₀*t - heightDiff = 0
        float a = 0.5f * gravity;
        float b = jumpForce;
        float c = -heightDiff;
        
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {
            // No real solution - jump not possible
            return false;
        }
        
        // Two solutions: time to reach height on way up and way down
        // We want the positive time solution
        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        float timeToTarget = Mathf.Max(t1, t2);
        
        if (timeToTarget <= 0)
        {
            return false;
        }
        
        // Calculate maximum horizontal distance achievable in that time
        // Include jump forward boost for more accurate calculation
        float effectiveHorizontalSpeed = moveSpeed + jumpForwardBoost;
        float maxHorizontalDist = effectiveHorizontalSpeed * timeToTarget;
        
        // Check if horizontal distance is feasible (with 80% safety margin for running jumps)
        return horizontalDist <= maxHorizontalDist * 1.8f; // Agent can build up speed
    }
    
    /// <summary>
    /// Called by agent during OnEpisodeBegin to get spawn position.
    /// </summary>
    public Vector3 GetAgentSpawnPosition()
    {
        if (agentSpawnPoint != null)
        {
            return agentSpawnPoint.position;
        }
        
        // Fallback: spawn at training area origin + offset (on first platform)
        // First platform is at y=0, height 0.5, so top is at y=0.25.
        // CharacterController center is at y=1 relative to transform, height=2.
        // CharacterController bottom = transform.y + (center.y - height/2) = transform.y + (1 - 1) = transform.y
        // To have bottom slightly above platform (to prevent sinking), spawn at y=1.25
        // This puts CharacterController bottom at y=1.25, which is 1 unit above platform top
        return transform.position + new Vector3(0, 1.25f, 0);
    }
    
    /// <summary>
    /// Called by agent to get target transform reference.
    /// </summary>
    public Transform GetTargetTransform()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Gets the target X position (calculated from last platform end + offset).
    /// This is the single source of truth for target position.
    /// </summary>
    public float GetTargetXPosition()
    {
        return lastPlatformEndX + targetOffset;
    }
    
    /// <summary>
    /// Updates target position based on last platform end position.
    /// Called after platform generation to position target correctly.
    /// </summary>
    void UpdateTargetPosition()
    {
        if (targetPosition != null)
        {
            float targetX = GetTargetXPosition();
            // Get spawn height for Y position (match agent spawn height)
            float spawnY = agentSpawnPoint != null ? agentSpawnPoint.position.y : 1.25f;
            
            // Update target position (in local space relative to TrainingArea)
            targetPosition.localPosition = new Vector3(targetX, spawnY, 0f);
            
            Debug.Log($"[TrainingArea] Target position updated: X={targetX:F1} (last platform end: {lastPlatformEndX:F1} + offset: {targetOffset:F1})");
        }
    }
    
    /// <summary>
    /// Optional: Reset any environment state (for advanced training scenarios).
    /// Called by agent at the start of each episode for randomized environments.
    /// </summary>
    public void ResetArea()
    {
        // Regenerate platforms with new randomization each episode
        if (randomizePlatforms && platformsContainer != null)
        {
            RegeneratePlatforms();
        }
    }
    
    /// <summary>
    /// Destroys and regenerates all platforms with new random configurations.
    /// </summary>
    void RegeneratePlatforms()
    {
        // Destroy old platforms
        if (platformsContainer != null)
        {
            Destroy(platformsContainer);
        }
        
        // Generate new ones (this will also update target position)
        GeneratePlatforms();
    }
}

