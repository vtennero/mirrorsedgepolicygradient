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
    [SerializeField] private Vector3 platformSize = new Vector3(12f, 0.5f, 6f);
    [SerializeField] private float[] platformHeights; // Fixed heights for deterministic training
    
    [Header("Randomization (keeps jumps feasible)")]
    [Tooltip("Randomize platform gaps and sizes each episode for better generalization")]
    [SerializeField] private bool randomizePlatforms = true;
    [Tooltip("Gap between platforms will vary between [min, max] units")]
    [SerializeField] private Vector2 gapRange = new Vector2(2f, 8f); // Safe range: agent can jump ~19 units
    [Tooltip("Platform width will vary between [min, max] units")]
    [SerializeField] private Vector2 platformWidthRange = new Vector2(10f, 14f);
    [Tooltip("Randomize platform heights (vertical position)")]
    [SerializeField] private bool randomizeHeights = true;
    [Tooltip("Max height difference between consecutive platforms (units). Agent can jump ~6.4 units high.")]
    [SerializeField] private Vector2 heightChangeRange = new Vector2(-1.5f, 2.5f); // Safe range for varied jumps
    [Tooltip("Absolute min/max heights to keep platforms within reasonable bounds")]
    [SerializeField] private Vector2 absoluteHeightRange = new Vector2(-0.5f, 5f);
    [Tooltip("Random seed for reproducible training (0 = random each time)")]
    [SerializeField] private int randomSeed = 0;
    
    private GameObject platformsContainer;
    
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
        float currentXPosition = 0f;
        float previousHeight = 0f;
        
        for (int i = 0; i < platformCount && i < platformHeights.Length; i++)
        {
            // Randomize platform width if enabled
            float platformWidth = randomizePlatforms 
                ? Random.Range(platformWidthRange.x, platformWidthRange.y) 
                : platformSize.x;
            
            Vector3 localPosition = new Vector3(currentXPosition, platformHeights[i], 0f);
            Vector3 size = new Vector3(platformWidth, platformSize.y, platformSize.z);
            CreatePlatform(i, localPosition, size);
            
            // Calculate next platform position with randomized gap
            if (i < platformCount - 1) // Don't calculate gap after last platform
            {
                float gap = randomizePlatforms 
                    ? Random.Range(gapRange.x, gapRange.y) 
                    : (platformSpacing - platformSize.x);
                
                // Next platform starts at: current position + half current width + gap + half next width
                float nextPlatformWidth = randomizePlatforms 
                    ? Random.Range(platformWidthRange.x, platformWidthRange.y) 
                    : platformSize.x;
                
                float nextXPosition = currentXPosition + (platformWidth / 2f) + gap + (nextPlatformWidth / 2f);
                
                // Validate jump feasibility (editor-only debug check)
                #if UNITY_EDITOR
                if (randomizePlatforms && randomizeHeights && i > 0)
                {
                    float horizontalDist = nextXPosition - currentXPosition;
                    float heightDiff = platformHeights[i + 1] - platformHeights[i];
                    
                    if (!IsJumpFeasible(horizontalDist, heightDiff))
                    {
                        Debug.LogWarning($"Platform {i} -> {i+1}: Potentially difficult jump! " +
                            $"Gap: {horizontalDist:F1}u, Height diff: {heightDiff:F1}u. " +
                            $"Agent may struggle with this configuration.");
                    }
                }
                #endif
                
                currentXPosition = nextXPosition;
            }
            
            previousHeight = platformHeights[i];
        }
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
        float jumpForce = 16f;  // Initial upward velocity
        float gravity = -20f;   // Downward acceleration
        float moveSpeed = 6f;   // Horizontal movement speed
        
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
        float maxHorizontalDist = moveSpeed * timeToTarget;
        
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
        
        // Generate new ones
        GeneratePlatforms();
    }
}

