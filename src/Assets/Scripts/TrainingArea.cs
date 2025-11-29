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
    [SerializeField] private int platformCount = 8;
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
        
        // If no heights array provided, create flat platforms
        if (platformHeights == null || platformHeights.Length == 0)
        {
            platformHeights = new float[platformCount];
            for (int i = 0; i < platformCount; i++)
            {
                platformHeights[i] = 0f; // Flat for initial testing
            }
        }
        
        // Initialize random seed if specified
        if (randomSeed > 0)
        {
            Random.InitState(randomSeed);
        }
        
        // Generate platforms with optional randomization
        float currentXPosition = 0f;
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
                
                currentXPosition += (platformWidth / 2f) + gap + (nextPlatformWidth / 2f);
            }
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
    /// Called by agent during OnEpisodeBegin to get spawn position.
    /// </summary>
    public Vector3 GetAgentSpawnPosition()
    {
        if (agentSpawnPoint != null)
        {
            return agentSpawnPoint.position;
        }
        
        // Fallback: spawn at training area origin + offset
        return transform.position + new Vector3(0, 2.5f, 0);
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

