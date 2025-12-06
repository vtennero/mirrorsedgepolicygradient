using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetupDemo : MonoBehaviour
{
    [Header("Character Setup")]
    public GameObject faithPrefab; // Drag Faith GLB here in Inspector
    
    void Start()
    {
        CreateDemo();
    }
    
    void CreateDemo()
    {
        // Create realistic Mirror's Edge style platforms with iconic materials
        float firstPlatformHeight = 1f; // Fixed height for first platform
        
        // Mirror's Edge color palette (CORRECT COLORS)
        Color[] mirrorEdgeColors = {
            new Color(0.9f, 0.9f, 0.9f, 1.0f),       // Clean white/light gray (most common)
            new Color(1.0f, 1.0f, 1.0f, 1.0f),       // Pure white 
            new Color(0.8f, 0.1f, 0.1f, 1.0f),       // Deep red (runner's vision)
            new Color(0.2f, 0.2f, 0.2f, 1.0f),       // Dark surfaces
            new Color(0.7f, 0.7f, 0.7f, 1.0f),       // Medium gray
            new Color(0.1f, 0.6f, 0.9f, 1.0f)        // Bright blue (glass/accents)
        };
        
        // Weighted probabilities (mostly white/gray like the reference)
        float[] colorWeights = { 0.5f, 0.3f, 0.1f, 0.05f, 0.04f, 0.01f };
        
        for (int i = 0; i < 36; i++)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Platform length and spacing calculations
            float platformLength = 20f; // Much longer platforms for running
            float gapSize = Random.Range(4f, 5f); // 4-5 unit gaps (safely manageable)
            float xPos = i * (platformLength + gapSize); // Position = (platform + gap) * index
            
            // Realistic height variation: max 1.2 units up/down (within jump reach)
            float height = (i == 0) ? firstPlatformHeight : Mathf.Clamp(firstPlatformHeight + Random.Range(-1.2f, 1.2f), 0.2f, 3f);
            
            platform.transform.position = new Vector3(xPos, height, 0);
            platform.transform.localScale = new Vector3(platformLength, 0.5f, 8f); // Long platforms: 20x8
            platform.name = "Platform_" + i;
            
            // Apply Mirror's Edge materials with proper colors and properties
            Renderer renderer = platform.GetComponent<Renderer>();
            Material mat = renderer.material;
            
            // Set base color first
            Color selectedColor = SelectWeightedColor(mirrorEdgeColors, colorWeights);
            mat.color = selectedColor;
            
            // Mirror's Edge material properties - clean, bright, slightly reflective
            mat.SetFloat("_Metallic", 0.05f);        // Very slight metallic (clean but not metal)
            mat.SetFloat("_Smoothness", 0.9f);       // High smoothness for that clean Mirror's Edge look
            mat.SetFloat("_SpecularHighlights", 1.0f);
            mat.SetFloat("_GlossyReflections", 0.8f);
            
            // Enhanced lighting response
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICGLOSSMAP");
            
            // Special red platforms get subtle emission
            if (selectedColor.r > 0.7f && selectedColor.g < 0.3f && selectedColor.b < 0.3f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", selectedColor * 0.15f); // Subtle red glow
            }
            
            Debug.Log($"Platform {i} - Color: R={selectedColor.r:F2} G={selectedColor.g:F2} B={selectedColor.b:F2}");
        }
        
        // Generate city skyline around the parkour area
        GenerateCitySkyline();
        
        // Check if player already exists in scene (don't create duplicate)
        if (FindObjectOfType<PlayerController>() != null)
        {
            Debug.Log("SetupDemo: Player already exists in scene, skipping player creation");
            // Still setup camera if needed
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraFollow>() == null)
            {
                PlayerController existingPlayer = FindObjectOfType<PlayerController>();
                CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.player = existingPlayer.transform;
            }
        }
        else
        {
            // Use the assigned prefab or try to load GLB
            GameObject faithModel = faithPrefab;
            
            if (faithModel == null)
            {
#if UNITY_EDITOR
                // Try to load GLB directly using AssetDatabase
                string glbPath = "Assets/Characters/faith/glb/idle.glb";
                faithModel = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
                
                if (faithModel != null)
                {
                    Debug.Log($"Auto-found Faith GLB at: {glbPath}");
                }
                else
                {
                    Debug.LogWarning($"Faith GLB not found. Drag idle.glb to SetupDemo's 'Faith Prefab' field in Inspector");
                }
#else
                Debug.LogWarning("Drag idle.glb to SetupDemo's 'Faith Prefab' field in Inspector");
#endif
            }
            
            // Create player
            GameObject player;
            if (faithModel != null)
            {
                player = Instantiate(faithModel);
                player.name = "Player";
            }
            else
            {
                // Fallback to capsule if GLB not found
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player (Fallback)";
                DestroyImmediate(player.GetComponent<CapsuleCollider>());
                player.GetComponent<Renderer>().material.color = Color.blue;
            }
            
            player.transform.position = new Vector3(0, 2.5f, 0); // Spawn above first platform
            player.transform.rotation = Quaternion.Euler(0, 90, 0); // Face along the platform line
            player.transform.localScale = Vector3.one * 1.5f; // Make Faith 1.5x bigger (reasonable size)
            
            // Add components
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            
            // CharacterController size adjusted for scaled character
            cc.height = 1.8f; // Keep normal human proportions
            cc.radius = 0.4f; // Slightly wider for scaled model
            cc.center = new Vector3(0, 0.9f, 0);
            
            // Check if PlayerController already exists (don't duplicate)
            if (player.GetComponent<PlayerController>() == null)
            {
                player.AddComponent<PlayerController>();
            }
            
            // Add ParkourAgent if it doesn't exist
            if (player.GetComponent<ParkourAgent>() == null)
            {
                player.AddComponent<ParkourAgent>();
                Debug.Log("SetupDemo: Added ParkourAgent component to player");
            }
            
            // Add animator and assign controller
            Animator animator = player.GetComponent<Animator>();
            if (animator == null) animator = player.AddComponent<Animator>();
            
#if UNITY_EDITOR
            // Try to find and assign the animator controller
            string[] possiblePaths = {
                "Assets/FaithAnimatorController.controller",
                "Assets/FaithAnimationController.controller",
                "Assets/MaggieAnimatorController.controller",
                "Assets/MaggieAnimationController.controller"
            };
            
            foreach (string path in possiblePaths)
            {
                UnityEditor.Animations.AnimatorController controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log($"Auto-assigned animator controller: {path}");
                    break;
                }
            }
            
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("No animator controller found. You'll need to assign it manually in Inspector.");
            }
