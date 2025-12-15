using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoSetupControlMode : MonoBehaviour
{
    void Awake()
    {

        if (ControlModeManager.Instance == null)
        {

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
