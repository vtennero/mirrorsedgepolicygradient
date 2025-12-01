using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script to set up Animus-style transparent wall material.
/// Attach this to the AnimusWall GameObject or run once to create the material.
/// </summary>
[ExecuteInEditMode]
public class AnimusWallSetup : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("Animus-style red glow color")]
    public Color wallColor = new Color(1f, 0.2f, 0.2f, 0.4f); // Red with 40% opacity for transparent glow
    
    [Tooltip("Enable grid pattern (Animus style)")]
    public bool useGridPattern = true;
    
    [Tooltip("Temporarily make wall fully opaque for testing")]
    public bool makeFullyOpaque = false;
    
    void Awake()
    {
        SetupAnimusMaterial();
    }
    
    void Start()
    {
        // Always ensure material is set up
        SetupAnimusMaterial();
    }
    
    void OnEnable()
    {
        // Also run when enabled
        SetupAnimusMaterial();
    }
    
    [ContextMenu("Setup Animus Material")]
    void SetupAnimusMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("AnimusWallSetup: No Renderer component found!");
            return;
        }
        
        // Always create a new material instance to ensure it works
        Material mat;
        
        // Try URP Lit shader first
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            // Fallback to Standard shader
            litShader = Shader.Find("Standard");
            if (litShader == null)
            {
                // Last resort: Unlit shader (always works)
                litShader = Shader.Find("Unlit/Color");
            }
        }
        
        mat = new Material(litShader);
        mat.name = "animus_wall";
        
        Debug.Log($"AnimusWallSetup: Created material with shader: {litShader.name}");
        
        // Determine final color (with optional full opacity for testing)
        Color finalColor = wallColor;
        if (makeFullyOpaque)
        {
            finalColor = new Color(wallColor.r, wallColor.g, wallColor.b, 1f); // Fully opaque for testing
            Debug.Log("AnimusWallSetup: Making wall fully opaque for testing!");
        }
        
        // Configure material based on shader type
        if (litShader.name.Contains("Universal Render Pipeline") || litShader.name.Contains("URP"))
        {
            // URP Lit shader
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetColor("_BaseColor", finalColor);
            mat.renderQueue = 3000;
        }
        else if (litShader.name.Contains("Standard"))
        {
            // Standard shader
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.color = finalColor;
            mat.renderQueue = 3000;
        }
        else
        {
            // Unlit/Color or other simple shader
            mat.color = finalColor;
        }
        
        // Set color (works for most shaders) - already set above, but ensure it's set
        mat.color = finalColor;
        
        // Add strong emission for glow effect (like holographic wall)
        try
        {
            mat.EnableKeyword("_EMISSION");
            // Strong red glow - multiply by higher value for more intense glow
            Color glowColor = new Color(wallColor.r, wallColor.g, wallColor.b, 1f) * 2f; // Bright red glow
            mat.SetColor("_EmissionColor", glowColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Debug.Log($"AnimusWallSetup: Added emission glow: {glowColor}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AnimusWallSetup: Could not set emission: {e.Message}");
        }
        
        // Apply material - use material (instance) for immediate visibility
        renderer.material = mat;
        Debug.Log($"AnimusWallSetup: Applied material to renderer. Color: {finalColor}, Alpha: {finalColor.a}");
        
        // Save material asset (optional - creates file in Materials folder)
        #if UNITY_EDITOR
        string materialPath = "Assets/Materials/animus_wall.mat";
        // Only create if it doesn't exist
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMat == null)
        {
            AssetDatabase.CreateAsset(mat, materialPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"AnimusWallSetup: Created material at {materialPath}");
        }
        else
        {
            // Update existing material
            EditorUtility.CopySerialized(mat, existingMat);
            AssetDatabase.SaveAssets();
            renderer.sharedMaterial = existingMat;
            Debug.Log($"AnimusWallSetup: Updated existing material at {materialPath}");
        }
        #endif
        
        Debug.Log($"AnimusWallSetup: Material configured successfully! Color: {wallColor}, Opacity: {wallColor.a}");
    }
}

