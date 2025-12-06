using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Displays a stamina bar UI in demo mode showing the agent's current stamina status.
/// Only activates when MLAGENTS_DEMO_MODE=true.
/// </summary>
public class DemoModeStaminaBar : MonoBehaviour
{
    private static DemoModeStaminaBar instance;
    
    [Header("Stamina Bar Settings")]
    [Tooltip("Position of stamina bar (0,0 = bottom-left, 1,1 = top-right)")]
    public Vector2 barPosition = new Vector2(0.02f, 0.95f); // Top-left corner
    
    [Tooltip("Size of stamina bar (width, height) in screen percentage")]
    public Vector2 barSize = new Vector2(0.3f, 0.03f); // 30% width, 3% height
    
    [Tooltip("Color when stamina is high (>50%)")]
    public Color highStaminaColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    
    [Tooltip("Color when stamina is medium (25-50%)")]
    public Color mediumStaminaColor = new Color(1f, 0.8f, 0.2f, 1f); // Yellow
    
    [Tooltip("Color when stamina is low (<25%)")]
    public Color lowStaminaColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
    
    [Tooltip("Background color of stamina bar")]
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray with transparency
    
    [Tooltip("Border color of stamina bar")]
    public Color borderColor = new Color(1f, 1f, 1f, 1f); // White
    
    [Tooltip("Update frequency (how often to check stamina in seconds)")]
    public float updateInterval = 0.1f; // Update 10 times per second
    
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
                // Try to find existing instance
                instance = FindObjectOfType<DemoModeStaminaBar>();
                
                // If not found, create one
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
            SetupStaminaUI();
        }
    }
    
    void Start()
    {
        // Ensure UI is visible if in demo mode
        if (isDemoMode && staminaCanvas != null)
        {
            staminaCanvas.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Public method to ensure stamina bar is initialized (called by other scripts)
    /// </summary>
    public static void EnsureInitialized()
    {
        if (instance == null)
        {
            Instance.ToString(); // Access Instance to trigger creation
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
        
        // 3. Check flag file (legacy method)
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
        
        // Create Canvas
        GameObject canvasObj = new GameObject("StaminaCanvas");
        canvasObj.transform.SetParent(transform);
        staminaCanvas = canvasObj.AddComponent<Canvas>();
        staminaCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        staminaCanvas.sortingOrder = 9998; // Just below flash canvas (9999)
        
        // Add CanvasScaler for proper scaling
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add GraphicRaycaster (required for UI)
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create container for stamina bar
        GameObject barContainer = new GameObject("StaminaBarContainer");
        barContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = barContainer.AddComponent<RectTransform>();
        
        // Position at top-left (anchor to top-left corner)
        // Use pixel coordinates directly for more reliable positioning
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        // Position: 20 pixels from left, 20 pixels from top
        containerRect.anchoredPosition = new Vector2(20f, -20f);
        // Size: 400 pixels wide, 30 pixels tall
        containerRect.sizeDelta = new Vector2(400f, 30f);
        
        // Create border (outermost)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(barContainer.transform, false);
        barBorder = borderObj.AddComponent<Image>();
        barBorder.color = borderColor;
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;
        
        // Create background (inside border)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barContainer.transform, false);
        barBackground = bgObj.AddComponent<Image>();
        barBackground.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.01f, 0.01f); // Small margin for border
        bgRect.anchorMax = new Vector2(0.99f, 0.99f);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create fill (stamina level)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform, false);
        barFill = fillObj.AddComponent<Image>();
        barFill.color = highStaminaColor;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.02f, 0.02f); // Small margin for border
        fillRect.anchorMax = new Vector2(0.98f, 0.98f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // Create text label
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
        
        // Make sure canvas is active
        staminaCanvas.gameObject.SetActive(true);
        
        Debug.Log("[DemoModeStaminaBar] ✓ Stamina bar UI setup complete and visible");
    }
    
    void Update()
    {
        if (!isDemoMode || staminaCanvas == null)
        {
            return;
        }
        
        // Update at specified interval
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStaminaDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateStaminaDisplay()
    {
        // Find agent if not already found
        if (agent == null)
        {
            agent = FindObjectOfType<ParkourAgent>();
            if (agent == null)
            {
                // Keep UI visible but show "No Agent" message
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
                // Show UI when agent is found
                if (staminaCanvas != null)
                {
                    staminaCanvas.gameObject.SetActive(true);
                }
            }
        }
        
        // Get stamina from agent using reflection (since currentStamina is private)
        float currentStamina = 0f;
        float maxStamina = 100f;
        
        try
        {
            // Use reflection to access private field
            var staminaField = typeof(ParkourAgent).GetField("currentStamina", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (staminaField != null)
            {
                currentStamina = (float)staminaField.GetValue(agent);
            }
            
            // Get max stamina from config
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
        
        // Calculate normalized stamina (0.0 to 1.0)
        float normalizedStamina = Mathf.Clamp01(currentStamina / maxStamina);
        
        // Update fill amount
        if (barFill != null)
        {
            barFill.fillAmount = normalizedStamina;
            
            // Update color based on stamina level
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
        
        // Update text
        if (staminaText != null)
        {
            staminaText.text = $"Stamina: {Mathf.RoundToInt(normalizedStamina * 100f)}%";
        }
    }
}

