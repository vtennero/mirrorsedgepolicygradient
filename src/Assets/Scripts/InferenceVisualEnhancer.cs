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
    
    void Awake()
    {
        // Disable old AnimusWalls IMMEDIATELY (before they run OnEnable)
        DisableAllAnimusWalls();
    }
    
    void Start()
    {
        // Check demo mode first (file-based, doesn't depend on ML-Agents)
        CheckDemoMode();
        
        // Initialize stamina bar early if in demo mode
        if (isDemoMode)
        {
            DemoModeStaminaBar.EnsureInitialized();
        }
        
        // DON'T disable training areas here - wait for inference mode check
        // Training mode needs all areas active, only inference should disable extras
        
        // Check inference mode and enable visuals (may need to wait for ML-Agents to initialize)
        StartCoroutine(CheckAndEnableVisuals());
    }
    
    void DisableAllAnimusWalls()
    {
        // Find and disable ALL AnimusWalls immediately (before OnEnable runs)
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true);
        int disabledCount = 0;
        
        foreach (TrainingArea area in allAreas)
        {
            Transform areaTarget = area.GetTargetTransform();
            if (areaTarget != null)
            {
                Transform existingWall = areaTarget.Find("AnimusWall");
                if (existingWall != null)
                {
                    // DESTROY the old wall completely
                    Debug.Log($"[InferenceVisualEnhancer] Destroying old AnimusWall at: {existingWall.position}");
                    #if UNITY_EDITOR
                    DestroyImmediate(existingWall.gameObject);
                    #else
                    Destroy(existingWall.gameObject);
                    #endif
                    disabledCount++;
                }
            }
        }
        
        // Also search by name (including inactive) and DESTROY them
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name == "AnimusWall")
            {
                Debug.Log($"[InferenceVisualEnhancer] Destroying AnimusWall found at: {obj.transform.position}");
                #if UNITY_EDITOR
                DestroyImmediate(obj, true); // Allow destroying assets
                #else
                Destroy(obj);
                #endif
                disabledCount++;
            }
        }
        
        if (disabledCount > 0)
        {
            Debug.Log($"[InferenceVisualEnhancer] Disabled {disabledCount} AnimusWall(s) in Awake");
        }
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
        
        if (isDemoMode)
        {
            // Enable visuals if demo mode is on (even if inference mode isn't detected)
            Debug.Log("✓ Enabling visual enhancements for demo mode...");
            EnableVisuals();
        }
        else
        {
            // Training mode - ensure all training areas are active
            Debug.Log($"Training mode detected (Demo={isDemoMode}, Inference={isInferenceMode}). Ensuring all training areas are active.");
            EnsureAllTrainingAreasActive();
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
        Debug.Log("InferenceVisualEnhancer: ✓ Enabling visual enhancements for demo mode");
        
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
        
        // 5. Create translucent finish wall at target (only in demo mode)
        Debug.Log("InferenceVisualEnhancer: Creating finish wall...");
        CreateFinishWall();
        
        // 6. Initialize stamina bar UI (ensures it's created and visible)
        DemoModeStaminaBar staminaBar = DemoModeStaminaBar.Instance;
        if (staminaBar != null)
        {
            Debug.Log("InferenceVisualEnhancer: ✓ Stamina bar initialized");
        }
        else
        {
            Debug.LogWarning("InferenceVisualEnhancer: ⚠ Stamina bar instance is null!");
        }
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
    
    void EnsureAllTrainingAreasActive()
    {
        // During training, ensure all training areas are active (for parallel training)
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true); // Include inactive
        int activated = 0;
        
        foreach (TrainingArea area in allAreas)
        {
            if (!area.gameObject.activeInHierarchy)
            {
                area.gameObject.SetActive(true);
                activated++;
            }
        }
        
        if (activated > 0)
        {
            Debug.Log($"✓ Activated {activated} TrainingAreas for training mode. Total active: {allAreas.Length}");
        }
        else
        {
            Debug.Log($"All {allAreas.Length} TrainingAreas are already active for training.");
        }
    }
    
    void EnhanceAgents()
    {
        ParkourAgent[] agents = FindObjectsOfType<ParkourAgent>();
        
        foreach (ParkourAgent agent in agents)
        {
            if (!agent.gameObject.activeInHierarchy) continue;
            
            // Find or load Faith model (GLB only)
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
        
        // Track is now 3x longer: ~1460 units (20 platforms, 80% are 3x longer)
        // Generate buildings along the full track length, extending beyond for atmosphere
        float trackStartX = areaPosition.x - 100f; // Start before track begins
        float trackEndX = areaPosition.x + 2000f;  // End well beyond track (track ends ~1460, extend to 2000)
        float trackWidth = 10f;                     // Track corridor width (platforms are 6 units deep, add margin)
        
        // Generate more buildings to cover the longer track (increased from default 200 to 600)
        int actualBuildingCount = buildingCount > 200 ? buildingCount : 600;
        
        for (int i = 0; i < actualBuildingCount; i++)
        {
            // Determine building size first (needed to calculate safe distance)
            float width = Random.Range(20f, 50f);
            float depth = Random.Range(20f, 50f);
            float maxBuildingSize = Mathf.Max(width, depth);
            
            // Safe distance from track: track width (6 units) + margin (10 units) + half building size
            float safeDistance = trackWidth + maxBuildingSize * 0.5f + 10f; // At least 30-40 units from track center
            
            // Position buildings along the full track length, on BOTH SIDES (never on track)
            // Choose which side (left or right) randomly
            float sideZ = Random.value < 0.5f 
                ? areaPosition.z + Random.Range(-200f, -safeDistance)  // Left side (negative Z)
                : areaPosition.z + Random.Range(safeDistance, 200f);   // Right side (positive Z)
            
            Vector3 buildingPos = new Vector3(
                Random.Range(trackStartX, trackEndX),
                0,
                sideZ // Always on one side, never on track
            );
            
            // Create building
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
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
        
        Debug.Log($"Generated {actualBuildingCount} city buildings around first TrainingArea (no colliders) covering track from {trackStartX:F0} to {trackEndX:F0}");
    }
    
    void CreateFinishWall()
    {
        // Find first active TrainingArea
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true);
        TrainingArea firstArea = null;
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
            Debug.LogWarning("InferenceVisualEnhancer: No active TrainingArea found for finish wall");
            return;
        }
        
        Transform targetTransform = firstArea.GetTargetTransform();
        
        // DESTROY old AnimusWall if it exists (do this here too, in case Awake didn't catch it)
        if (targetTransform != null)
        {
            Transform oldWall = targetTransform.Find("AnimusWall");
            if (oldWall != null)
            {
                Debug.Log($"[InferenceVisualEnhancer] Destroying old AnimusWall in CreateFinishWall at: {oldWall.position}");
                #if UNITY_EDITOR
                DestroyImmediate(oldWall.gameObject);
                #else
                Destroy(oldWall.gameObject);
                #endif
            }
        }
        
        // Get target LOCAL position (same as platforms use)
        if (targetTransform == null)
        {
            Debug.LogWarning($"[InferenceVisualEnhancer] Target transform not found");
            return;
        }
        
        Vector3 targetLocalPos = targetTransform.localPosition; // Use LOCAL position like platforms
        Debug.Log($"[InferenceVisualEnhancer] Target local position: {targetLocalPos}");
        
        // Create wall EXACTLY like platforms: Cube primitive, local position, parent to TrainingArea
        GameObject finishWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishWall.name = "FinishWall";
        
        // Size: reasonable wall dimensions
        float wallWidth = 1f;   // Thickness (Z depth)
        float wallHeight = 10f; // Height (Y)
        float wallDepth = 12f;  // Width (X, perpendicular to track)
        Vector3 wallSize = new Vector3(wallDepth, wallHeight, wallWidth);
        
        // Position: Use LOCAL position like platforms, with bottom at platform level
        // Platform is at targetLocalPos.y (typically ~1.25)
        // Wall bottom should be at platform level, so center is at platformY + half height
        float platformY = targetLocalPos.y;
        float wallCenterY = platformY + (wallHeight * 0.5f);
        Vector3 wallLocalPos = new Vector3(targetLocalPos.x, wallCenterY, targetLocalPos.z);
        
        // Parent to TrainingArea and set local position/scale (EXACTLY like platforms)
        finishWall.transform.SetParent(firstArea.transform);
        finishWall.transform.localPosition = wallLocalPos;
        finishWall.transform.localRotation = Quaternion.Euler(0, 90, 0); // Face -X (towards agent)
        finishWall.transform.localScale = wallSize;
        
        // Remove collider
        Collider wallCollider = finishWall.GetComponent<Collider>();
        if (wallCollider != null)
        {
            Destroy(wallCollider);
        }
        
        // Simple material - use existing material from primitive (this worked before!)
        Renderer renderer = finishWall.GetComponent<Renderer>();
        Material mat = renderer.material; // Get material instance (this was working!)
        
        // Blue-grey Animus color - LOW RED, HIGH BLUE, TRANSLUCENT
        Color wallColor = new Color(0.2f, 0.4f, 0.7f, 0.4f); // Blue-grey, 40% opacity for translucent Animus look
        
        // Check shader type and configure transparency accordingly
        string shaderName = mat.shader.name;
        Debug.Log($"[InferenceVisualEnhancer] Material shader: {shaderName}");
        
        if (shaderName.Contains("Universal Render Pipeline") || shaderName.Contains("URP"))
        {
            // URP Lit shader - use URP-specific transparency settings
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha blend
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetColor("_BaseColor", wallColor); // URP uses _BaseColor
            mat.renderQueue = 3000;
        }
        else if (shaderName.Contains("Standard"))
        {
            // Standard shader - use Standard transparency settings
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.color = wallColor; // Standard uses color
            mat.renderQueue = 3000;
        }
        else
        {
            // Fallback - just set color
            mat.color = wallColor;
        }
        
        // Animus-style blue-grey glow (works for both shaders)
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.15f, 0.35f, 0.6f, 1f) * 2f); // Brighter glow for Animus effect
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        
        Debug.Log($"[InferenceVisualEnhancer] ✓ Created finish wall with Animus texture at local position: {wallLocalPos}, size: {wallSize}");
    }
    
    /// <summary>
    /// Creates a procedural grid texture for Animus-style holographic walls.
    /// </summary>
    Texture2D CreateGridTexture(int width, int height, Color baseColor, Color gridColor, int gridSize)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        int cellWidth = width / gridSize;
        int cellHeight = height / gridSize;
        int lineWidth = Mathf.Max(1, width / 256); // Thin grid lines
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = baseColor;
                
                // Draw vertical grid lines
                if (x % cellWidth < lineWidth || x % cellWidth > cellWidth - lineWidth)
                {
                    pixelColor = Color.Lerp(pixelColor, gridColor, 0.8f);
                }
                
                // Draw horizontal grid lines
                if (y % cellHeight < lineWidth || y % cellHeight > cellHeight - lineWidth)
                {
                    pixelColor = Color.Lerp(pixelColor, gridColor, 0.8f);
                }
                
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        
        return texture;
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

