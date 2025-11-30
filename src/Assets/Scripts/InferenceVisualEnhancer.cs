using UnityEngine;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Enhances the scene with visual assets (Faith model, materials, buildings) during inference/demo mode.
/// Only activates when running in inference mode AND demo flag is set.
/// </summary>
public class InferenceVisualEnhancer : MonoBehaviour
{
    [Header("Visual Assets")]
    [Tooltip("Faith GLB model prefab (idle.glb). If null, will try to auto-load.")]
    public GameObject faithPrefab;
    
    [Tooltip("Animator Controller for Faith model. If null, will try to auto-load.")]
    public RuntimeAnimatorController animatorController;
    
    [Header("Demo Mode Settings")]
    [Tooltip("If true, only first TrainingArea will be active (others disabled)")]
    public bool singleAgentMode = true;
    
    [Header("City Skyline")]
    [Tooltip("Number of buildings to generate (only for first TrainingArea)")]
    public int buildingCount = 200;
    
    [Tooltip("Building spawn area around first TrainingArea")]
    public Vector2 buildingSpawnRange = new Vector2(-400f, 400f);
    
    private bool isDemoMode = false;
    private bool isInferenceMode = false;
    private GameObject citySkylineContainer;
    
    void Start()
    {
        // Check demo mode first (file-based, doesn't depend on ML-Agents)
        CheckDemoMode();
        
        // For single agent mode, disable extra areas immediately
        if (isDemoMode && singleAgentMode)
        {
            DisableExtraTrainingAreas();
        }
        
        // Check inference mode and enable visuals (may need to wait for ML-Agents to initialize)
        StartCoroutine(CheckAndEnableVisuals());
    }
    
