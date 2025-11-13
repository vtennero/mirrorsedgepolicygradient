using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 12f; // 2x normal speed
    public float sprintAccelTime = 3f; // Time to reach max sprint speed
    private float currentSprintTime = 0f;
    private bool isMoving = false;
    public float jumpForce = 8f;
    public float gravity = -20f;
    
    [Header("Ground Detection")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask = 1;
    
    // Components
    private CharacterController characterController;
    
    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintPressed;
    
    // Mouse look variables
    private float mouseX = 0f;
    private float mouseY = 0f;
    private float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f; // Limit how far up/down you can look
    
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
    }
    
    void Update()
    {
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
        
        // Mouse look
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            mouseX += mouseDelta.x * mouseSensitivity * 0.1f;
            mouseY -= mouseDelta.y * mouseSensitivity * 0.1f; // Inverted Y for natural feel
            mouseY = Mathf.Clamp(mouseY, -verticalLookLimit, verticalLookLimit);
        }
        
        // Jump
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }
        
        // Sprint with progressive acceleration
        sprintPressed = keyboard.leftShiftKey.isPressed;
        
        // Check if player is moving
        isMoving = moveInput.magnitude > 0.1f;
        
        if (sprintPressed && isMoving)
        {
            currentSprintTime = Mathf.Min(currentSprintTime + Time.deltaTime, sprintAccelTime);
        }
        else
        {
            currentSprintTime = Mathf.Max(currentSprintTime - Time.deltaTime * 2f, 0f); // Decelerate faster
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
            velocity.y = -2f; // Small negative value to keep grounded
        }
    }
    
    void HandleMovement()
    {
        // Apply mouse rotation to player
        transform.rotation = Quaternion.Euler(mouseY, mouseX, 0); // Apply both vertical and horizontal rotation
        
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
        
        // Apply movement
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }
    
    void HandleJumping()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Handle jumping - ONLY when grounded and jump pressed
        if (jumpPressed && isGrounded)
        {
            velocity.y = jumpForce;
        }
        
        // Always reset jump input at end of frame
        jumpPressed = false;
        
        // Apply vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
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
            bool isActuallySprinting = sprintPressed && isMoving && (currentSprintTime > 0.5f);
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
            float animationSpeed = 1f;
            if (sprintPressed && isMoving && currentSprintTime > 0.5f)
            {
                // Calculate animation speed based on actual movement speed (capped for smoothness)
                float sprintProgress = currentSprintTime / sprintAccelTime;
                float sprintCurve = sprintProgress * sprintProgress;
                float currentSprintSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, sprintCurve);
                animationSpeed = Mathf.Lerp(1f, 1.5f, (currentSprintSpeed - moveSpeed) / (sprintSpeed - moveSpeed)); // Max 1.5x speed
            }
            animator.speed = animationSpeed;
        }
        else
        {
            animator.speed = 1f; // Normal speed for jump animations
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
