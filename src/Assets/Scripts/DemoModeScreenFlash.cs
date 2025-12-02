using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Provides screen flash effects (red for failure, green for success) in demo mode.
/// Only activates when MLAGENTS_DEMO_MODE=true.
/// </summary>
public class DemoModeScreenFlash : MonoBehaviour
{
    private static DemoModeScreenFlash instance;
    
    [Header("Flash Settings")]
    [Tooltip("Duration of the flash in seconds")]
    public float flashDuration = 0.5f;
    
    [Tooltip("Alpha/transparency of the flash (0 = transparent, 1 = opaque)")]
    [Range(0f, 1f)]
    public float flashAlpha = 0.3f;
    
    private bool isDemoMode = false;
    private Canvas flashCanvas;
    private Image flashImage;
    private Coroutine currentFlashCoroutine;
    
    public static DemoModeScreenFlash Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find existing instance
                instance = FindObjectOfType<DemoModeScreenFlash>();
                
                // If not found, create one
                if (instance == null)
                {
                    GameObject go = new GameObject("DemoModeScreenFlash");
                    instance = go.AddComponent<DemoModeScreenFlash>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Check demo mode
        CheckDemoMode();
        
        // Only setup UI if in demo mode
        if (isDemoMode)
        {
            SetupFlashUI();
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
                
                if (System.IO.File.Exists(normalizedPath))
                {
                    try
                    {
                        string content = System.IO.File.ReadAllText(normalizedPath);
                        if (content.Contains("MLAGENTS_DEMO_MODE=true") || content.Contains("MLAGENTS_DEMO_MODE=1"))
                        {
                            isDemoMode = true;
                            Debug.Log($"[DemoModeScreenFlash] Demo mode ENABLED from: {normalizedPath}");
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
                Debug.Log("[DemoModeScreenFlash] Demo mode ENABLED from flag file");
            }
        }
        
        if (!isDemoMode)
        {
            Debug.Log("[DemoModeScreenFlash] Demo mode DISABLED - screen flash effects will not be active");
        }
    }
    
    void SetupFlashUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("FlashCanvas");
        canvasObj.transform.SetParent(transform);
        flashCanvas = canvasObj.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 9999; // Ensure it's on top
        
        // Add CanvasScaler for proper scaling
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add GraphicRaycaster (required for UI)
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Image for flash overlay
        GameObject imageObj = new GameObject("FlashImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        flashImage = imageObj.AddComponent<Image>();
        flashImage.color = new Color(1f, 1f, 1f, 0f); // Start transparent
        flashImage.raycastTarget = false; // Don't block input
        
        // Make it fill the entire screen
        RectTransform rectTransform = flashImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        Debug.Log("[DemoModeScreenFlash] Flash UI setup complete");
    }
    
    /// <summary>
    /// Flash the screen red (for agent failure/death)
    /// </summary>
    public void FlashRed()
    {
        if (!isDemoMode || flashImage == null)
        {
            return;
        }
        
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        currentFlashCoroutine = StartCoroutine(FlashCoroutine(Color.red));
    }
    
    /// <summary>
    /// Flash the screen green (for agent success/reaching target)
    /// </summary>
    public void FlashGreen()
    {
        if (!isDemoMode || flashImage == null)
        {
            return;
        }
        
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        currentFlashCoroutine = StartCoroutine(FlashCoroutine(Color.green));
    }
    
    private IEnumerator FlashCoroutine(Color flashColor)
    {
        if (flashImage == null) yield break;
        
        // Set color with transparency
        Color startColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        Color targetColor = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
        
        // Fade in
        float elapsed = 0f;
        float fadeInTime = flashDuration * 0.3f; // 30% of time to fade in
        
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInTime;
            flashImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        flashImage.color = targetColor;
        
        // Hold at full opacity
        yield return new WaitForSeconds(flashDuration * 0.4f); // 40% of time at full
        
        // Fade out
        elapsed = 0f;
        float fadeOutTime = flashDuration * 0.3f; // 30% of time to fade out
        
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutTime;
            flashImage.color = Color.Lerp(targetColor, startColor, t);
            yield return null;
        }
        
        flashImage.color = startColor;
        currentFlashCoroutine = null;
    }
}