    void CheckDemoMode()
    {
        // 1. Environment variable (if manually set)
        string demoEnv = System.Environment.GetEnvironmentVariable("MLAGENTS_DEMO_MODE");
        isDemoMode = !string.IsNullOrEmpty(demoEnv) && (demoEnv.ToLower() == "true" || demoEnv == "1");
        
        // 2. Check demo_mode.env file - try multiple paths
        if (!isDemoMode)
        {
            // Application.dataPath = Assets folder, so go up to project root
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            string srcFolder = System.IO.Path.Combine(projectRoot, "src");
            
            string[] possiblePaths = {
                System.IO.Path.Combine(srcFolder, "demo_mode.env"),
                System.IO.Path.Combine(projectRoot, "demo_mode.env"),
                System.IO.Path.Combine(Application.dataPath, "..", "src", "demo_mode.env"),
                System.IO.Path.Combine(Application.dataPath, "..", "demo_mode.env"),
                System.IO.Path.Combine(Application.streamingAssetsPath, "demo_mode.env")
            };
            
            foreach (string path in possiblePaths)
            {
                string normalizedPath = System.IO.Path.GetFullPath(path);
                Debug.Log($"Checking for demo_mode.env at: {normalizedPath}");
                
                if (System.IO.File.Exists(normalizedPath))
                {
                    try
                    {
                        string content = System.IO.File.ReadAllText(normalizedPath);
                        Debug.Log($"Found demo_mode.env, content: {content}");
                        if (content.Contains("MLAGENTS_DEMO_MODE=true") || content.Contains("MLAGENTS_DEMO_MODE=1"))
                        {
                            isDemoMode = true;
                            Debug.Log($"✓ Demo mode ENABLED from: {normalizedPath}");
                            break;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error reading demo_mode.env at {normalizedPath}: {e.Message}");
                    }
                }
            }
        }
        
        // 3. Check flag file (legacy method)
        if (!isDemoMode)
        {
            string demoFlagPath = System.IO.Path.Combine(Application.streamingAssetsPath, "demo_mode.flag");
            if (System.IO.File.Exists(demoFlagPath))
            {
                isDemoMode = true;
                Debug.Log("✓ Demo mode ENABLED from flag file");
            }
        }
        
        if (!isDemoMode)
        {
            Debug.Log("✗ Demo mode DISABLED - demo_mode.env not found or not set");
        }
    }
    
    System.Collections.IEnumerator CheckAndEnableVisuals()
    {
        // Wait a frame for ML-Agents to initialize
        yield return null;
        
        // Check inference mode
        CheckInferenceMode();
        
        Debug.Log($"InferenceVisualEnhancer: Demo={isDemoMode}, Inference={isInferenceMode}");
        
        if (isDemoMode && isInferenceMode)
        {
            Debug.Log("✓ Enabling visual enhancements...");
            EnableVisuals();
        }
        else if (isDemoMode && !isInferenceMode)
        {
            Debug.LogWarning("⚠ Demo mode is ON but inference mode not detected. Make sure you're running with --inference flag!");
        }
        else
        {
            Debug.Log($"Skipping visual enhancement (Demo={isDemoMode}, Inference={isInferenceMode})");
        }
    }
    
    void CheckInferenceMode()
    {
        // Method 1: Check BehaviorParameters Model field
        ParkourAgent[] agents = FindObjectsOfType<ParkourAgent>();
        if (agents.Length > 0)
        {
            BehaviorParameters behaviorParams = agents[0].GetComponent<BehaviorParameters>();
            if (behaviorParams != null)
            {
                isInferenceMode = behaviorParams.Model != null;
                if (isInferenceMode)
                {
                    Debug.Log("✓ Inference mode detected: Model loaded in BehaviorParameters");
                    return;
                }
            }
        }
        
        // Method 2: Check BehaviorType
        if (!isInferenceMode)
        {
            ParkourAgent[] allAgents = FindObjectsOfType<ParkourAgent>();
            foreach (ParkourAgent agent in allAgents)
            {
                BehaviorParameters bp = agent.GetComponent<BehaviorParameters>();
                if (bp != null && bp.BehaviorType == BehaviorType.InferenceOnly)
                {
                    isInferenceMode = true;
                    Debug.Log("✓ Inference mode detected: BehaviorType is InferenceOnly");
                    return;
                }
            }
        }
        
        // Method 3: If demo mode is on, assume inference (user wants visuals)
        if (isDemoMode && !isInferenceMode)
        {
            Debug.LogWarning("⚠ Could not detect inference mode, but demo mode is ON. Enabling visuals anyway.");
            isInferenceMode = true; // Assume inference if demo is explicitly enabled
        }
    }
    
    void EnableVisuals()
    {
        Debug.Log("InferenceVisualEnhancer: Enabling visual enhancements for demo mode");
        
        // 1. Handle single agent mode (disable other TrainingAreas)
        if (singleAgentMode)
        {
            DisableExtraTrainingAreas();
        }
        
        // 2. Enhance agents with Faith model
        EnhanceAgents();
        
        // 3. Enhance platform materials
        EnhancePlatforms();
        
        // 4. Generate city skyline (only for first TrainingArea)
        GenerateCitySkyline();
    }
    
    void DisableExtraTrainingAreas()
    {
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true); // Include inactive
        TrainingArea firstArea = null;
        int activeCount = 0;
        
        // Find FIRST TrainingArea (the one without number) - this is the one camera follows
        foreach (TrainingArea area in allAreas)
        {
            if (area.gameObject.name == "TrainingArea" && area.gameObject.activeInHierarchy)
            {
                firstArea = area;
                break;
            }
        }
        
        // If first one not found, find first active one
        if (firstArea == null)
        {
            foreach (TrainingArea area in allAreas)
            {
                if (area.gameObject.activeInHierarchy)
                {
                    firstArea = area;
                    break;
                }
            }
        }
        
        // Count active areas
        foreach (TrainingArea area in allAreas)
        {
            if (area.gameObject.activeInHierarchy)
            {
                activeCount++;
            }
        }
        
        if (firstArea != null && activeCount > 1)
        {
            Debug.Log($"Disabling {activeCount - 1} extra TrainingAreas for demo mode (keeping: {firstArea.name})");
            
            int disabled = 0;
            foreach (TrainingArea area in allAreas)
            {
                if (area != firstArea && area.gameObject.activeInHierarchy)
                {
                    area.gameObject.SetActive(false);
                    disabled++;
                }
            }
            
            Debug.Log($"✓ Disabled {disabled} TrainingAreas. Only {firstArea.name} remains active.");
        }
        else if (firstArea == null)
        {
            Debug.LogWarning("No active TrainingArea found!");
        }
        else
        {
            Debug.Log($"Only 1 TrainingArea active ({firstArea.name}), nothing to disable.");
        }
    }
    
