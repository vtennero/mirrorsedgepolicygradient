using UnityEngine;

/// <summary>
/// Shared movement component that handles physics-based movement for both Player and RL Agent.
/// This avoids code duplication and ensures consistent movement behavior.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public float moveSpeed = -1f; // -1 means use config
    public float sprintSpeed = -1f; // -1 means use config
    public float jumpForce = -1f; // -1 means use config
    public float gravity = -1f; // -1 means use config
    
    private CharacterController characterController;
    private Vector3 velocity;
    
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public Vector3 Velocity => velocity;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        InitializeFromConfig();
    }
    
    void InitializeFromConfig()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        if (moveSpeed < 0) moveSpeed = config.moveSpeed;
        if (sprintSpeed < 0) sprintSpeed = config.sprintSpeed;
        if (jumpForce < 0) jumpForce = config.jumpForce;
        if (gravity < 0) gravity = config.gravity;
    }
    
    void Update()
    {
        // Apply gravity continuously
        if (!IsGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            // Reset velocity when grounded
            CharacterConfig config = CharacterConfigManager.Config;
            velocity.y = config.groundedVelocityReset; // Small negative value to keep grounded
        }
        
        // Apply vertical movement
        if (characterController != null)
        {
            characterController.Move(velocity * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Moves the character forward at normal speed.
    /// </summary>
    public void MoveForward()
    {
        MoveForward(moveSpeed);
    }
    
    /// <summary>
    /// Moves the character forward at specified speed.
    /// </summary>
    public void MoveForward(float speed)
    {
        if (characterController != null)
        {
            Vector3 moveDirection = transform.forward * speed * Time.deltaTime;
            characterController.Move(moveDirection);
        }
    }
    
    /// <summary>
    /// Moves the character in a specific direction.
    /// </summary>
    public void Move(Vector3 direction, float speed)
    {
        if (characterController != null)
        {
            Vector3 moveDirection = direction.normalized * speed * Time.deltaTime;
            characterController.Move(moveDirection);
        }
    }
    
    /// <summary>
    /// Makes the character jump if grounded.
    /// </summary>
    public void Jump()
    {
        if (IsGrounded)
        {
            velocity.y = jumpForce;
        }
    }
    
    /// <summary>
    /// Sets the vertical velocity directly (for jumping).
    /// </summary>
    public void SetVerticalVelocity(float verticalVelocity)
    {
        velocity.y = verticalVelocity;
    }
    
    /// <summary>
    /// Resets velocity (useful for resets).
    /// </summary>
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}

