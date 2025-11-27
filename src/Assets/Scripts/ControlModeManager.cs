using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages control mode switching between Player Control and RL Agent Control.
/// Can be configured via config file or Unity Inspector (future: menu system).
/// </summary>
public class ControlModeManager : MonoBehaviour
{
    public enum ControlMode
    {
        Player,      // Human player control (WASD, mouse)
        RLAgent,     // Reinforcement Learning agent control
        Heuristic    // Manual control via ML-Agents heuristic (for testing)
    }
    
    [Header("Control Settings")]
    [SerializeField] private ControlMode currentMode = ControlMode.Player;
    [SerializeField] private bool loadFromConfig = true;
    [SerializeField] private string configFileName = "control_config.json";
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ParkourAgent parkourAgent;
    
    private static ControlModeManager instance;
    public static ControlModeManager Instance => instance;
    
    public ControlMode CurrentMode 
    { 
        get => currentMode; 
        private set => currentMode = value; 
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
        
        // Auto-find components if not assigned
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if (parkourAgent == null)
            parkourAgent = FindObjectOfType<ParkourAgent>();
        
        // Warn if components not found
        if (playerController == null)
            Debug.LogWarning("ControlModeManager: PlayerController not found in scene!");
        if (parkourAgent == null)
            Debug.LogWarning("ControlModeManager: ParkourAgent not found in scene!");
    }
    
    void Start()
    {
        // Load config if enabled
        if (loadFromConfig)
        {
            LoadControlModeFromConfig();
        }
        
        // Apply initial mode (with delay to ensure all components are ready)
        StartCoroutine(ApplyControlModeDelayed());
    }
    
    private System.Collections.IEnumerator ApplyControlModeDelayed()
    {
        // Wait one frame to ensure all components are initialized
        yield return null;
        
        // Log current state before applying
        Debug.Log($"ControlModeManager: About to apply mode '{currentMode}'");
        Debug.Log($"  - PlayerController found: {playerController != null}");
        Debug.Log($"  - ParkourAgent found: {parkourAgent != null}");
        
        SetControlMode(currentMode);
    }
    
    /// <summary>
    /// Sets the control mode and enables/disables appropriate components.
    /// </summary>
    public void SetControlMode(ControlMode mode)
    {
        currentMode = mode;
        
        // Re-find components if they weren't found initially
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if (parkourAgent == null)
            parkourAgent = FindObjectOfType<ParkourAgent>();
        
        // Enable/disable components based on mode
        if (playerController != null)
        {
            bool shouldEnable = (mode == ControlMode.Player);
            playerController.enabled = shouldEnable;
            Debug.Log($"PlayerController enabled: {shouldEnable} (mode: {mode})");
        }
        else
        {
            Debug.LogError("ControlModeManager: PlayerController not found! Cannot set control mode.");
        }
        
        if (parkourAgent != null)
        {
            bool shouldEnable = (mode == ControlMode.RLAgent || mode == ControlMode.Heuristic);
            parkourAgent.enabled = shouldEnable;
            Debug.Log($"ParkourAgent enabled: {shouldEnable} (mode: {mode})");
        }
        else
        {
            Debug.LogWarning("ControlModeManager: ParkourAgent not found. RL Agent mode won't work.");
        }
        
        // Handle cursor lock based on mode
        if (mode == ControlMode.Player)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        
        Debug.Log($"✓ Control Mode set to: {mode}");
    }
    
    /// <summary>
    /// Loads control mode from JSON config file in StreamingAssets or project root.
    /// Falls back to Inspector value if file not found.
    /// </summary>
    private void LoadControlModeFromConfig()
    {
        // Try multiple possible locations
        string[] possiblePaths = new string[]
        {
            System.IO.Path.Combine(Application.streamingAssetsPath, configFileName),
            System.IO.Path.Combine(Application.dataPath, "..", configFileName), // Project root
            System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", configFileName)), // Absolute path
            configFileName // Current directory (for editor)
        };
        
        string configPath = null;
        foreach (string path in possiblePaths)
        {
            if (System.IO.File.Exists(path))
            {
                configPath = path;
                break;
            }
        }
        
        if (configPath != null)
        {
            try
            {
                string json = System.IO.File.ReadAllText(configPath);
                ControlConfig config = JsonUtility.FromJson<ControlConfig>(json);
                
                // Trim whitespace and handle case-insensitive matching
                string modeStr = config.controlMode.Trim();
                bool parsed = System.Enum.TryParse<ControlMode>(modeStr, true, out ControlMode parsedMode);
                
                if (parsed && System.Enum.IsDefined(typeof(ControlMode), parsedMode))
                {
                    currentMode = parsedMode;
                    Debug.Log($"✓ Loaded control mode from config ({configPath}): {parsedMode}");
                }
                else
                {
                    Debug.LogWarning($"Invalid control mode in config: '{config.controlMode}'. Valid values: Player, RLAgent, Heuristic. Using default: {currentMode}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load control config: {e.Message}. Using Inspector value: {currentMode}");
            }
        }
        else
        {
            Debug.LogWarning($"Control config file not found. Tried paths:\n  - {string.Join("\n  - ", possiblePaths)}\nUsing Inspector value: {currentMode}");
        }
    }
    
    /// <summary>
    /// Saves current control mode to config file.
    /// </summary>
    public void SaveControlModeToConfig()
    {
        string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, configFileName);
        
        // Create StreamingAssets directory if it doesn't exist
        if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
        }
        
        ControlConfig config = new ControlConfig
        {
            controlMode = currentMode.ToString()
        };
        
        string json = JsonUtility.ToJson(config, true);
        System.IO.File.WriteAllText(configPath, json);
        Debug.Log($"Saved control mode to config: {currentMode}");
    }
    
    // Runtime switching (for future menu system)
    void Update()
    {
        // Debug keys for switching modes (remove in production or make configurable)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Use Input System instead of old Input class
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                SetControlMode(ControlMode.Player);
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                SetControlMode(ControlMode.RLAgent);
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                SetControlMode(ControlMode.Heuristic);
            }
        }
        #endif
    }
    
    [System.Serializable]
    private class ControlConfig
    {
        public string controlMode;
    }
}

