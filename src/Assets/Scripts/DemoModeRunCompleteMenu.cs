using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.MLAgents;
using System.Collections;

/// <summary>
/// Shows a menu with run statistics when an episode completes in demo mode.
/// Handles pause/resume of inference loop and fadeout effects.
/// </summary>
public class DemoModeRunCompleteMenu : MonoBehaviour
{
    private static DemoModeRunCompleteMenu instance;
    
    [Header("UI References")]
    [Tooltip("Canvas for the menu (will be created if null)")]
    public Canvas menuCanvas;
    
    [Tooltip("Fadeout duration in seconds")]
    public float fadeoutDuration = 1f;
    
    [Tooltip("Menu fade in duration")]
    public float menuFadeInDuration = 0.5f;
    
    [Tooltip("Countdown duration before auto-resuming (seconds)")]
    public float countdownDuration = 10f; // Doubled from 5f to 10f
    
    private bool isDemoMode = false;
    private bool isPaused = false;
    private ParkourAgent currentAgent;
    private DecisionRequester decisionRequester;
    
    // UI Components
    private GameObject menuPanel;
    private Image fadeoutImage;
    private Text resultText;
    private Text statsText;
    private Text countdownText;
    
    // Audio
    private AudioSource audioSource;
    private System.Random audioRandom; // Separate random instance for audio (not affected by Unity's Random state)
    
    // Stats from last run
    private string lastEndReason = "";
    private float lastEpisodeReward = 0f;
    private float lastEpisodeTime = 0f;
    private float lastMaxDistance = 0f;
    private int lastJumpCount = 0;
    private int lastForwardCount = 0;
    private int lastSprintCount = 0;
    private int lastIdleCount = 0;
    private int lastRollCount = 0;
    private float lastRollPercent = 0f; // Roll percentage for style audio threshold
    
    public static DemoModeRunCompleteMenu Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DemoModeRunCompleteMenu>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DemoModeRunCompleteMenu");
                    instance = go.AddComponent<DemoModeRunCompleteMenu>();
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
        
        CheckDemoMode();
        
