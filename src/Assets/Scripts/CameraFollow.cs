using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public Vector3 offset = new Vector3(float.MinValue, float.MinValue, float.MinValue); // MinValue means use config
    public float rotationSpeed = -1f; // -1 means use config
    public float verticalRotationLimit = -1f; // -1 means use config
    
    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;
    
    // Public property so PlayerController can sync rotation
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
            
            // Get mouse input for camera rotation (using new Input System)
            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                float mouseX = mouseDelta.x * rotationSpeed * config.mouseInputMultiplier;
                float mouseY = mouseDelta.y * rotationSpeed * config.mouseInputMultiplier;
                
                // Horizontal rotation (around Y axis) - rotates around player
                horizontalRotation += mouseX;
                
                // Vertical rotation (around X axis) - tilts camera up/down
                verticalRotation -= mouseY;
                verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
            }
            
            // Create rotation quaternion
            Quaternion rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
            
            // Calculate camera position: rotate offset around player, then add to player position
            Vector3 rotatedOffset = rotation * offset;
            transform.position = player.position + rotatedOffset;
            
            // Camera looks at player (with slight height offset for better view)
            transform.LookAt(player.position + Vector3.up * config.cameraLookHeightOffset);
        }
    }
}
