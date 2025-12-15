using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public Vector3 offset = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    public float rotationSpeed = -1f;
    public float verticalRotationLimit = -1f;
    
    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;
    
    public float HorizontalRotation => horizontalRotation;
    
    void Awake()
    {
        InitializeFromConfig();
    }
    
    void InitializeFromConfig()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        if (offset.x == float.MinValue) offset = config.cameraOffset;
        if (rotationSpeed < 0) rotationSpeed = config.cameraRotationSpeed;
        if (verticalRotationLimit < 0) verticalRotationLimit = config.cameraVerticalRotationLimit;
    }
    
    void LateUpdate()
    {
        if (player != null)
        {
            CharacterConfig config = CharacterConfigManager.Config;
            
            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                float mouseX = mouseDelta.x * rotationSpeed * config.mouseInputMultiplier;
                float mouseY = mouseDelta.y * rotationSpeed * config.mouseInputMultiplier;
                
                horizontalRotation += mouseX;
                
                verticalRotation -= mouseY;
                verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
            }
            
            Quaternion rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
            
            Vector3 rotatedOffset = rotation * offset;
            transform.position = player.position + rotatedOffset;
            
            transform.LookAt(player.position + Vector3.up * config.cameraLookHeightOffset);
        }
    }
}