        if (isDemoMode)
        {
            SetupMenuUI();
        }
    }
    
    void CheckDemoMode()
    {
        // Same logic as other demo mode scripts
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
                System.IO.Path.Combine(Application.dataPath, "..", "demo_mode.env")
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
                            break;
                        }
                    }
                    catch { }
                }
            }
        }
    }
    
    void SetupMenuUI()
    {
        // Create Canvas
        if (menuCanvas == null)
        {
            GameObject canvasObj = new GameObject("RunCompleteMenuCanvas");
            canvasObj.transform.SetParent(transform);
            menuCanvas = canvasObj.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 10000; // Above everything
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create fadeout overlay with background image
        GameObject fadeoutObj = new GameObject("FadeoutOverlay");
        fadeoutObj.transform.SetParent(menuCanvas.transform, false);
        fadeoutImage = fadeoutObj.AddComponent<Image>();
        
        // Load background image
        Texture2D bgTexture = LoadBackgroundImage();
        if (bgTexture != null)
        {
            Sprite bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0.5f));
            fadeoutImage.sprite = bgSprite;
            fadeoutImage.type = Image.Type.Simple; // Use Simple to fill the entire area
        }
        else
        {
            // Fallback to black if image not found
            fadeoutImage.color = new Color(0f, 0f, 0f, 0f);
        }
        
        fadeoutImage.color = new Color(1f, 1f, 1f, 0f); // Start transparent (white tint for image)
        fadeoutImage.raycastTarget = false;
        
        RectTransform fadeoutRect = fadeoutImage.GetComponent<RectTransform>();
        fadeoutRect.anchorMin = Vector2.zero;
        fadeoutRect.anchorMax = Vector2.one;
        fadeoutRect.sizeDelta = Vector2.zero;
        fadeoutRect.anchoredPosition = Vector2.zero;
        
        // Create menu panel (centered)
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(menuCanvas.transform, false);
        
        Image panelBg = menuPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark blue-grey background
        
        RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600f, 500f);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Add Vertical Layout Group
        VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        
        // Result text (Success/Failed)
        GameObject resultObj = new GameObject("ResultText");
        resultObj.transform.SetParent(menuPanel.transform, false);
        resultText = resultObj.AddComponent<Text>();
        resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultText.fontSize = 48;
        resultText.fontStyle = FontStyle.Bold;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.color = Color.white;
        
        RectTransform resultRect = resultText.GetComponent<RectTransform>();
        resultRect.sizeDelta = new Vector2(0f, 60f);
        
        // Stats text
        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(menuPanel.transform, false);
        statsText = statsObj.AddComponent<Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 24;
        statsText.alignment = TextAnchor.MiddleLeft;
        statsText.color = Color.white;
        
        RectTransform statsRect = statsText.GetComponent<RectTransform>();
        statsRect.sizeDelta = new Vector2(0f, 300f);
        
        // Countdown text (replaces button)
        GameObject countdownObj = new GameObject("CountdownText");
        countdownObj.transform.SetParent(menuPanel.transform, false);
        countdownText = countdownObj.AddComponent<Text>();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 36;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.color = new Color(0.2f, 0.4f, 0.7f, 1f); // Blue-grey Animus color
        countdownText.raycastTarget = false;
        
        RectTransform countdownRect = countdownText.GetComponent<RectTransform>();
        countdownRect.sizeDelta = new Vector2(0f, 60f);
        
        // Hide menu initially
        menuPanel.SetActive(false);
        menuCanvas.gameObject.SetActive(false);
        
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f; // 2D sound (not 3D)
        audioSource.loop = false;
        
        // Initialize separate random instance for audio (ensures true randomness, not affected by Unity's Random state)
        audioRandom = new System.Random(System.Environment.TickCount);
        
        // Ensure GameObject is active (needed for AudioSource to work)
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // Check if AudioListener exists (required for audio playback)
        if (FindObjectOfType<AudioListener>() == null)
        {
            Debug.LogWarning("[DemoModeRunCompleteMenu] No AudioListener found in scene! Audio won't play. Add AudioListener to Main Camera.");
        }
        
        Debug.Log($"[DemoModeRunCompleteMenu] Menu UI setup complete. AudioSource enabled: {audioSource.enabled}, GameObject active: {gameObject.activeSelf}");
    }
    
    /// <summary>
    /// Called by ParkourAgent when episode ends in demo mode.
    /// </summary>
    public void OnEpisodeComplete(ParkourAgent agent, string endReason, float episodeReward, float episodeTime, 
        float maxDistance, int jumpCount, int forwardCount, int sprintCount, int idleCount, int rollCount)
    {
        if (!isDemoMode)
        {
            return;
        }
        
        // Store stats
        currentAgent = agent;
        lastEndReason = endReason;
        lastEpisodeReward = episodeReward;
        lastEpisodeTime = episodeTime;
        lastMaxDistance = maxDistance;
        lastJumpCount = jumpCount;
        lastForwardCount = forwardCount;
        lastSprintCount = sprintCount;
        lastIdleCount = idleCount;
        lastRollCount = rollCount;
        
        // Find DecisionRequester to pause
        decisionRequester = agent.GetComponent<DecisionRequester>();
        
        // Start fadeout and menu sequence
        StartCoroutine(FadeoutAndShowMenu());
    }
    
    IEnumerator FadeoutAndShowMenu()
    {
        // Wait a tiny bit to let EndEpisode() complete
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Pause inference (after EndEpisode has been called)
        if (decisionRequester != null)
        {
            decisionRequester.enabled = false;
            isPaused = true;
            Debug.Log("[DemoModeRunCompleteMenu] Paused DecisionRequester");
        }
        
        // DON'T pause time scale - it breaks UI input
        // Just pause DecisionRequester to stop inference loop
        
        // Enable canvas
        menuCanvas.gameObject.SetActive(true);
        
        // Fadeout to background image (or black)
        float elapsed = 0f;
        while (elapsed < fadeoutDuration)
        {
            elapsed += Time.deltaTime; // Use regular deltaTime since timeScale is not 0
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeoutDuration);
            // If using sprite, keep white tint; if using color, use black
            if (fadeoutImage.sprite != null)
            {
                fadeoutImage.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                fadeoutImage.color = new Color(0f, 0f, 0f, alpha);
            }
            yield return null;
        }
        if (fadeoutImage.sprite != null)
        {
            fadeoutImage.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            fadeoutImage.color = new Color(0f, 0f, 0f, 1f);
        }
        
        // Update menu text
        UpdateMenuText();
        
        // Play audio based on result and style threshold (start loading in background)
        bool isSuccess = lastEndReason == "Success";
        bool hasStyle = lastRollPercent >= 2.0f; // 2% roll threshold for style audio
        StartCoroutine(PlayResultAudioCoroutine(isSuccess, hasStyle));
        
        // Show menu (fade in)
        menuPanel.SetActive(true);
        CanvasGroup panelGroup = menuPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
        {
            panelGroup = menuPanel.AddComponent<CanvasGroup>();
        }
        
        elapsed = 0f;
        while (elapsed < menuFadeInDuration)
        {
            elapsed += Time.deltaTime; // Use regular deltaTime since timeScale is not 0
            float alpha = Mathf.Lerp(0f, 1f, elapsed / menuFadeInDuration);
            panelGroup.alpha = alpha;
            yield return null;
        }
        panelGroup.alpha = 1f;
        
        // Start countdown
        StartCoroutine(CountdownAndResume());
    }
    
    void UpdateMenuText()
    {
        // Result text
        bool success = lastEndReason == "Success";
        resultText.text = success ? "SUCCESS!" : "FAILED";
        resultText.color = success ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.2f, 0.2f);
        
        // Stats text
        string stats = $"Result: {lastEndReason}\n\n";
        stats += $"Time: {lastEpisodeTime:F2}s\n";
        stats += $"Distance: {lastMaxDistance:F1}m\n";
        stats += $"Reward: {lastEpisodeReward:F2}\n\n";
        stats += $"Actions:\n";
        
        // Calculate percentages
        int totalActions = lastJumpCount + lastForwardCount + lastSprintCount + lastIdleCount + lastRollCount;
        if (totalActions > 0)
        {
            float jumpPercent = (float)lastJumpCount / totalActions * 100f;
            float forwardPercent = (float)lastForwardCount / totalActions * 100f;
            float sprintPercent = (float)lastSprintCount / totalActions * 100f;
            lastRollPercent = (float)lastRollCount / totalActions * 100f; // Store for audio threshold check
            float idlePercent = (float)lastIdleCount / totalActions * 100f;
            
            stats += $"  Jumps: {jumpPercent:F1}%\n";
            stats += $"  Forward: {forwardPercent:F1}%\n";
            stats += $"  Sprint: {sprintPercent:F1}%\n";
            stats += $"  Roll: {lastRollPercent:F1}%\n"; // Roll percentage display
            stats += $"  Idle: {idlePercent:F1}%\n\n";
            
            // Add style indicator if roll percentage is significant
            if (lastRollPercent >= 2.0f)
            {
                stats += $"Style: {lastRollPercent:F1}% rolls ✨";
            }
        }
        else
        {
            stats += "  No actions recorded";
        }
        
        statsText.text = stats;
    }
    
    IEnumerator CountdownAndResume()
    {
        float remaining = countdownDuration;
        while (remaining > 0f)
        {
            int seconds = Mathf.CeilToInt(remaining);
            countdownText.text = $"Loading next run in {seconds}...";
            remaining -= Time.deltaTime;
            yield return null;
        }
        
        // Auto-resume
        StartCoroutine(FadeinAndResume());
    }
    
    IEnumerator FadeinAndResume()
    {
        // Fade out menu
        CanvasGroup panelGroup = menuPanel.GetComponent<CanvasGroup>();
        if (panelGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < menuFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / menuFadeInDuration);
                panelGroup.alpha = alpha;
                yield return null;
            }
        }
        menuPanel.SetActive(false);
        
        // Fade in from background (or black)
        float fadeInDuration = fadeoutDuration * 0.5f; // Faster fade in
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeInDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeInDuration);
            // If using sprite, keep white tint; if using color, use black
            if (fadeoutImage.sprite != null)
            {
                fadeoutImage.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                fadeoutImage.color = new Color(0f, 0f, 0f, alpha);
            }
            yield return null;
        }
        if (fadeoutImage.sprite != null)
        {
            fadeoutImage.color = new Color(1f, 1f, 1f, 0f);
        }
        else
        {
            fadeoutImage.color = new Color(0f, 0f, 0f, 0f);
        }
        
        // Resume inference
        if (decisionRequester != null)
        {
            decisionRequester.enabled = true;
            isPaused = false;
            Debug.Log("[DemoModeRunCompleteMenu] Resumed DecisionRequester");
        }
        
        // Manually trigger new episode
        if (currentAgent != null)
        {
            currentAgent.OnEpisodeBegin();
        }
        
        // Hide canvas
        menuCanvas.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Loads the background image from Assets/UI/backg01.jpg
    /// </summary>
    Texture2D LoadBackgroundImage()
    {
        // Try multiple paths
        string[] possiblePaths = {
            System.IO.Path.Combine(Application.dataPath, "UI", "backg01.jpg"),
            System.IO.Path.Combine(Application.dataPath, "Assets", "UI", "backg01.jpg"),
            System.IO.Path.Combine(Application.streamingAssetsPath, "UI", "backg01.jpg")
        };
        
        foreach (string path in possiblePaths)
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        Debug.Log($"[DemoModeRunCompleteMenu] Loaded background image from: {fullPath}");
                        return texture;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[DemoModeRunCompleteMenu] Failed to load background image from {fullPath}: {e.Message}");
                }
            }
        }
        
        // Try loading as resource (if in Resources folder)
        Texture2D resourceTexture = Resources.Load<Texture2D>("UI/backg01");
        if (resourceTexture != null)
        {
            Debug.Log("[DemoModeRunCompleteMenu] Loaded background image from Resources");
            return resourceTexture;
        }
        
        Debug.LogWarning("[DemoModeRunCompleteMenu] Background image not found, using black fallback");
        return null;
    }
    
    /// <summary>
    /// Plays a random audio file based on success/failure result and style threshold.
    /// Uses "successstyle" or "fellstyle" if roll percentage >= 2%, otherwise uses regular "success" or "fell".
    /// </summary>
    IEnumerator PlayResultAudioCoroutine(bool isSuccess, bool hasStyle)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[DemoModeRunCompleteMenu] AudioSource not initialized");
            yield break;
        }
        
        string prefix;
        int randomNum;
        
        // Use style audio if roll percentage >= 2%, otherwise use regular audio
        if (hasStyle)
        {
            if (isSuccess)
            {
                prefix = "successstyle";
                randomNum = audioRandom.Next(1, 7); // 1-6 (inclusive min, exclusive max)
            }
            else
            {
                prefix = "fellstyle";
                randomNum = audioRandom.Next(1, 4); // 1-3 (fellstyle01-03, inclusive min, exclusive max)
            }
        }
        else
        {
            if (isSuccess)
            {
                prefix = "success";
                randomNum = audioRandom.Next(1, 7); // 1-6 (inclusive min, exclusive max)
            }
            else
            {
                prefix = "fell";
                randomNum = audioRandom.Next(1, 7); // 1-6 (inclusive min, exclusive max)
            }
        }
        
        string fileName = $"{prefix}{randomNum:D2}.mp3"; // success01.mp3, successstyle01.mp3, fellstyle01.mp3, etc.
        
        Debug.Log($"[DemoModeRunCompleteMenu] Playing audio: {fileName} (roll%: {lastRollPercent:F1}%, hasStyle: {hasStyle}, isSuccess: {isSuccess}, randomNum: {randomNum})");
        
        yield return StartCoroutine(LoadAndPlayAudio(fileName));
    }
    
    /// <summary>
    /// Loads and plays an audio clip from Assets/Audio folder.
    /// </summary>
    IEnumerator LoadAndPlayAudio(string fileName)
    {
        // Try multiple paths
        string[] possiblePaths = {
            System.IO.Path.Combine(Application.dataPath, "Audio", fileName),
            System.IO.Path.Combine(Application.dataPath, "Assets", "Audio", fileName),
            System.IO.Path.Combine(Application.streamingAssetsPath, "Audio", fileName)
        };
        
        AudioClip clip = null;
        
        // Try file paths first
        foreach (string path in possiblePaths)
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            if (System.IO.File.Exists(fullPath))
            {
                string fileUrl = "file://" + fullPath;
                Debug.Log($"[DemoModeRunCompleteMenu] Loading audio from: {fileUrl}");
                
                using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(fileUrl, UnityEngine.AudioType.MPEG))
                {
                    yield return www.SendWebRequest();
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                        Debug.Log($"[DemoModeRunCompleteMenu] Successfully loaded audio: {fileName}");
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"[DemoModeRunCompleteMenu] Failed to load audio: {www.error}");
                    }
                }
            }
        }
        
        // Fallback: Try Resources folder
        if (clip == null)
        {
            string resourcePath = $"Audio/{System.IO.Path.GetFileNameWithoutExtension(fileName)}";
            clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                Debug.Log($"[DemoModeRunCompleteMenu] Loaded audio from Resources: {resourcePath}");
            }
        }
        
        // Play the clip if found
        if (clip != null && audioSource != null)
        {
            // Stop any currently playing audio
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[DemoModeRunCompleteMenu] Playing audio: {fileName}, Volume: {audioSource.volume}, IsPlaying: {audioSource.isPlaying}");
            
            // Double-check it's actually playing after a frame
            yield return null;
            if (!audioSource.isPlaying && clip != null)
            {
                Debug.LogWarning($"[DemoModeRunCompleteMenu] Audio failed to play! Clip: {clip.name}, Length: {clip.length}, LoadState: {clip.loadState}");
                // Try PlayOneShot as fallback
                audioSource.PlayOneShot(clip);
            }
        }
        else
        {
            if (clip == null)
            {
                Debug.LogWarning($"[DemoModeRunCompleteMenu] Audio clip is null for: {fileName}");
            }
            if (audioSource == null)
            {
                Debug.LogWarning($"[DemoModeRunCompleteMenu] AudioSource is null!");
            }
        }
    }
}

