using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(DecisionRequester))]
public class ParkourAgent : Agent
{
    [Header("Required References")]
    [SerializeField] private Transform target; // Finish line - MUST BE ASSIGNED IN INSPECTOR
    [SerializeField] private CharacterController controller; // Auto-finds if not assigned
    [SerializeField] private TrainingArea trainingArea; // Auto-assigned by TrainingArea script
    
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    [SerializeField] private float moveSpeed = -1f; // -1 means use config
    [SerializeField] private float jumpForce = -1f; // -1 means use config
    [SerializeField] private float gravity = -1f; // -1 means use config
    
    private Vector3 startPos;
    private float episodeTimer;
    private float lastProgressZ; // Actually tracks X progress (name kept for compatibility)
    private Vector3 velocity; // For gravity and jumping
    private int currentAction = 0; // Store current action to apply in FixedUpdate
    
    // Public property for animation sync
    public int CurrentAction => currentAction;
    
    // Tracking metrics for better diagnostics
    private float episodeReward = 0f;
    private int jumpCount = 0;
    private int forwardActionCount = 0;
    private int idleActionCount = 0;
    private float maxDistanceReached = 0f;
    
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
        
        // Warn if target not assigned (will be auto-assigned by TrainingArea if in training mode)
        if (target == null && trainingArea == null)
        {
            Debug.LogWarning("ParkourAgent: Target not assigned. If using TrainingArea, this will be auto-assigned. Otherwise, assign manually in Inspector.");
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
        Debug.Log($"[ParkourAgent] OnEpisodeBegin called for agent '{name}'");
        
        // Reset training area (regenerate platforms if randomization enabled)
        if (trainingArea != null)
        {
            trainingArea.ResetArea();
            transform.position = trainingArea.GetAgentSpawnPosition();
            target = trainingArea.GetTargetTransform(); // Update target reference
            Debug.Log($"[ParkourAgent] Reset to position: {transform.position}, Target: {target?.position}");
        }
        else
        {
            transform.position = startPos; // Fallback for non-training scenarios
            Debug.Log($"[ParkourAgent] No TrainingArea, using startPos: {startPos}");
        }
        
        transform.rotation = Quaternion.Euler(0, 90, 0); // Face along X-axis where platforms are
        controller.enabled = false; // Prevent physics glitch
        controller.enabled = true;
        
        episodeTimer = 0f;
        lastProgressZ = transform.position.x; // Track X position for progress
        velocity = Vector3.zero;
        
        // Reset episode metrics
        episodeReward = 0f;
        jumpCount = 0;
        forwardActionCount = 0;
        idleActionCount = 0;
        maxDistanceReached = 0f;
        
        Debug.Log($"[ParkourAgent] OnEpisodeBegin completed successfully");
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Position relative to target (with null check)
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            sensor.AddObservation(toTarget); // 3 floats
            
            // Debug verification: Check raycasts align with target direction
            #if UNITY_EDITOR
            Vector3 toTargetNormalized = toTarget.normalized;
            Vector3 forward = transform.forward;
            float alignment = Vector3.Dot(forward, toTargetNormalized);
            // alignment should be close to 1.0 if perfectly aligned
            if (alignment < 0.9f)
            {
                Debug.LogWarning($"[ParkourAgent] Raycast misalignment! Forward: {forward}, ToTarget: {toTargetNormalized}, Dot: {alignment:F3}");
            }
            #endif
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
        
        // === PLATFORM DETECTION RAYCASTS ===
        // Multiple downward raycasts ahead to detect gaps and platform edges
        CharacterConfig config = CharacterConfigManager.Config;
        float maxRayDist = 10f; // How far down to check
        float[] forwardDistances = { 2f, 4f, 6f, 8f, 10f }; // Check at these distances ahead
        
        foreach (float forwardDist in forwardDistances)
        {
            Vector3 rayOrigin = transform.position + transform.forward * forwardDist + Vector3.up * 0.5f;
            RaycastHit hit;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxRayDist))
            {
                // Hit a platform - return normalized distance to platform
                sensor.AddObservation(hit.distance / maxRayDist); // 1 float per raycast
            }
            else
            {
                // No platform detected (gap or too far down)
                sensor.AddObservation(1f); // 1 float (1.0 = no platform)
            }
        }
        // 5 floats for platform detection
        
        // Forward obstacle raycast (for walls/obstacles, not gaps)
        RaycastHit obstacleHit;
        float raycastDist = config.obstacleRaycastDistance;
        float obstacleDistance = Physics.Raycast(transform.position, transform.forward, out obstacleHit, raycastDist) 
            ? obstacleHit.distance : raycastDist;
        sensor.AddObservation(obstacleDistance / raycastDist); // 1 float, normalized
        
        // Total: 3 + 3 + 1 + 5 + 1 = 13 observations
    }
    
    void FixedUpdate()
    {
        // Calculate horizontal movement
        Vector3 horizontalMove = Vector3.zero;
        if (currentAction == 2) // Move forward
        {
            horizontalMove = transform.forward * moveSpeed * Time.fixedDeltaTime;
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
            velocity.y = config.groundedVelocityReset;
        }
        
        // Combine horizontal movement and vertical velocity into ONE Move() call
        Vector3 finalMovement = horizontalMove + (velocity * Time.fixedDeltaTime);
        if (controller != null)
        {
            controller.Move(finalMovement);
        }
        
        // Emergency fall reset (backup if OnActionReceived not called)
        CharacterConfig configCheck = CharacterConfigManager.Config;
        if (transform.position.y < configCheck.fallThreshold - 5f)
        {
            Debug.LogWarning($"ParkourAgent '{name}': Emergency fall reset at y={transform.position.y}. Check training setup!");
            EndEpisode();
        }
        
        // Debug: Visualize raycasts (only in editor)
        #if UNITY_EDITOR
        DrawDebugRaycasts();
        #endif
    }
    
    /// <summary>
    /// Visualize the platform detection raycasts for debugging
    /// </summary>
    void DrawDebugRaycasts()
    {
        float maxRayDist = 10f;
        float[] forwardDistances = { 2f, 4f, 6f, 8f, 10f };
        
        foreach (float forwardDist in forwardDistances)
        {
            Vector3 rayOrigin = transform.position + transform.forward * forwardDist + Vector3.up * 0.5f;
            RaycastHit hit;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxRayDist))
            {
                // Hit platform - draw green to hit point, then red to max distance
                Debug.DrawLine(rayOrigin, hit.point, Color.green);
                Debug.DrawLine(hit.point, rayOrigin + Vector3.down * maxRayDist, Color.red);
            }
            else
            {
                // No hit - draw red line
                Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * maxRayDist, Color.red);
            }
        }
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Discrete actions: 0=nothing, 1=jump, 2=run forward
        currentAction = actions.DiscreteActions[0];
        
        episodeTimer += Time.fixedDeltaTime;
        
        // Track action distribution
        switch (currentAction)
        {
            case 0: idleActionCount++; break;
            case 1: jumpCount++; break;
            case 2: forwardActionCount++; break;
        }
        
        // Handle one-time actions (jump)
        if (currentAction == 1 && controller.isGrounded)
        {
            TriggerJump();
        }
        
        // REWARD SHAPING - This is critical
        CharacterConfig config = CharacterConfigManager.Config;
        
        // 1. Speed reward: forward progress (along X-axis where platforms are)
        float currentX = transform.position.x;
        float progressDelta = currentX - lastProgressZ; // lastProgressZ tracks X now
        if (progressDelta > 0)
        {
            float progressReward = progressDelta * config.progressRewardMultiplier;
            AddReward(progressReward);
            episodeReward += progressReward;
        }
        lastProgressZ = currentX;
        
        // Track max distance for diagnostics
        float distanceFromStart = currentX - startPos.x;
        if (distanceFromStart > maxDistanceReached)
        {
            maxDistanceReached = distanceFromStart;
        }
        
        // 2. Staying alive/on platform reward (encourages not falling)
        if (controller.isGrounded)
        {
            AddReward(0.001f); // Small reward per step for being on ground
            episodeReward += 0.001f;
        }
        
        // 3. Time penalty (encourages speed)
        AddReward(config.timePenalty);
        episodeReward += config.timePenalty;
        
        // 3. Reached target (only if target is assigned)
        if (target != null && Vector3.Distance(transform.position, target.position) < config.targetReachDistance)
        {
            AddReward(config.targetReachReward);
            episodeReward += config.targetReachReward;
            LogEpisodeStats("Success");
            EndEpisode();
        }
        
        // 4. Fell off / timeout
        if (transform.position.y < config.fallThreshold || episodeTimer > config.episodeTimeout)
        {
            AddReward(config.fallPenalty);
            episodeReward += config.fallPenalty;
            LogEpisodeStats(transform.position.y < config.fallThreshold ? "Fell" : "Timeout");
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
    /// Log episode statistics for diagnostics
    /// </summary>
    private void LogEpisodeStats(string endReason)
    {
        // Log custom stats to TensorBoard
        Academy.Instance.StatsRecorder.Add("Episode/TotalReward", episodeReward);
        Academy.Instance.StatsRecorder.Add("Episode/Length", episodeTimer);
        Academy.Instance.StatsRecorder.Add("Episode/MaxDistance", maxDistanceReached);
        Academy.Instance.StatsRecorder.Add("Actions/JumpCount", jumpCount);
        Academy.Instance.StatsRecorder.Add("Actions/ForwardCount", forwardActionCount);
        Academy.Instance.StatsRecorder.Add("Actions/IdleCount", idleActionCount);
        
        // Calculate action distribution percentages
        int totalActions = jumpCount + forwardActionCount + idleActionCount;
        if (totalActions > 0)
        {
            Academy.Instance.StatsRecorder.Add("Actions/JumpPercentage", (float)jumpCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/ForwardPercentage", (float)forwardActionCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/IdlePercentage", (float)idleActionCount / totalActions * 100f);
        }
    }
    
}