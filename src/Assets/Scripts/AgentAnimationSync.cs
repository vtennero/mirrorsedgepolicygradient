using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

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
    private const float JUMP_START_DURATION = 0.2f;
    private bool wasRolling = false;
    
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
        
        bool isGrounded = controller.isGrounded;
        bool isMoving = IsMoving();
        int currentAction = GetCurrentAction();
        
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("Speed", isMoving ? 1f : 0f);
        animator.SetFloat("VerticalVelocity", controller.velocity.y);
        
        if (isGrounded)
        {
            if (isJumping)
            {

                animator.SetTrigger("jumpend");
                isJumping = false;
                Debug.Log("Jump End Animation");
            }
            
            if (currentAction == 4 && agent.IsRolling)
            {
                animator.SetBool("IsJogging", false);
                animator.SetBool("IsSprinting", false);
                animator.SetBool("IsRolling", true);
                if (!wasRolling)
                {
                    animator.SetTrigger("rollstart");
                    Debug.Log("Roll Start Animation");
                }
            }
            else if (isMoving && currentAction == 2)
            {
                animator.SetBool("IsJogging", true);
                animator.SetBool("IsSprinting", false);
                animator.SetBool("IsRolling", false);
            }
            else if (isMoving && currentAction == 3 && agent.IsSprinting)
            {
                animator.SetBool("IsJogging", false);
                animator.SetBool("IsSprinting", true);
                animator.SetBool("IsRolling", false);
            }
            else
            {
                animator.SetBool("IsJogging", false);
                animator.SetBool("IsSprinting", false);
                animator.SetBool("IsRolling", false);
            }
            
            if (wasRolling && !agent.IsRolling)
            {
                animator.SetTrigger("rollend");
                Debug.Log("Roll End Animation");
            }
            
            wasRolling = agent.IsRolling;
        }
        else
        {

            animator.SetBool("IsJogging", false);
            animator.SetBool("IsSprinting", false);
            animator.SetBool("IsRolling", false);
            
            if (!wasGrounded && !isJumping)
            {

                animator.SetTrigger("jumpstart");
                isJumping = true;
                jumpStartTime = Time.time;
                Debug.Log("Jump Start Animation");
            }
            else if (isJumping && Time.time - jumpStartTime > JUMP_START_DURATION)
            {

                animator.SetTrigger("jumploop");
                Debug.Log("Jump Loop Animation");

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

        if (agent != null)
        {
            return agent.CurrentAction;
        }
        
        if (controller != null)
        {
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            if (horizontalSpeed > 0.1f)
            {
                return 2;
            }
        }
        return 0;
    }
}
