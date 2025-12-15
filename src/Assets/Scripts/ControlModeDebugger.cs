using UnityEngine;

public class ControlModeDebugger : MonoBehaviour
{
    void Start()
    {
        InvokeRepeating(nameof(LogState), 1f, 2f);
    }
    
    void LogState()
    {
        if (ControlModeManager.Instance == null)
        {
            Debug.LogWarning("ControlModeDebugger: ControlModeManager.Instance is NULL!");
            return;
        }
        
        var mode = ControlModeManager.Instance.CurrentMode;
        var playerController = FindObjectOfType<PlayerController>();
        var parkourAgent = FindObjectOfType<ParkourAgent>();
        
        Debug.Log($"=== Control Mode Debug ===");
        Debug.Log($"Current Mode: {mode}");
        Debug.Log($"PlayerController: {(playerController != null ? "Found" : "NOT FOUND")} - Enabled: {(playerController != null ? playerController.enabled.ToString() : "N/A")}");
        Debug.Log($"ParkourAgent: {(parkourAgent != null ? "Found" : "NOT FOUND")} - Enabled: {(parkourAgent != null ? parkourAgent.enabled.ToString() : "N/A")}");
        Debug.Log($"=========================");
    }
    
    [ContextMenu("Force RL Agent Mode")]
    void ForceRLAgentMode()
    {
        if (ControlModeManager.Instance != null)
        {
            ControlModeManager.Instance.SetControlMode(ControlModeManager.ControlMode.RLAgent);
        }
    }
    
    [ContextMenu("Force Player Mode")]
    void ForcePlayerMode()
    {
        if (ControlModeManager.Instance != null)
        {
            ControlModeManager.Instance.SetControlMode(ControlModeManager.ControlMode.Player);
        }
    }
}
