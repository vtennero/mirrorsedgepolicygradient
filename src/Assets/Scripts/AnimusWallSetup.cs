using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class AnimusWallSetup : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("Animus-style red glow color")]
    public Color wallColor = new Color(1f, 0.2f, 0.2f, 0.4f);
    
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

        SetupAnimusMaterial();
    }
    
    void OnEnable()
    {

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
        
        Material mat;
        
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {

            litShader = Shader.Find("Standard");
            if (litShader == null)
            {

                litShader = Shader.Find("Unlit/Color");
            }
        }
        
        mat = new Material(litShader);
        mat.name = "animus_wall";
        
        Debug.Log($"AnimusWallSetup: Created material with shader: {litShader.name}");
        
        Color finalColor = wallColor;
        if (makeFullyOpaque)
        {
            finalColor = new Color(wallColor.r, wallColor.g, wallColor.b, 1f);
            Debug.Log("AnimusWallSetup: Making wall fully opaque for testing!");
        }
        
        if (litShader.name.Contains("Universal Render Pipeline") || litShader.name.Contains("URP"))
        {

            mat.SetFloat("_Surface", 1);
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

            mat.SetFloat("_Mode", 3);
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

            mat.color = finalColor;
        }
        
        mat.color = finalColor;
        
        try
        {
            mat.EnableKeyword("_EMISSION");

            Color glowColor = new Color(wallColor.r, wallColor.g, wallColor.b, 1f) * 2f;
            mat.SetColor("_EmissionColor", glowColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Debug.Log($"AnimusWallSetup: Added emission glow: {glowColor}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AnimusWallSetup: Could not set emission: {e.Message}");
        }
        
        renderer.material = mat;
        Debug.Log($"AnimusWallSetup: Applied material to renderer. Color: {finalColor}, Alpha: {finalColor.a}");
        
        #if UNITY_EDITOR
        string materialPath = "Assets/Materials/animus_wall.mat";

        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMat == null)
        {
            AssetDatabase.CreateAsset(mat, materialPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"AnimusWallSetup: Created material at {materialPath}");
        }
        else
        {

            EditorUtility.CopySerialized(mat, existingMat);
            AssetDatabase.SaveAssets();
            renderer.sharedMaterial = existingMat;
            Debug.Log($"AnimusWallSetup: Updated existing material at {materialPath}");
        }
        #endif
        
        Debug.Log($"AnimusWallSetup: Material configured successfully! Color: {wallColor}, Opacity: {wallColor.a}");
    }
}
