using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public float moveSpeed = -1f; // -1 means use config
    public float sprintSpeed = -1f; // -1 means use config
    public float sprintAccelTime = -1f; // -1 means use config
    private float currentSprintTime = 0f;
    private bool isMoving = false;
    public float jumpForce = -1f; // -1 means use config
    public float jumpForwardBoost = -1f; // -1 means use config
    public float gravity = -1f; // -1 means use config
    
    [Header("Ground Detection")]
    public float groundCheckDistance = -1f; // -1 means use config
    public LayerMask groundMask = 1;
    
    // Components
    private CharacterController characterController;
    
    // Movement variables
    private Vector3 velocity; // Vertical velocity (gravity/jumping)
    private Vector3 jumpMomentum; // Horizontal momentum from jump
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintPressed;
    
    // Mouse look variables
    private float mouseX = 0f;
    private float mouseSensitivity = -1f; // -1 means use config
    public float verticalLookLimit = -1f; // -1 means use config
    
    // Animation (if available)
    private Animator animator;
    
    // Jump animation states
    private enum JumpState
    {
        Grounded,
        JumpStart,
        JumpLoop,
        JumpEnd
    }
    private JumpState currentJumpState = JumpState.Grounded;
    private bool wasGrounded = true;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Initialize mouse rotation to match player's starting rotation
        mouseX = transform.eulerAngles.y;
        
        // Lock cursor for mouse look
        Cursor.lockState = CursorLockMode.Locked;
        
        // Initialize values from config if not overridden
        InitializeFromConfig();
    }
    
    void InitializeFromConfig()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        if (moveSpeed < 0) moveSpeed = config.moveSpeed;
        if (sprintSpeed < 0) sprintSpeed = config.sprintSpeed;
        if (sprintAccelTime < 0) sprintAccelTime = config.sprintAccelTime;
        if (jumpForce < 0) jumpForce = config.jumpForce;
        if (jumpForwardBoost < 0) jumpForwardBoost = config.jumpForwardBoost;
        if (gravity < 0) gravity = config.gravity;
        if (groundCheckDistance < 0) groundCheckDistance = config.groundCheckDistance;
        if (mouseSensitivity < 0) mouseSensitivity = config.mouseSensitivity;
        if (verticalLookLimit < 0) verticalLookLimit = config.playerVerticalLookLimit;
    }
    
    void Update()
    {
        // Only process input and movement if in Player control mode
        // If ControlModeManager doesn't exist, default to Player mode (backward compatibility)
        if (ControlModeManager.Instance != null)
        {
            if (ControlModeManager.Instance.CurrentMode != ControlModeManager.ControlMode.Player)
            {
                return; // Skip all processing if not in player mode
            }
        }
        // If no ControlModeManager exists, continue with player control (default behavior)
        
        HandleInput();
        HandleGroundDetection();
        HandleMovement();
        UpdateAnimations(); // Do animations BEFORE jumping logic
        HandleJumping();
    }
    
    void HandleInput()
    {
        // New Input System - direct keyboard access
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null) return;
        
        CharacterConfig config = CharacterConfigManager.Config;
        
        // WASD movement
        moveInput.x = 0;
        moveInput.y = 0;
        
        if (keyboard.aKey.isPressed) moveInput.x = -1;
        if (keyboard.dKey.isPressed) moveInput.x = 1;
        if (keyboard.wKey.isPressed) moveInput.y = 1;
        if (keyboard.sKey.isPressed) moveInput.y = -1;
        
        // Arrow keys as alternative
        if (keyboard.leftArrowKey.isPressed) moveInput.x = -1;
        if (keyboard.rightArrowKey.isPressed) moveInput.x = 1;
        if (keyboard.upArrowKey.isPressed) moveInput.y = 1;
        if (keyboard.downArrowKey.isPressed) moveInput.y = -1;
        
        // Mouse look - for third-person, only horizontal rotation affects player
        // Vertical rotation is handled by camera
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            mouseX += mouseDelta.x * mouseSensitivity * config.mouseInputMultiplier; // Horizontal rotation (player turns)
            // mouseY is no longer used for player rotation in third-person mode
            // Camera handles vertical rotation
        }
        
        // Jump
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }
        
        // Sprint with progressive acceleration
        sprintPressed = keyboard.leftShiftKey.isPressed;
        
        // Check if player is moving
        isMoving = moveInput.magnitude > config.movementThreshold;
        
        if (sprintPressed && isMoving)
        {
            currentSprintTime = Mathf.Min(currentSprintTime + Time.deltaTime, sprintAccelTime);
        }
        else
        {
            currentSprintTime = Mathf.Max(currentSprintTime - Time.deltaTime * config.sprintDecelerationRate, 0f); // Decelerate faster
        }
        
        // Toggle cursor lock with Escape
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    void HandleGroundDetection()
    {
        // Use CharacterController's built-in ground detection
        isGrounded = characterController.isGrounded;
        
        // Reset velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            CharacterConfig config = CharacterConfigManager.Config;
            velocity.y = config.groundedVelocityReset; // Small negative value to keep grounded
            jumpMomentum = Vector3.zero; // Reset jump momentum when grounded
        }
    }
    
    void HandleMovement()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        // For third-person: Sync player rotation with camera's horizontal rotation
        // This gives proper third-person feel where player faces camera direction
        CameraFollow cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            // Sync player rotation with camera's horizontal rotation
            transform.rotation = Quaternion.Euler(0, cameraFollow.HorizontalRotation, 0);
        }
        else
        {
            // Fallback: rotate player with mouse X if no camera follow script
            transform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
        
        // Calculate movement direction relative to player's facing direction
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        
        // Use sprint speed if shift is held
        // Calculate progressive sprint speed with more noticeable curve
        float sprintProgress = currentSprintTime / sprintAccelTime;
        float sprintCurve = sprintProgress * sprintProgress; // Quadratic curve (slow start, faster finish)
        float currentSprintSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, sprintCurve);
        
        float currentSpeed = (sprintPressed && isMoving) ? currentSprintSpeed : moveSpeed;
        
        // Apply movement (normal movement + jump momentum for longer jumps)
        Vector3 totalMovement = (moveDirection * currentSpeed) + jumpMomentum;
        characterController.Move(totalMovement * Time.deltaTime);
        
        // Decay jump momentum over time (air resistance)
        jumpMomentum = Vector3.Lerp(jumpMomentum, Vector3.zero, Time.deltaTime * config.jumpMomentumDecayRate);
    }
    
    void HandleJumping()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Handle jumping - ONLY when grounded and jump pressed
        if (jumpPressed && isGrounded)
        {
            velocity.y = jumpForce;
            
            // Add forward momentum when jumping (for longer jumps)
            if (moveInput.magnitude > config.movementThreshold)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                Vector3 jumpDirection = (forward * moveInput.y + right * moveInput.x).normalized;
                // Add horizontal boost for longer jumps
                jumpMomentum = jumpDirection * jumpForwardBoost;
            }
            else
            {
                jumpMomentum = Vector3.zero;
            }
        }
        
        // Always reset jump input at end of frame
        jumpPressed = false;
        
        // Apply vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        CharacterConfig config = CharacterConfigManager.Config;
        
        // Calculate speed for animation blending
        float speed = moveInput.magnitude;
        
        // Handle jump state machine
        HandleJumpAnimations();
        
        // Only handle ground movement animations when grounded
        if (currentJumpState == JumpState.Grounded)
        {
            // Set jogging and sprinting based on movement
            animator.SetBool("IsJogging", isMoving);
            // Use sprint animation when sprinting progress is significant
            bool isActuallySprinting = sprintPressed && isMoving && (currentSprintTime > config.sprintAnimationThreshold);
            animator.SetBool("IsSprinting", isActuallySprinting);
        }
        else
        {
            // Turn off ground movement animations when in air
            animator.SetBool("IsJogging", false);
            animator.SetBool("IsSprinting", false);
        }
        
        // Set parameters for the animator
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", velocity.y);
        
        // Speed up sprint animation to match movement speed
        if (currentJumpState == JumpState.Grounded)
        {
            float animationSpeed = config.baseAnimationSpeed;
            if (sprintPressed && isMoving && currentSprintTime > config.sprintAnimationThreshold)
            {
                // Calculate animation speed based on actual movement speed (capped for smoothness)
                float sprintProgress = currentSprintTime / sprintAccelTime;
                float sprintCurve = sprintProgress * sprintProgress;
                float currentSprintSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, sprintCurve);
                animationSpeed = Mathf.Lerp(config.baseAnimationSpeed, config.maxSprintAnimationSpeed, (currentSprintSpeed - moveSpeed) / (sprintSpeed - moveSpeed));
            }
            animator.speed = animationSpeed;
        }
        else
        {
            animator.speed = config.baseAnimationSpeed; // Normal speed for jump animations
        }
        
        // Update previous frame state
        wasGrounded = isGrounded;
    }
    
    void HandleJumpAnimations()
    {
        // State transitions
        switch (currentJumpState)
        {
            case JumpState.Grounded:
                if (jumpPressed && isGrounded)
                {
                    // Start jump sequence
                    currentJumpState = JumpState.JumpStart;
                    animator.SetTrigger("jumpstart");
                    Debug.Log("Jump Start Animation");
                }
                break;
                
            case JumpState.JumpStart:
                if (!isGrounded)
                {
                    // Transition to loop when we leave ground
                    currentJumpState = JumpState.JumpLoop;
                    animator.SetTrigger("jumploop");
                    Debug.Log("Jump Loop Animation");
                }
                break;
                
            case JumpState.JumpLoop:
                if (isGrounded && velocity.y <= 0)
                {
                    // Landing detected
                    currentJumpState = JumpState.JumpEnd;
                    animator.SetTrigger("jumpend");
                    Debug.Log("Jump End Animation");
                }
                break;
                
            case JumpState.JumpEnd:
                // Wait for landing animation to finish, then return to grounded
                // You can add a timer here or use animation events
                if (isGrounded)
                {
                    currentJumpState = JumpState.Grounded;
                    Debug.Log("Back to Grounded State");
                }
                break;
        }
    }
    
    
    // For debugging
    void OnDrawGizmosSelected()
    {
        // Draw ground check sphere
        if (characterController != null)
        {
            Vector3 groundCheckPos = transform.position - Vector3.up * (characterController.height * 0.5f + groundCheckDistance);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos, 0.2f);
        }
    }
}
