using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

/// <summary>
/// Syncs ParkourAgent actions with Faith model animations.
/// Handles run, jump (start/mid/end) animations based on agent state.
/// </summary>
public class AgentAnimationSync : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public ParkourAgent agent;
    
    private CharacterController controller;
    private Vector3 lastPosition;
    private bool wasGrounded = true;
    private bool isJumping = false;
    private float jumpStartTime = 0f;
    private const float JUMP_START_DURATION = 0.2f; // Time before transitioning to jump loop
    
    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<ParkourAgent>();
        }
        
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogWarning("AgentAnimationSync: No animator found!");
            enabled = false;
            return;
        }
        
        lastPosition = transform.position;
    }
    
    void Update()
    {
        if (animator == null || agent == null || controller == null) return;
        
        // Get current agent state
        bool isGrounded = controller.isGrounded;
        bool isMoving = IsMoving();
        int currentAction = GetCurrentAction();
        
        // Update animator parameters
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("Speed", isMoving ? 1f : 0f);
        animator.SetFloat("VerticalVelocity", controller.velocity.y);
        
        // Handle jump animations (3-part: start, loop, end)
        if (isGrounded)
        {
            if (isJumping)
            {
                // Landing - trigger jump end
                animator.SetTrigger("jumpend");
                isJumping = false;
                Debug.Log("Jump End Animation");
            }
            
            // Ground movement animations
            if (isMoving && currentAction == 2) // Action 2 = jog forward
            {
                animator.SetBool("IsJogging", true);
                animator.SetBool("IsSprinting", false);
                
            }
            else if (isMoving && currentAction == 3 && agent.IsSprinting) // Action 3 = sprint (only if actually sprinting, i.e., has stamina)
            {
                animator.SetBool("IsJogging", false);
                animator.SetBool("IsSprinting", true);
                
            }
            else
            {
                animator.SetBool("IsJogging", false);
                animator.SetBool("IsSprinting", false);
            }
        }
        else // In air
        {
            // Turn off ground movement
            animator.SetBool("IsJogging", false);
            animator.SetBool("IsSprinting", false);
            
            // Handle jump state transitions
            if (!wasGrounded && !isJumping)
            {
                // Just left ground - trigger jump start
                animator.SetTrigger("jumpstart");
                isJumping = true;
                jumpStartTime = Time.time;
                Debug.Log("Jump Start Animation");
            }
            else if (isJumping && Time.time - jumpStartTime > JUMP_START_DURATION)
            {
                // Transition to jump loop
                animator.SetTrigger("jumploop");
                Debug.Log("Jump Loop Animation");
                // Don't reset isJumping - wait for landing
            }
        }
        
        wasGrounded = isGrounded;
        lastPosition = transform.position;
    }
    
    bool IsMoving()
    {
        if (controller == null) return false;
        float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        return horizontalSpeed > 0.1f;
    }
    
    int GetCurrentAction()
    {
        // Get current action from ParkourAgent
        if (agent != null)
        {
            return agent.CurrentAction;
        }
        
        // Fallback: infer from movement
        if (controller != null)
        {
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            if (horizontalSpeed > 0.1f)
            {
                return 2; // Running forward
            }
        }
        return 0; // Idle
    }
}

