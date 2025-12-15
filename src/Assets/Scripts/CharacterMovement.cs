using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public float moveSpeed = -1f;
    public float sprintSpeed = -1f;
    public float jumpForce = -1f;
    public float gravity = -1f;
    
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

        if (!IsGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {

            CharacterConfig config = CharacterConfigManager.Config;
            velocity.y = config.groundedVelocityReset;
        }
        
        if (characterController != null)
        {
            characterController.Move(velocity * Time.deltaTime);
        }
    }
    
    public void MoveForward()
    {
        MoveForward(moveSpeed);
    }
    
    public void MoveForward(float speed)
    {
        if (characterController != null)
        {
            Vector3 moveDirection = transform.forward * speed * Time.deltaTime;
            characterController.Move(moveDirection);
        }
    }
    
    public void Move(Vector3 direction, float speed)
    {
        if (characterController != null)
        {
            Vector3 moveDirection = direction.normalized * speed * Time.deltaTime;
            characterController.Move(moveDirection);
        }
    }
    
    public void Jump()
    {
        if (IsGrounded)
        {
            velocity.y = jumpForce;
        }
    }
    
    public void SetVerticalVelocity(float verticalVelocity)
    {
        velocity.y = verticalVelocity;
    }
    
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}