#endif
            
            // Setup camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.player = player.transform;
            }
            
            Debug.Log("Demo ready! Use WASD + Space");
        }
    }
    
    void ApplyMirrorEdgeMaterial(GameObject platform, Color[] colors, float[] weights)
    {
        // Weighted random color selection
        Color selectedColor = SelectWeightedColor(colors, weights);
        
        Debug.Log($"Selected color for {platform.name}: R={selectedColor.r:F2}, G={selectedColor.g:F2}, B={selectedColor.b:F2}");
        
        // Create material with Mirror's Edge properties
        Material material = new Material(Shader.Find("Standard"));
        material.color = selectedColor;
        
        // Mirror's Edge aesthetic: clean, bright, slightly reflective
        material.SetFloat("_Metallic", 0.1f);      // Slight metallic look
        material.SetFloat("_Smoothness", 0.8f);    // High smoothness for clean look
        material.SetFloat("_SpecularHighlights", 1.0f);
        material.SetFloat("_GlossyReflections", 0.7f);
        
        // Special treatment for red platforms (key navigation elements)
        if (selectedColor.r > 0.8f && selectedColor.g < 0.3f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", selectedColor * 0.2f); // Subtle glow
        }
        
        platform.GetComponent<Renderer>().material = material;
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
        
        return colors[0]; // Default to white
    }
    
    void GenerateCitySkyline()
    {
        // City color palette - cool blues and whites for background atmosphere
        Color[] cityColors = {
            new Color(0.7f, 0.85f, 1.0f, 0.8f),     // Light blue with transparency
            new Color(0.8f, 0.9f, 1.0f, 0.9f),      // Very light blue
            new Color(0.9f, 0.95f, 1.0f, 0.85f),    // Almost white with blue tint
            new Color(0.6f, 0.8f, 1.0f, 0.7f),      // Medium blue
            new Color(1.0f, 1.0f, 1.0f, 0.9f)       // Pure white
        };
        
        // Track is now 3x longer: ~1460 units (20 platforms, 80% are 3x longer)
        // Generate buildings along the full track length, extending beyond for atmosphere
        float trackStartX = -100f; // Start before track begins
        float trackEndX = 2000f;    // End well beyond track (track ends ~1460, extend to 2000)
        float trackWidth = 10f;     // Track corridor width (platforms are 6 units deep, add margin)
        
        // Generate more buildings to cover the longer track (increased from 200 to 600)
        int buildingCount = 600;
        
        for (int i = 0; i < buildingCount; i++)
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
                ? Random.Range(-200f, -safeDistance)  // Left side (negative Z)
                : Random.Range(safeDistance, 200f); // Right side (positive Z)
            
            Vector3 buildingPos = new Vector3(
                Random.Range(trackStartX, trackEndX),
                0,
                sideZ // Always on one side, never on track
            );
            
            // Create building
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Wave-like height variation across the map
            float waveX = Mathf.Sin(buildingPos.x * 0.01f) * 30f;
            float waveZ = Mathf.Cos(buildingPos.z * 0.015f) * 25f;
            float baseWaveHeight = waveX + waveZ;
            
            // Buildings START at -100 and go up/down from there (player is at ~0-3 level)
            float buildingHeight = Random.Range(50f, 150f); // Height of building
            float buildingBase = -100f + baseWaveHeight; // Base starts at -100, varies with waves
            float buildingTop = buildingBase + buildingHeight;
            
            building.transform.position = new Vector3(buildingPos.x, (buildingBase + buildingTop) / 2f, buildingPos.z);
            building.transform.localScale = new Vector3(width, buildingHeight, depth);
            building.name = "Building_" + i;
            
            // Apply city material
            Renderer renderer = building.GetComponent<Renderer>();
            Material mat = renderer.material;
            
            // Random city color
            Color selectedColor = cityColors[Random.Range(0, cityColors.Length)];
            mat.color = selectedColor;
            
            // City building material properties - clean but atmospheric
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", 0.7f);
            
            // Enable transparency for atmospheric effect
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        
        Debug.Log($"Generated {buildingCount} city buildings for atmospheric skyline");
    }
}
