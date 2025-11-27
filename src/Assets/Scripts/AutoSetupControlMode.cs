using UnityEngine;

/// <summary>
/// Helper script that automatically creates ControlModeManager if it doesn't exist.
/// Add this to any GameObject in the scene (or create an empty GameObject for it).
/// This ensures the control mode system works even if you forget to add ControlModeManager manually.
/// </summary>
[DefaultExecutionOrder(-100)] // Run before other scripts
public class AutoSetupControlMode : MonoBehaviour
{
    void Awake()
    {
        // Check if ControlModeManager already exists
        if (ControlModeManager.Instance == null)
        {
            // Create a new GameObject with ControlModeManager
            GameObject managerObj = new GameObject("ControlModeManager (Auto-Created)");
            managerObj.AddComponent<ControlModeManager>();
            Debug.Log("AutoSetupControlMode: ✓ Created ControlModeManager automatically. It will load from control_config.json");
        }
        else
        {
            Debug.Log("AutoSetupControlMode: ControlModeManager already exists in scene");
        }
    }
}