    void EnhanceAgents()
    {
        ParkourAgent[] agents = FindObjectsOfType<ParkourAgent>();
        
        foreach (ParkourAgent agent in agents)
        {
            if (!agent.gameObject.activeInHierarchy) continue;
            
            // Find or load Faith model
            GameObject faithModel = faithPrefab;
            
            if (faithModel == null)
            {
#if UNITY_EDITOR
                // Try to auto-load idle.glb
                string[] possiblePaths = {
                    "Assets/Characters/glb/faith/idle.glb",
                    "Assets/Characters/faith/glb/idle.glb"
                };
                
                foreach (string path in possiblePaths)
                {
                    faithModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (faithModel != null)
                    {
                        Debug.Log($"Auto-loaded Faith model from: {path}");
                        break;
                    }
                }
#endif
            }
            
            if (faithModel == null)
            {
                Debug.LogWarning($"InferenceVisualEnhancer: Could not find Faith model for agent {agent.name}. Skipping visual enhancement.");
                continue;
            }
            
            // Hide capsule completely - disable all renderers and colliders (keep CharacterController)
            Renderer[] capsuleRenderers = agent.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in capsuleRenderers)
            {
                if (renderer.name == "Capsule" || renderer.name == agent.name)
                {
                    renderer.enabled = false;
                }
            }
            
            // Also try to find and disable Capsule child object
            Transform capsuleChild = agent.transform.Find("Capsule");
            if (capsuleChild != null)
            {
                Renderer capsuleRenderer = capsuleChild.GetComponent<Renderer>();
                if (capsuleRenderer != null)
                {
                    capsuleRenderer.enabled = false;
                }
                // Or disable the whole GameObject
                capsuleChild.gameObject.SetActive(false);
            }
            
            // Instantiate Faith model as child of agent
            GameObject faithInstance = Instantiate(faithModel, agent.transform);
            faithInstance.name = "FaithModel";
            faithInstance.transform.localPosition = Vector3.zero;
            faithInstance.transform.localRotation = Quaternion.identity;
            faithInstance.transform.localScale = Vector3.one * 1.5f; // Scale up like SetupDemo
            
            // Add/configure Animator
            Animator animator = faithInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = faithInstance.AddComponent<Animator>();
            }
            
            // Assign animator controller
            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }
            else
            {
#if UNITY_EDITOR
                // Try to auto-load animator controller
                string[] controllerPaths = {
                    "Assets/MaggieAnimationController.controller",
                    "Assets/FaithAnimatorController.controller",
                    "Assets/FaithAnimationController.controller"
                };
                
                foreach (string path in controllerPaths)
                {
                    RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                        Debug.Log($"Auto-loaded Animator Controller from: {path}");
                        break;
                    }
                }
