using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetupDemo : MonoBehaviour
{
    [Header("Character Setup")]
    public GameObject faithPrefab;
    
    void Start()
    {
        CreateDemo();
    }
    
    void CreateDemo()
    {

        float firstPlatformHeight = 1f;
        
        Color[] mirrorEdgeColors = {
            new Color(0.9f, 0.9f, 0.9f, 1.0f),
            new Color(1.0f, 1.0f, 1.0f, 1.0f),
            new Color(0.8f, 0.1f, 0.1f, 1.0f),
            new Color(0.2f, 0.2f, 0.2f, 1.0f),
            new Color(0.7f, 0.7f, 0.7f, 1.0f),
            new Color(0.1f, 0.6f, 0.9f, 1.0f)
        };
        
        float[] colorWeights = { 0.5f, 0.3f, 0.1f, 0.05f, 0.04f, 0.01f };
        
        for (int i = 0; i < 36; i++)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            float platformLength = 20f;
            float gapSize = Random.Range(4f, 5f);
            float xPos = i * (platformLength + gapSize);
            
            float height = (i == 0) ? firstPlatformHeight : Mathf.Clamp(firstPlatformHeight + Random.Range(-1.2f, 1.2f), 0.2f, 3f);
            
            platform.transform.position = new Vector3(xPos, height, 0);
            platform.transform.localScale = new Vector3(platformLength, 0.5f, 8f);
            platform.name = "Platform_" + i;
            
            Renderer renderer = platform.GetComponent<Renderer>();
            Material mat = renderer.material;
            
            Color selectedColor = SelectWeightedColor(mirrorEdgeColors, colorWeights);
            mat.color = selectedColor;
            
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Smoothness", 0.9f);
            mat.SetFloat("_SpecularHighlights", 1.0f);
            mat.SetFloat("_GlossyReflections", 0.8f);
            
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICGLOSSMAP");
            
            if (selectedColor.r > 0.7f && selectedColor.g < 0.3f && selectedColor.b < 0.3f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", selectedColor * 0.15f);
            }
            
            Debug.Log($"Platform {i} - Color: R={selectedColor.r:F2} G={selectedColor.g:F2} B={selectedColor.b:F2}");
        }
        
        GenerateCitySkyline();
        
        if (FindObjectOfType<PlayerController>() != null)
        {
            Debug.Log("SetupDemo: Player already exists in scene, skipping player creation");

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

            GameObject faithModel = faithPrefab;
            
            if (faithModel == null)
            {
#if UNITY_EDITOR

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
            
            GameObject player;
            if (faithModel != null)
            {
                player = Instantiate(faithModel);
                player.name = "Player";
            }
            else
            {

                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player (Fallback)";
                DestroyImmediate(player.GetComponent<CapsuleCollider>());
                player.GetComponent<Renderer>().material.color = Color.blue;
            }
            
            player.transform.position = new Vector3(0, 2.5f, 0);
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            player.transform.localScale = Vector3.one * 1.5f;
            
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);
            
            if (player.GetComponent<PlayerController>() == null)
            {
                player.AddComponent<PlayerController>();
            }
            
            if (player.GetComponent<ParkourAgent>() == null)
            {
                player.AddComponent<ParkourAgent>();
                Debug.Log("SetupDemo: Added ParkourAgent component to player");
            }
            
            Animator animator = player.GetComponent<Animator>();
            if (animator == null) animator = player.AddComponent<Animator>();
            
#if UNITY_EDITOR

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

        Color selectedColor = SelectWeightedColor(colors, weights);
        
        Debug.Log($"Selected color for {platform.name}: R={selectedColor.r:F2}, G={selectedColor.g:F2}, B={selectedColor.b:F2}");
        
        Material material = new Material(Shader.Find("Standard"));
        material.color = selectedColor;
        
        material.SetFloat("_Metallic", 0.1f);
        material.SetFloat("_Smoothness", 0.8f);
        material.SetFloat("_SpecularHighlights", 1.0f);
        material.SetFloat("_GlossyReflections", 0.7f);
        
        if (selectedColor.r > 0.8f && selectedColor.g < 0.3f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", selectedColor * 0.2f);
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
        
        return colors[0];
    }
    
    void GenerateCitySkyline()
    {

        Color[] cityColors = {
            new Color(0.7f, 0.85f, 1.0f, 0.8f),
            new Color(0.8f, 0.9f, 1.0f, 0.9f),
            new Color(0.9f, 0.95f, 1.0f, 0.85f),
            new Color(0.6f, 0.8f, 1.0f, 0.7f),
            new Color(1.0f, 1.0f, 1.0f, 0.9f)
        };
        
        float trackStartX = -100f;
        float trackEndX = 2000f;
        float trackWidth = 10f;
        
        int buildingCount = 600;
        
        for (int i = 0; i < buildingCount; i++)
        {

            float width = Random.Range(20f, 50f);
            float depth = Random.Range(20f, 50f);
            float maxBuildingSize = Mathf.Max(width, depth);
            
            float safeDistance = trackWidth + maxBuildingSize * 0.5f + 10f;
            
            float sideZ = Random.value < 0.5f 
                ? Random.Range(-200f, -safeDistance)
                : Random.Range(safeDistance, 200f);
            
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
            building.name = "Building_" + i;
            
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
        }
        
        Debug.Log($"Generated {buildingCount} city buildings for atmospheric skyline");
    }
}
