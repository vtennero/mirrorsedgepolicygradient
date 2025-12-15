using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DemoModeStaminaBar : MonoBehaviour
{
    private static DemoModeStaminaBar instance;
    
    [Header("Stamina Bar Settings")]
    [Tooltip("Position of stamina bar (0,0 = bottom-left, 1,1 = top-right)")]
    public Vector2 barPosition = new Vector2(0.02f, 0.95f);
    
    [Tooltip("Size of stamina bar (width, height) in screen percentage")]
    public Vector2 barSize = new Vector2(0.3f, 0.03f);
    
    [Tooltip("Color when stamina is high (>50%)")]
    public Color highStaminaColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    
    [Tooltip("Color when stamina is medium (25-50%)")]
    public Color mediumStaminaColor = new Color(1f, 0.8f, 0.2f, 1f);
    
    [Tooltip("Color when stamina is low (<25%)")]
    public Color lowStaminaColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    
    [Tooltip("Background color of stamina bar")]
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Tooltip("Border color of stamina bar")]
    public Color borderColor = new Color(1f, 1f, 1f, 1f);
    
    [Tooltip("Update frequency (how often to check stamina in seconds)")]
    public float updateInterval = 0.1f;
    
    private bool isDemoMode = false;
    private Canvas staminaCanvas;
    private Image barBackground;
    private Image barFill;
    private Image barBorder;
    private Text staminaText;
    private ParkourAgent agent;
    private float lastUpdateTime = 0f;
    
    public static DemoModeStaminaBar Instance
    {
        get
        {
            if (instance == null)
            {

                instance = FindObjectOfType<DemoModeStaminaBar>();
                
                if (instance == null)
                {
                    GameObject go = new GameObject("DemoModeStaminaBar");
                    instance = go.AddComponent<DemoModeStaminaBar>();
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
            SetupStaminaUI();
        }
    }
    
    void Start()
    {

        if (isDemoMode && staminaCanvas != null)
        {
            staminaCanvas.gameObject.SetActive(true);
        }
    }
    
    public static void EnsureInitialized()
    {
        if (instance == null)
        {
            Instance.ToString();
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
                            Debug.Log($"[DemoModeStaminaBar] Demo mode ENABLED from: {normalizedPath}");
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
                Debug.Log("[DemoModeStaminaBar] Demo mode ENABLED from flag file");
            }
        }
        
        if (!isDemoMode)
        {
            Debug.Log("[DemoModeStaminaBar] Demo mode DISABLED - stamina bar will not be displayed");
        }
    }
    
    void SetupStaminaUI()
    {
        Debug.Log("[DemoModeStaminaBar] Setting up stamina UI...");
        
        GameObject canvasObj = new GameObject("StaminaCanvas");
        canvasObj.transform.SetParent(transform);
        staminaCanvas = canvasObj.AddComponent<Canvas>();
        staminaCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        staminaCanvas.sortingOrder = 9998;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject barContainer = new GameObject("StaminaBarContainer");
        barContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = barContainer.AddComponent<RectTransform>();
        
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);

        containerRect.anchoredPosition = new Vector2(20f, -20f);

        containerRect.sizeDelta = new Vector2(400f, 30f);
        
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(barContainer.transform, false);
        barBorder = borderObj.AddComponent<Image>();
        barBorder.color = borderColor;
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;
        
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barContainer.transform, false);
        barBackground = bgObj.AddComponent<Image>();
        barBackground.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.01f, 0.01f);
        bgRect.anchorMax = new Vector2(0.99f, 0.99f);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform, false);
        barFill = fillObj.AddComponent<Image>();
        barFill.color = highStaminaColor;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.02f, 0.02f);
        fillRect.anchorMax = new Vector2(0.98f, 0.98f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        GameObject textObj = new GameObject("StaminaText");
        textObj.transform.SetParent(barContainer.transform, false);
        staminaText = textObj.AddComponent<Text>();
        staminaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        staminaText.fontSize = 18;
        staminaText.color = Color.white;
        staminaText.alignment = TextAnchor.MiddleCenter;
        staminaText.text = "Stamina: 100%";
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        staminaCanvas.gameObject.SetActive(true);
        
        Debug.Log("[DemoModeStaminaBar] ✓ Stamina bar UI setup complete and visible");
    }
    
    void Update()
    {
        if (!isDemoMode || staminaCanvas == null)
        {
            return;
        }
        
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStaminaDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateStaminaDisplay()
    {

        if (agent == null)
        {
            agent = FindObjectOfType<ParkourAgent>();
            if (agent == null)
            {

                if (staminaCanvas != null)
                {
                    staminaCanvas.gameObject.SetActive(true);
                }
                if (staminaText != null)
                {
                    staminaText.text = "Stamina: Waiting for agent...";
                }
                if (barFill != null)
                {
                    barFill.fillAmount = 0f;
                }
                return;
            }
            else
            {

                if (staminaCanvas != null)
                {
                    staminaCanvas.gameObject.SetActive(true);
                }
            }
        }
        
        float currentStamina = 0f;
        float maxStamina = 100f;
        
        try
        {

            var staminaField = typeof(ParkourAgent).GetField("currentStamina", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (staminaField != null)
            {
                currentStamina = (float)staminaField.GetValue(agent);
            }
            
            CharacterConfig config = CharacterConfigManager.Config;
            if (config != null)
            {
                maxStamina = config.maxStamina;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DemoModeStaminaBar] Could not read stamina: {e.Message}");
            return;
        }
        
        float normalizedStamina = Mathf.Clamp01(currentStamina / maxStamina);
        
        if (barFill != null)
        {
            barFill.fillAmount = normalizedStamina;
            
            if (normalizedStamina > 0.5f)
            {
                barFill.color = highStaminaColor;
            }
            else if (normalizedStamina > 0.25f)
            {
                barFill.color = mediumStaminaColor;
            }
            else
            {
                barFill.color = lowStaminaColor;
            }
        }
        
        if (staminaText != null)
        {
            staminaText.text = $"Stamina: {Mathf.RoundToInt(normalizedStamina * 100f)}%";
        }
    }
}
