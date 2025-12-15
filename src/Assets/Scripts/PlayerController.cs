using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public float moveSpeed = -1f;
    public float sprintSpeed = -1f;
    public float sprintAccelTime = -1f;
    private float currentSprintTime = 0f;
    private bool isMoving = false;
    public float jumpForce = -1f;
    public float jumpForwardBoost = -1f;
    public float gravity = -1f;
    
    [Header("Ground Detection")]
    public float groundCheckDistance = -1f;
    public LayerMask groundMask = 1;
    
    private CharacterController characterController;
    
    private Vector3 velocity;
    private Vector3 jumpMomentum;
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintPressed;
    
    private float mouseX = 0f;
    private float mouseSensitivity = -1f;
    public float verticalLookLimit = -1f;
    
    private Animator animator;
    
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
        
        mouseX = transform.eulerAngles.y;
        
        Cursor.lockState = CursorLockMode.Locked;
        
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

        if (ControlModeManager.Instance != null)
        {
            if (ControlModeManager.Instance.CurrentMode != ControlModeManager.ControlMode.Player)
            {
                return;
            }
        }

        HandleInput();
        HandleGroundDetection();
        HandleMovement();
        UpdateAnimations();
        HandleJumping();
    }
    
    void HandleInput()
    {

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null) return;
        
        CharacterConfig config = CharacterConfigManager.Config;
        
        moveInput.x = 0;
        moveInput.y = 0;
        
        if (keyboard.aKey.isPressed) moveInput.x = -1;
        if (keyboard.dKey.isPressed) moveInput.x = 1;
        if (keyboard.wKey.isPressed) moveInput.y = 1;
        if (keyboard.sKey.isPressed) moveInput.y = -1;
        
        if (keyboard.leftArrowKey.isPressed) moveInput.x = -1;
        if (keyboard.rightArrowKey.isPressed) moveInput.x = 1;
        if (keyboard.upArrowKey.isPressed) moveInput.y = 1;
        if (keyboard.downArrowKey.isPressed) moveInput.y = -1;
        
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            mouseX += mouseDelta.x * mouseSensitivity * config.mouseInputMultiplier;

        }
        
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }
        
        sprintPressed = keyboard.leftShiftKey.isPressed;
        
        isMoving = moveInput.magnitude > config.movementThreshold;
        
        if (sprintPressed && isMoving)
        {
            currentSprintTime = Mathf.Min(currentSprintTime + Time.deltaTime, sprintAccelTime);
        }
        else
        {
            currentSprintTime = Mathf.Max(currentSprintTime - Time.deltaTime * config.sprintDecelerationRate, 0f);
        }
        
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

        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            CharacterConfig config = CharacterConfigManager.Config;
            velocity.y = config.groundedVelocityReset;
            jumpMomentum = Vector3.zero;
        }
    }
    
    void HandleMovement()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        CameraFollow cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {

            transform.rotation = Quaternion.Euler(0, cameraFollow.HorizontalRotation, 0);
        }
        else
        {

            transform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
        
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        
        float sprintProgress = currentSprintTime / sprintAccelTime;
        float sprintCurve = sprintProgress * sprintProgress;
        float currentSprintSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, sprintCurve);
        
        float currentSpeed = (sprintPressed && isMoving) ? currentSprintSpeed : moveSpeed;
        
        Vector3 totalMovement = (moveDirection * currentSpeed) + jumpMomentum;
        characterController.Move(totalMovement * Time.deltaTime);
        
        jumpMomentum = Vector3.Lerp(jumpMomentum, Vector3.zero, Time.deltaTime * config.jumpMomentumDecayRate);
    }
    
    void HandleJumping()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        velocity.y += gravity * Time.deltaTime;
        
        if (jumpPressed && isGrounded)
        {
            velocity.y = jumpForce;
            
            if (moveInput.magnitude > config.movementThreshold)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                Vector3 jumpDirection = (forward * moveInput.y + right * moveInput.x).normalized;

                jumpMomentum = jumpDirection * jumpForwardBoost;
            }
            else
            {
                jumpMomentum = Vector3.zero;
            }
        }
        
        jumpPressed = false;
        
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        CharacterConfig config = CharacterConfigManager.Config;
        
        float speed = moveInput.magnitude;
        
        HandleJumpAnimations();
        
        if (currentJumpState == JumpState.Grounded)
        {

            animator.SetBool("IsJogging", isMoving);

            bool isActuallySprinting = sprintPressed && isMoving && (currentSprintTime > config.sprintAnimationThreshold);
            animator.SetBool("IsSprinting", isActuallySprinting);
        }
        else
        {

            animator.SetBool("IsJogging", false);
            animator.SetBool("IsSprinting", false);
        }
        
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", velocity.y);
        
        if (currentJumpState == JumpState.Grounded)
        {
            float animationSpeed = config.baseAnimationSpeed;
            if (sprintPressed && isMoving && currentSprintTime > config.sprintAnimationThreshold)
            {

                float sprintProgress = currentSprintTime / sprintAccelTime;
                float sprintCurve = sprintProgress * sprintProgress;
                float currentSprintSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, sprintCurve);
                animationSpeed = Mathf.Lerp(config.baseAnimationSpeed, config.maxSprintAnimationSpeed, (currentSprintSpeed - moveSpeed) / (sprintSpeed - moveSpeed));
            }
            animator.speed = animationSpeed;
        }
        else
        {
            animator.speed = config.baseAnimationSpeed;
        }
        
        wasGrounded = isGrounded;
    }
    
    void HandleJumpAnimations()
    {

        switch (currentJumpState)
        {
            case JumpState.Grounded:
                if (jumpPressed && isGrounded)
                {

                    currentJumpState = JumpState.JumpStart;
                    animator.SetTrigger("jumpstart");
                    Debug.Log("Jump Start Animation");
                }
                break;
                
            case JumpState.JumpStart:
                if (!isGrounded)
                {

                    currentJumpState = JumpState.JumpLoop;
                    animator.SetTrigger("jumploop");
                    Debug.Log("Jump Loop Animation");
                }
                break;
                
            case JumpState.JumpLoop:
                if (isGrounded && velocity.y <= 0)
                {

                    currentJumpState = JumpState.JumpEnd;
                    animator.SetTrigger("jumpend");
                    Debug.Log("Jump End Animation");
                }
                break;
                
            case JumpState.JumpEnd:

                if (isGrounded)
                {
                    currentJumpState = JumpState.Grounded;
                    Debug.Log("Back to Grounded State");
                }
                break;
        }
    }
    
    void OnDrawGizmosSelected()
    {

        if (characterController != null)
        {
            Vector3 groundCheckPos = transform.position - Vector3.up * (characterController.height * 0.5f + groundCheckDistance);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos, 0.2f);
        }
    }
}