#endif
            }
            
            // Add animation sync component to connect ParkourAgent actions to animator
            AgentAnimationSync animSync = agent.GetComponent<AgentAnimationSync>();
            if (animSync == null)
            {
                animSync = agent.gameObject.AddComponent<AgentAnimationSync>();
            }
            animSync.animator = animator;
            animSync.agent = agent;
            
            Debug.Log($"Enhanced agent {agent.name} with Faith model and animation sync");
        }
    }
    
    void EnhancePlatforms()
    {
        // Mirror's Edge color palette (from SetupDemo)
        Color[] mirrorEdgeColors = {
            new Color(0.9f, 0.9f, 0.9f, 1.0f),       // Clean white/light gray
            new Color(1.0f, 1.0f, 1.0f, 1.0f),       // Pure white 
            new Color(0.8f, 0.1f, 0.1f, 1.0f),       // Deep red
            new Color(0.2f, 0.2f, 0.2f, 1.0f),       // Dark surfaces
            new Color(0.7f, 0.7f, 0.7f, 1.0f),       // Medium gray
            new Color(0.1f, 0.6f, 0.9f, 1.0f)        // Bright blue
        };
        
        float[] colorWeights = { 0.5f, 0.3f, 0.1f, 0.05f, 0.04f, 0.01f };
        
        // Find all platform GameObjects - search through TrainingAreas
        int enhancedCount = 0;
        
        // Search through all TrainingAreas and their children
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true);
        foreach (TrainingArea area in allAreas)
        {
            if (!area.gameObject.activeInHierarchy) continue;
            
            // Look for "Platforms" container child
            Transform platformsContainer = area.transform.Find("Platforms");
            if (platformsContainer != null)
            {
                // Search all children of Platforms container
                for (int i = 0; i < platformsContainer.childCount; i++)
                {
                    GameObject platform = platformsContainer.GetChild(i).gameObject;
                    if (platform.name.StartsWith("Platform_") && platform.GetComponent<Renderer>() != null)
                    {
                        Renderer renderer = platform.GetComponent<Renderer>();
                        Material mat = renderer.material;
                        
                        // Apply Mirror's Edge material properties
                        Color selectedColor = SelectWeightedColor(mirrorEdgeColors, colorWeights);
                        mat.color = selectedColor;
                        mat.SetFloat("_Metallic", 0.05f);
                        mat.SetFloat("_Smoothness", 0.9f);
                        mat.SetFloat("_SpecularHighlights", 1.0f);
                        mat.SetFloat("_GlossyReflections", 0.8f);
                        
                        mat.EnableKeyword("_NORMALMAP");
                        mat.EnableKeyword("_METALLICGLOSSMAP");
                        
                        // Special red platforms get emission
                        if (selectedColor.r > 0.7f && selectedColor.g < 0.3f && selectedColor.b < 0.3f)
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", selectedColor * 0.15f);
                        }
                        
                        enhancedCount++;
                    }
                }
            }
        }
        
        Debug.Log($"Enhanced {enhancedCount} platforms with Mirror's Edge materials");
    }
    
    void GenerateCitySkyline()
    {
        // Find first active TrainingArea
        TrainingArea firstArea = null;
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>();
        
        foreach (TrainingArea area in allAreas)
        {
            if (area.gameObject.activeInHierarchy)
            {
                firstArea = area;
                break;
            }
        }
        
        if (firstArea == null)
        {
            Debug.LogWarning("InferenceVisualEnhancer: No active TrainingArea found for city skyline");
            return;
        }
        
        // Create container for buildings
        if (citySkylineContainer == null)
        {
            citySkylineContainer = new GameObject("CitySkyline");
            citySkylineContainer.transform.SetParent(firstArea.transform);
            citySkylineContainer.transform.localPosition = Vector3.zero;
        }
        
        // City color palette (from SetupDemo)
        Color[] cityColors = {
            new Color(0.7f, 0.85f, 1.0f, 0.8f),     // Light blue with transparency
            new Color(0.8f, 0.9f, 1.0f, 0.9f),      // Very light blue
            new Color(0.9f, 0.95f, 1.0f, 0.85f),    // Almost white with blue tint
            new Color(0.6f, 0.8f, 1.0f, 0.7f),      // Medium blue
            new Color(1.0f, 1.0f, 1.0f, 0.9f)       // Pure white
        };
        
        Vector3 areaPosition = firstArea.transform.position;
        
        for (int i = 0; i < buildingCount; i++)
        {
            // Position buildings around the first TrainingArea
            Vector3 buildingPos = new Vector3(
                areaPosition.x + Random.Range(buildingSpawnRange.x, buildingSpawnRange.y),
                0,
                areaPosition.z + Random.Range(buildingSpawnRange.x, buildingSpawnRange.y)
            );
            
            // Keep buildings away from the parkour path (X axis from area position)
            if (buildingPos.x > areaPosition.x - 20f && buildingPos.x < areaPosition.x + 320f && 
                Mathf.Abs(buildingPos.z - areaPosition.z) < 30f)
            {
                if (buildingPos.z > areaPosition.z) buildingPos.z += 50f;
                else buildingPos.z -= 50f;
            }
            
            // Create building
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            float width = Random.Range(20f, 50f);
            float depth = Random.Range(20f, 50f);
            
            // Wave-like height variation
            float waveX = Mathf.Sin(buildingPos.x * 0.01f) * 30f;
            float waveZ = Mathf.Cos(buildingPos.z * 0.015f) * 25f;
            float baseWaveHeight = waveX + waveZ;
            
            float buildingHeight = Random.Range(50f, 150f);
            float buildingBase = -100f + baseWaveHeight;
            float buildingTop = buildingBase + buildingHeight;
            
            building.transform.position = new Vector3(buildingPos.x, (buildingBase + buildingTop) / 2f, buildingPos.z);
            building.transform.localScale = new Vector3(width, buildingHeight, depth);
            building.name = $"Building_{i}";
            
            // Remove collider (buildings should not collide)
            Collider collider = building.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            // Apply city material
            Renderer renderer = building.GetComponent<Renderer>();
            Material mat = renderer.material;
            
            Color selectedColor = cityColors[Random.Range(0, cityColors.Length)];
            mat.color = selectedColor;
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", 0.7f);
            
            // Enable transparency
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            building.transform.SetParent(citySkylineContainer.transform);
        }
        
        Debug.Log($"Generated {buildingCount} city buildings around first TrainingArea (no colliders)");
    }
    
    Color SelectWeightedColor(Color[] colors, float[] weights)
    {
        float randomValue = Random.Range(0f, 1f);
        float cumulative = 0f;
        
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
            {
                return colors[i];
            }
        }
        
        return colors[0]; // Default
    }
}

