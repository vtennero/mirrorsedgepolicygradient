using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

                instance = FindObjectOfType<DemoModeScreenFlash>();
                
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

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        CheckDemoMode();
        
        if (isDemoMode)
        {
            SetupFlashUI();
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

        GameObject canvasObj = new GameObject("FlashCanvas");
        canvasObj.transform.SetParent(transform);
        flashCanvas = canvasObj.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 9999;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject imageObj = new GameObject("FlashImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        flashImage = imageObj.AddComponent<Image>();
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false;
        
        RectTransform rectTransform = flashImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        Debug.Log("[DemoModeScreenFlash] Flash UI setup complete");
    }
    
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
        
        Color startColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        Color targetColor = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
        
        float elapsed = 0f;
        float fadeInTime = flashDuration * 0.3f;
        
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInTime;
            flashImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        flashImage.color = targetColor;
        
        yield return new WaitForSeconds(flashDuration * 0.4f);
        
        elapsed = 0f;
        float fadeOutTime = flashDuration * 0.3f;
        
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
