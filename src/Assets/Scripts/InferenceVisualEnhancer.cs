using UnityEngine;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        DisableAllAnimusWalls();
    }
    
    void Start()
    {

        CheckDemoMode();
        
        if (isDemoMode)
        {
            DemoModeStaminaBar.EnsureInitialized();
        }
        
        StartCoroutine(CheckAndEnableVisuals());
    }
    
    void DisableAllAnimusWalls()
    {

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
        
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name == "AnimusWall")
            {
                Debug.Log($"[InferenceVisualEnhancer] Destroying AnimusWall found at: {obj.transform.position}");
                #if UNITY_EDITOR
                DestroyImmediate(obj, true);
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

        string demoEnv = System.Environment.GetEnvironmentVariable("MLAGENTS_DEMO_MODE");
        isDemoMode = !string.IsNullOrEmpty(demoEnv) && (demoEnv.ToLower() == "true" || demoEnv == "1");
        
        if (!isDemoMode)
        {

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

        yield return null;
        
        CheckInferenceMode();
        
        Debug.Log($"InferenceVisualEnhancer: Demo={isDemoMode}, Inference={isInferenceMode}");
        
        if (isDemoMode)
        {

            Debug.Log("✓ Enabling visual enhancements for demo mode...");
            EnableVisuals();
        }
        else
        {

            Debug.Log($"Training mode detected (Demo={isDemoMode}, Inference={isInferenceMode}). Ensuring all training areas are active.");
            EnsureAllTrainingAreasActive();
        }
    }
    
    void CheckInferenceMode()
    {

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
        
        if (isDemoMode && !isInferenceMode)
        {
            Debug.LogWarning("⚠ Could not detect inference mode, but demo mode is ON. Enabling visuals anyway.");
            isInferenceMode = true;
        }
    }
    
    void EnableVisuals()
    {
        Debug.Log("InferenceVisualEnhancer: ✓ Enabling visual enhancements for demo mode");
        
        if (singleAgentMode)
        {
            DisableExtraTrainingAreas();
        }
        
        EnhanceAgents();
        
        EnhancePlatforms();
        
        GenerateCitySkyline();
        
        Debug.Log("InferenceVisualEnhancer: Creating finish wall...");
        CreateFinishWall();
        
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
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true);
        TrainingArea firstArea = null;
        int activeCount = 0;
        
        foreach (TrainingArea area in allAreas)
        {
            if (area.gameObject.name == "TrainingArea" && area.gameObject.activeInHierarchy)
            {
                firstArea = area;
                break;
            }
        }
        
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

        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true);
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
            
            GameObject faithModel = faithPrefab;
            
            if (faithModel == null)
            {
#if UNITY_EDITOR

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
            
            Renderer[] capsuleRenderers = agent.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in capsuleRenderers)
            {
                if (renderer.name == "Capsule" || renderer.name == agent.name)
                {
                    renderer.enabled = false;
                }
            }
            
            Transform capsuleChild = agent.transform.Find("Capsule");
            if (capsuleChild != null)
            {
                Renderer capsuleRenderer = capsuleChild.GetComponent<Renderer>();
                if (capsuleRenderer != null)
                {
                    capsuleRenderer.enabled = false;
                }

                capsuleChild.gameObject.SetActive(false);
            }
            
            GameObject faithInstance = Instantiate(faithModel, agent.transform);
            faithInstance.name = "FaithModel";
            faithInstance.transform.localPosition = Vector3.zero;
            faithInstance.transform.localRotation = Quaternion.identity;
            faithInstance.transform.localScale = Vector3.one * 1.5f;
            
            Animator animator = faithInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = faithInstance.AddComponent<Animator>();
            }
            
            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }
            else
            {
#if UNITY_EDITOR

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

        Color[] mirrorEdgeColors = {
            new Color(0.9f, 0.9f, 0.9f, 1.0f),
            new Color(1.0f, 1.0f, 1.0f, 1.0f),
            new Color(0.8f, 0.1f, 0.1f, 1.0f),
            new Color(0.2f, 0.2f, 0.2f, 1.0f),
            new Color(0.7f, 0.7f, 0.7f, 1.0f),
            new Color(0.1f, 0.6f, 0.9f, 1.0f),
            new Color(0.9f, 0.5f, 0.1f, 1.0f)
        };
        
        float[] colorWeights = { 0.5f, 0.3f, 0.1f, 0.05f, 0.04f, 0.01f, 0.0f };
        
        int enhancedCount = 0;
        
        TrainingArea[] allAreas = FindObjectsOfType<TrainingArea>(true);
        foreach (TrainingArea area in allAreas)
        {
            if (!area.gameObject.activeInHierarchy) continue;
            
            Transform platformsContainer = area.transform.Find("Platforms");
            if (platformsContainer != null)
            {

                for (int i = 0; i < platformsContainer.childCount; i++)
                {
                    GameObject platform = platformsContainer.GetChild(i).gameObject;
                    if (platform.name.StartsWith("Platform_") && platform.GetComponent<Renderer>() != null)
                    {
                        Renderer renderer = platform.GetComponent<Renderer>();
                        Material mat = renderer.material;
                        
                        Color selectedColor = SelectWeightedColor(mirrorEdgeColors, colorWeights);
                        mat.color = selectedColor;
                        
                        mat.SetFloat("_Metallic", 0.1f);
                        mat.SetFloat("_Smoothness", 0.8f);
                        mat.SetFloat("_SpecularHighlights", 1.0f);
                        mat.SetFloat("_GlossyReflections", 0.7f);
                        
                        mat.EnableKeyword("_NORMALMAP");
                        mat.EnableKeyword("_METALLICGLOSSMAP");
                        
                        bool isRed = selectedColor.r > 0.7f && selectedColor.g < 0.3f && selectedColor.b < 0.3f;
                        bool isOrange = selectedColor.r > 0.7f && selectedColor.g > 0.4f && selectedColor.g < 0.6f && selectedColor.b < 0.2f;
                        bool isBlack = selectedColor.r < 0.3f && selectedColor.g < 0.3f && selectedColor.b < 0.3f;
                        
                        if (isRed || isOrange || isBlack)
                        {

                            mat.SetFloat("_Metallic", 0.2f);
                            mat.SetFloat("_Smoothness", 0.95f);
                            mat.SetFloat("_GlossyReflections", 0.9f);
                            
                            mat.EnableKeyword("_EMISSION");
                            float emissionStrength = isBlack ? 0.1f : 0.2f;
                            mat.SetColor("_EmissionColor", selectedColor * emissionStrength);
                            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
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
        
        if (citySkylineContainer == null)
        {
            citySkylineContainer = new GameObject("CitySkyline");
            citySkylineContainer.transform.SetParent(firstArea.transform);
            citySkylineContainer.transform.localPosition = Vector3.zero;
        }
        
        Color[] cityColors = {
            new Color(0.7f, 0.85f, 1.0f, 0.8f),
            new Color(0.8f, 0.9f, 1.0f, 0.9f),
            new Color(0.9f, 0.95f, 1.0f, 0.85f),
            new Color(0.6f, 0.8f, 1.0f, 0.7f),
            new Color(1.0f, 1.0f, 1.0f, 0.9f)
        };
        
        Vector3 areaPosition = firstArea.transform.position;
        
        float trackStartX = areaPosition.x - 100f;
        float trackEndX = areaPosition.x + 2000f;
        float trackWidth = 10f;
        
        int actualBuildingCount = buildingCount > 200 ? buildingCount : 600;
        
        for (int i = 0; i < actualBuildingCount; i++)
        {

            float width = Random.Range(20f, 50f);
            float depth = Random.Range(20f, 50f);
            float maxBuildingSize = Mathf.Max(width, depth);
            
            float safeDistance = trackWidth + maxBuildingSize * 0.5f + 10f;
            
            float sideZ = Random.value < 0.5f 
                ? areaPosition.z + Random.Range(-200f, -safeDistance)
                : areaPosition.z + Random.Range(safeDistance, 200f);
            
            Vector3 buildingPos = new Vector3(
                Random.Range(trackStartX, trackEndX),
                0,
                sideZ
            );
            
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            float waveX = Mathf.Sin(buildingPos.x * 0.01f) * 30f;
            float waveZ = Mathf.Cos(buildingPos.z * 0.015f) * 25f;
            float baseWaveHeight = waveX + waveZ;
            
            float buildingHeight = Random.Range(50f, 150f);
            float buildingBase = -100f + baseWaveHeight;
            float buildingTop = buildingBase + buildingHeight;
            
            building.transform.position = new Vector3(buildingPos.x, (buildingBase + buildingTop) / 2f, buildingPos.z);
            building.transform.localScale = new Vector3(width, buildingHeight, depth);
            building.name = $"Building_{i}";
            
            Collider collider = building.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            Renderer renderer = building.GetComponent<Renderer>();
            Material mat = renderer.material;
            
            Color selectedColor = cityColors[Random.Range(0, cityColors.Length)];
            mat.color = selectedColor;
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", 0.7f);
            
            mat.SetFloat("_Mode", 3);
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
    
    public void UpdateFinishWall()
    {
        if (!isDemoMode)
        {
            return;
        }
        CreateFinishWall();
    }
    
    void CreateFinishWall()
    {

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
        
        if (targetTransform != null)
        {
            Transform oldWall = targetTransform.Find("FinishWall");
            if (oldWall == null)
            {
                oldWall = targetTransform.Find("AnimusWall");
            }
            if (oldWall != null)
            {
                Debug.Log($"[InferenceVisualEnhancer] Destroying old wall in CreateFinishWall at: {oldWall.position}");
                #if UNITY_EDITOR
                DestroyImmediate(oldWall.gameObject);
                #else
                Destroy(oldWall.gameObject);
                #endif
            }
        }
        
        Transform wallInArea = firstArea.transform.Find("FinishWall");
        if (wallInArea == null)
        {
            wallInArea = firstArea.transform.Find("AnimusWall");
        }
        if (wallInArea != null)
        {
            Debug.Log($"[InferenceVisualEnhancer] Destroying old wall in TrainingArea at: {wallInArea.position}");
            #if UNITY_EDITOR
            DestroyImmediate(wallInArea.gameObject);
            #else
            Destroy(wallInArea.gameObject);
            #endif
        }
        
        if (targetTransform == null)
        {
            Debug.LogWarning($"[InferenceVisualEnhancer] Target transform not found");
            return;
        }
        
        Vector3 targetWorldPos = targetTransform.position;
        Vector3 targetLocalPos = firstArea.transform.InverseTransformPoint(targetWorldPos);
        Debug.Log($"[InferenceVisualEnhancer] Target world position: {targetWorldPos}, Target local position: {targetLocalPos}");
        
        GameObject finishWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishWall.name = "FinishWall";
        
        float wallWidth = 1f;
        float wallHeight = 10f;
        float wallDepth = 12f;
        Vector3 wallSize = new Vector3(wallDepth, wallHeight, wallWidth);
        
        float wallCenterY = targetLocalPos.y + (wallHeight * 0.5f);
        Vector3 wallLocalPos = new Vector3(targetLocalPos.x, wallCenterY, targetLocalPos.z);
        
        finishWall.transform.SetParent(firstArea.transform);
        finishWall.transform.localPosition = wallLocalPos;
        finishWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
        finishWall.transform.localScale = wallSize;
        
        Collider wallCollider = finishWall.GetComponent<Collider>();
        if (wallCollider != null)
        {
            Destroy(wallCollider);
        }
        
        Renderer renderer = finishWall.GetComponent<Renderer>();
        Material mat = renderer.material;
        
        Color wallColor = new Color(0.2f, 0.4f, 0.7f, 0.4f);
        
        string shaderName = mat.shader.name;
        Debug.Log($"[InferenceVisualEnhancer] Material shader: {shaderName}");
        
        if (shaderName.Contains("Universal Render Pipeline") || shaderName.Contains("URP"))
        {

            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetColor("_BaseColor", wallColor);
            mat.renderQueue = 3000;
        }
        else if (shaderName.Contains("Standard"))
        {

            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.color = wallColor;
            mat.renderQueue = 3000;
        }
        else
        {

            mat.color = wallColor;
        }
        
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.15f, 0.35f, 0.6f, 1f) * 2f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        
        Vector3 wallWorldPos = finishWall.transform.position;
        float wallTargetXDiff = Mathf.Abs(wallWorldPos.x - targetWorldPos.x);
        Debug.Log($"[InferenceVisualEnhancer] ✓ Created finish wall at local position: {wallLocalPos}, world position: {wallWorldPos}, size: {wallSize}");
        Debug.Log($"[InferenceVisualEnhancer] Wall X position verification: Target X={targetWorldPos.x:F3}, Wall X={wallWorldPos.x:F3}, Difference={wallTargetXDiff:F6} (should be < 0.001)");
        
        if (wallTargetXDiff > 0.001f)
        {
            Debug.LogError($"[InferenceVisualEnhancer] ⚠ WARNING: Wall X position mismatch! Target X={targetWorldPos.x:F3}, Wall X={wallWorldPos.x:F3}, Difference={wallTargetXDiff:F3}");
        }
    }
    
    Texture2D CreateGridTexture(int width, int height, Color baseColor, Color gridColor, int gridSize)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        int cellWidth = width / gridSize;
        int cellHeight = height / gridSize;
        int lineWidth = Mathf.Max(1, width / 256);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = baseColor;
                
                if (x % cellWidth < lineWidth || x % cellWidth > cellWidth - lineWidth)
                {
                    pixelColor = Color.Lerp(pixelColor, gridColor, 0.8f);
                }
                
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
        
        return colors[0];
    }
    
}
