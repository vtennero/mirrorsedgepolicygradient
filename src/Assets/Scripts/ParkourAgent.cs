using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(CharacterController))]
public class ParkourAgent : Agent
{
    [Header("Required References")]
    [SerializeField] private Transform target; // Finish line - MUST BE ASSIGNED IN INSPECTOR
    [SerializeField] private CharacterController controller; // Auto-finds if not assigned
    
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    [SerializeField] private float moveSpeed = -1f; // -1 means use config
    [SerializeField] private float jumpForce = -1f; // -1 means use config
    [SerializeField] private float gravity = -1f; // -1 means use config
    
    private Vector3 startPos;
    private float episodeTimer;
    private float lastProgressZ;
    private Vector3 velocity; // For gravity and jumping
    
    public override void Initialize()
    {
        // Auto-find CharacterController if not assigned
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("ParkourAgent: CharacterController not found! Add CharacterController component to this GameObject.");
            }
        }
        
        // Warn if target not assigned
        if (target == null)
        {
            Debug.LogError("ParkourAgent: Target not assigned! Please assign a Transform (finish line) in the Inspector.");
        }
        
        // Initialize values from config if not overridden
        InitializeFromConfig();
        
        startPos = transform.position;
        lastProgressZ = startPos.z;
    }
    
    void InitializeFromConfig()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        if (moveSpeed < 0) moveSpeed = config.moveSpeed;
        if (jumpForce < 0) jumpForce = config.jumpForce;
        if (gravity < 0) gravity = config.gravity;
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset to start
        transform.position = startPos;
        transform.rotation = Quaternion.identity;
        controller.enabled = false; // Prevent physics glitch
        controller.enabled = true;
        
        episodeTimer = 0f;
        lastProgressZ = startPos.z;
        velocity = Vector3.zero; // Reset velocity
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Position relative to target (with null check)
        if (target != null)
        {
            sensor.AddObservation(target.position - transform.position); // 3 floats
        }
        else
        {
            // If no target, just observe zero vector (agent can't learn to reach target without it)
            sensor.AddObservation(Vector3.zero); // 3 floats
        }
        
        // Velocity
        sensor.AddObservation(controller.velocity); // 3 floats
        
        // Grounded state
        sensor.AddObservation(controller.isGrounded ? 1f : 0f); // 1 float
        
        // Forward obstacle raycast (simple version)
        CharacterConfig config = CharacterConfigManager.Config;
        RaycastHit hit;
        float raycastDist = config.obstacleRaycastDistance;
        float obstacleDistance = Physics.Raycast(transform.position, transform.forward, out hit, raycastDist) 
            ? hit.distance : raycastDist;
        sensor.AddObservation(obstacleDistance / raycastDist); // 1 float, normalized
        
        // Total: 8 observations
    }
    
    void FixedUpdate()
    {
        // Only process movement if in RL Agent mode
        if (ControlModeManager.Instance != null && 
            ControlModeManager.Instance.CurrentMode != ControlModeManager.ControlMode.RLAgent &&
            ControlModeManager.Instance.CurrentMode != ControlModeManager.ControlMode.Heuristic)
        {
            return; // Skip movement if not in RL mode
        }
        
        // Apply gravity continuously
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }
        else if (velocity.y < 0)
        {
            // Reset velocity when grounded
            CharacterConfig config = CharacterConfigManager.Config;
            velocity.y = config.groundedVelocityReset; // Small negative value to keep grounded
        }
        
        // Apply vertical movement (gravity/jumping)
        if (controller != null)
        {
            controller.Move(velocity * Time.fixedDeltaTime);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Only process actions if in RL Agent mode or Heuristic mode
        if (ControlModeManager.Instance != null)
        {
            var mode = ControlModeManager.Instance.CurrentMode;
            if (mode != ControlModeManager.ControlMode.RLAgent && 
                mode != ControlModeManager.ControlMode.Heuristic)
            {
                return;
            }
        }
        
        // Discrete actions: 0=nothing, 1=jump, 2=run forward
        int action = actions.DiscreteActions[0];
        
        episodeTimer += Time.fixedDeltaTime;
        
        // Map to movement code
        switch(action)
        {
            case 1: // Jump
                if (controller.isGrounded)
                {
                    TriggerJump();
                }
                break;
            case 2: // Run forward
                MoveForward();
                break;
        }
        
        // REWARD SHAPING - This is critical
        CharacterConfig config = CharacterConfigManager.Config;
        
        // 1. Speed reward: forward progress
        float currentZ = transform.position.z;
        float progressDelta = currentZ - lastProgressZ;
        if (progressDelta > 0) AddReward(progressDelta * config.progressRewardMultiplier);
        lastProgressZ = currentZ;
        
        // 2. Time penalty (encourages speed)
        AddReward(config.timePenalty);
        
        // 3. Reached target (only if target is assigned)
        if (target != null && Vector3.Distance(transform.position, target.position) < config.targetReachDistance)
        {
            AddReward(config.targetReachReward);
            EndEpisode();
        }
        
        // 4. Fell off / timeout
        if (transform.position.y < config.fallThreshold || episodeTimer > config.episodeTimeout)
        {
            AddReward(config.fallPenalty);
            EndEpisode();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control for testing
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0; // Default: nothing
        
        if (Input.GetKey(KeyCode.Space)) discreteActions[0] = 1; // Jump
        if (Input.GetKey(KeyCode.W)) discreteActions[0] = 2; // Forward
    }
    
    /// <summary>
    /// Triggers a jump if the agent is grounded.
    /// </summary>
    private void TriggerJump()
    {
        if (controller.isGrounded)
        {
            velocity.y = jumpForce;
        }
    }
    
    /// <summary>
    /// Moves the agent forward at moveSpeed.
    /// </summary>
    private void MoveForward()
    {
        if (controller != null)
        {
            Vector3 moveDirection = transform.forward * moveSpeed * Time.fixedDeltaTime;
            controller.Move(moveDirection);
        }
    }
}