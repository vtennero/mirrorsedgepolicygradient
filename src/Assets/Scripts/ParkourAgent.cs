using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

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
    private bool justJumped = false; // Track if we just jumped to apply horizontal boost
    
    // Public property for animation sync
    public int CurrentAction => currentAction;
    
    // Tracking metrics for better diagnostics
    private float episodeReward = 0f;
    private int jumpCount = 0;
    private int forwardActionCount = 0;
    private int idleActionCount = 0;
    private int sprintActionCount = 0;
    private float maxDistanceReached = 0f;
    
    // Stamina system
    private float currentStamina = 100f;
    
    // Public property for animation sync (checks if actually sprinting, considering stamina)
    public bool IsSprinting => currentAction == 3 && currentStamina > 0f;
    
    public override void Initialize()
    {
        // TIMESCALE DEBUG: Log actual time scale at initialization
        string debugId = System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ========== PARKOURAGENT INITIALIZE ==========");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Debug ID: {debugId}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.timeScale BEFORE: {Time.timeScale}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.fixedDeltaTime: {Time.fixedDeltaTime}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.deltaTime: {Time.deltaTime}");
        
        // DEMO MODE ONLY: Apply custom time scale for inference/demo viewing
        // ⚠️ CRITICAL: This ONLY runs in demo mode (MLAGENTS_DEMO_MODE=true)
        // ⚠️ Training is COMPLETELY UNAFFECTED - this code is skipped during training
        // ML-Agents doesn't apply engine_settings.time_scale in Editor mode, so we read it manually
        if (IsDemoMode())
        {
            Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Demo mode detected - applying custom time scale");
            ApplyTimeScaleFromConfig(debugId);
        }
        else
        {
            Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Training mode - NOT applying custom time scale (training unaffected)");
        }
        
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.timeScale AFTER: {Time.timeScale}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ===========================================");
        
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
        lastProgressZ = startPos.x; // Fixed: track X, not Z
    }
    
    /// <summary>
    /// Checks if demo mode is enabled (MLAGENTS_DEMO_MODE=true).
    /// DEMO MODE ONLY - Training is completely unaffected.
    /// </summary>
    bool IsDemoMode()
    {
        // 1. Check environment variable (if manually set)
        string demoEnv = System.Environment.GetEnvironmentVariable("MLAGENTS_DEMO_MODE");
        if (!string.IsNullOrEmpty(demoEnv) && (demoEnv.ToLower() == "true" || demoEnv == "1"))
        {
            return true;
        }
        
        // 2. Check demo_mode.env file - try multiple paths
        string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
        string srcFolder = System.IO.Path.Combine(projectRoot, "src");
        
        string[] possiblePaths = {
            System.IO.Path.Combine(srcFolder, "demo_mode.env"),
            System.IO.Path.Combine(projectRoot, "demo_mode.env"),
            System.IO.Path.Combine(Application.dataPath, "..", "src", "demo_mode.env"),
            System.IO.Path.Combine(Application.dataPath, "..", "demo_mode.env"),
            System.IO.Path.Combine(Application.streamingAssetsPath, "demo_mode.env")
        };
        
        foreach (string path in possiblePaths)
        {
            string normalizedPath = System.IO.Path.GetFullPath(path);
            if (System.IO.File.Exists(normalizedPath))
            {
                try
                {
                    string content = System.IO.File.ReadAllText(normalizedPath);
                    if (content.Contains("MLAGENTS_DEMO_MODE=true") || content.Contains("MLAGENTS_DEMO_MODE=1"))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Continue checking other paths
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Applies time_scale from config file for DEMO/INFERENCE MODE ONLY.
    /// ⚠️ CRITICAL: This function is ONLY called when IsDemoMode() returns true.
    /// ⚠️ Training is COMPLETELY UNAFFECTED - this code never runs during training.
    /// </summary>
    void ApplyTimeScaleFromConfig(string debugId)
    {
        // METHOD 1: Read from TIMESCALE.txt file (written by Python script before ML-Agents starts)
        // This file is only created by run_inference.py, which is only used for demo/inference
        string timescaleFile = System.IO.Path.Combine(Application.dataPath, "..", "TIMESCALE.txt");
        if (System.IO.File.Exists(timescaleFile))
        {
            try
            {
                string content = System.IO.File.ReadAllText(timescaleFile).Trim();
                if (float.TryParse(content, out float timeScaleValue))
                {
                    Time.timeScale = timeScaleValue;
                    Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ✓ Applied time_scale from TIMESCALE.txt: {timeScaleValue} (DEMO MODE ONLY)");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TIMESCALE-DEBUG-{debugId}] Could not read TIMESCALE.txt: {e.Message}");
            }
        }
        
        // METHOD 2: Fallback - read from most recent inference config
        // Only inference runs create inference_* directories, so this is safe
        try
        {
            string resultsPath = System.IO.Path.Combine(Application.dataPath, "..", "results");
            if (System.IO.Directory.Exists(resultsPath))
            {
                var inferenceDirs = System.IO.Directory.GetDirectories(resultsPath, "inference_*");
                if (inferenceDirs.Length > 0)
                {
                    System.Array.Sort(inferenceDirs, (a, b) => 
                        System.IO.File.GetLastWriteTime(b).CompareTo(System.IO.File.GetLastWriteTime(a)));
                    
                    string latestConfigPath = System.IO.Path.Combine(inferenceDirs[0], "configuration.yaml");
                    if (System.IO.File.Exists(latestConfigPath))
                    {
                        string configContent = System.IO.File.ReadAllText(latestConfigPath);
                        var timeScaleMatch = System.Text.RegularExpressions.Regex.Match(
                            configContent, @"time_scale:\s*([0-9.eE+-]+)");
                        
                        if (timeScaleMatch.Success && float.TryParse(timeScaleMatch.Groups[1].Value, out float timeScaleValue))
                        {
                            Time.timeScale = timeScaleValue;
                            Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ✓ Applied time_scale from config: {timeScaleValue} (DEMO MODE ONLY)");
                            return;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TIMESCALE-DEBUG-{debugId}] Could not read from results: {e.Message}");
        }
        
        Debug.LogWarning($"[TIMESCALE-DEBUG-{debugId}] ⚠ Using default Time.timeScale: {Time.timeScale} (DEMO MODE - no custom time scale found)");
    }
    
    void InitializeFromConfig()
    {
        CharacterConfig config = CharacterConfigManager.Config;
        
        if (moveSpeed < 0) moveSpeed = config.moveSpeed;
        if (jumpForce < 0) jumpForce = config.jumpForce;
        if (gravity < 0) gravity = config.gravity;
        
        // Debug: Log initialized values
        Debug.Log($"[ParkourAgent] Initialized - moveSpeed={moveSpeed}, jumpForce={jumpForce}, gravity={gravity}");
    }
    
    public override void OnEpisodeBegin()
    {
        Debug.Log($"[ParkourAgent] OnEpisodeBegin called for agent '{name}'");
        
        // TIMESCALE DEBUG: Log time scale at episode start
        Debug.Log($"[TIMESCALE-DEBUG] OnEpisodeBegin - Time.timeScale: {Time.timeScale}, Time.fixedDeltaTime: {Time.fixedDeltaTime}, Time.deltaTime: {Time.deltaTime}");
        
        // Reset training area (regenerate platforms if randomization enabled)
        if (trainingArea != null)
        {
            trainingArea.ResetArea();
            transform.position = trainingArea.GetAgentSpawnPosition();
            target = trainingArea.GetTargetTransform(); // Update target reference
            Debug.Log($"[ParkourAgent] Reset to position: {transform.position}, Target: {target?.position}, Target X: {target?.position.x:F1}");
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
        startPos = transform.position; // Update startPos to current spawn position
        velocity = Vector3.zero;
        justJumped = false; // Reset jump flag
        
        // Reset episode metrics
        episodeReward = 0f;
        jumpCount = 0;
        forwardActionCount = 0;
        idleActionCount = 0;
        sprintActionCount = 0;
        maxDistanceReached = 0f;
        
        // Reset stamina to max
        CharacterConfig config = CharacterConfigManager.Config;
        currentStamina = config.maxStamina;
        
        Debug.Log($"[ParkourAgent] OnEpisodeBegin completed successfully");
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Debug: Log observations occasionally to verify they're not all zeros
        if (Time.frameCount == 1 || Time.frameCount % 500 == 0)
        {
            if (target != null)
            {
                Vector3 toTarget = target.position - transform.position;
                Debug.Log($"[ParkourAgent] Observations - ToTarget: {toTarget}, Velocity: {controller.velocity}, Grounded: {controller.isGrounded}, Pos: {transform.position}");
            }
        }
        
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
        
        // Stamina (normalized: 0.0 to 1.0)
        float normalizedStamina = currentStamina / config.maxStamina;
        sensor.AddObservation(normalizedStamina); // 1 float
        
        // Total: 3 + 3 + 1 + 5 + 1 + 1 = 14 observations
    }
    
    void FixedUpdate()
    {
        // TIMESCALE DEBUG: Log every 300 frames (every ~6 seconds at 50Hz)
        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"[TIMESCALE-DEBUG] FixedUpdate Frame {Time.frameCount} - Time.timeScale: {Time.timeScale}, Time.fixedDeltaTime: {Time.fixedDeltaTime}, Time.deltaTime: {Time.deltaTime:F6}, realtimeSinceStartup: {Time.realtimeSinceStartup:F2}");
        }
        
        // === STAMINA SYSTEM (in physics step) ===
        CharacterConfig config = CharacterConfigManager.Config;
        
        // Consume stamina while sprinting (action 3)
        if (currentAction == 3 && currentStamina > 0f)
        {
            currentStamina -= config.staminaConsumptionRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Max(0f, currentStamina); // Clamp to 0
            
            // If stamina depleted during sprint, fall back to jog
            if (currentStamina <= 0f)
            {
                currentAction = 2; // Fall back to jog
            }
        }
        // Regenerate stamina when not sprinting and not jumping
        else if (currentAction != 3 && currentAction != 1)
        {
            currentStamina += config.staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Min(config.maxStamina, currentStamina); // Clamp to max
        }
        
        // Calculate horizontal movement
        Vector3 horizontalMove = Vector3.zero;
        float currentMoveSpeed = moveSpeed; // Default to jog speed
        
        if (currentAction == 2) // Jog forward
        {
            currentMoveSpeed = moveSpeed;
            horizontalMove = transform.forward * currentMoveSpeed * Time.fixedDeltaTime;
        }
        else if (currentAction == 3 && currentStamina > 0f) // Sprint forward (only if stamina available)
        {
            currentMoveSpeed = config.sprintSpeed;
            horizontalMove = transform.forward * currentMoveSpeed * Time.fixedDeltaTime;
        }
        
        // Apply jump forward boost if we just jumped (for more horizontal, human-like jumps)
        if (justJumped)
        {
            Vector3 jumpBoost = transform.forward * config.jumpForwardBoost * Time.fixedDeltaTime;
            horizontalMove += jumpBoost;
            justJumped = false; // Reset flag after applying boost
        }
        
        // Debug: Log when movement action is taken
        if (Time.frameCount % 50 == 0 && horizontalMove.magnitude > 0.001f)
        {
            Debug.Log($"[ParkourAgent] Movement active! currentAction={currentAction}, speed={currentMoveSpeed}, stamina={currentStamina:F1}, horizontalMove={horizontalMove}");
        }
        
        // Apply gravity continuously
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }
        else if (velocity.y < 0)
        {
            // Reset velocity when grounded
            velocity.y = config.groundedVelocityReset;
        }
        
        // Combine horizontal movement and vertical velocity into ONE Move() call
        Vector3 finalMovement = horizontalMove + (velocity * Time.fixedDeltaTime);
        if (controller != null)
        {
            controller.Move(finalMovement);
            // Debug: Log actual movement applied
            if (Time.frameCount % 100 == 0 && horizontalMove.magnitude > 0.001f)
            {
                Debug.Log($"[ParkourAgent] Movement applied! horizontalMove={horizontalMove}, finalMovement={finalMovement}, pos before={transform.position}");
            }
        }
        else
        {
            Debug.LogError("[ParkourAgent] CharacterController is NULL! Movement cannot be applied!");
        }
        
        // Emergency fall reset (backup if OnActionReceived not called)
        if (transform.position.y < config.fallThreshold - 5f)
        {
            Debug.LogWarning($"[ParkourAgent] EMERGENCY FALL RESET! Agent y={transform.position.y:F2} < {config.fallThreshold - 5f}. Position: {transform.position}. Agent is falling through world!");
            
            // Flash red screen in demo mode
            if (DemoModeScreenFlash.Instance != null)
            {
                DemoModeScreenFlash.Instance.FlashRed();
            }
            
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
        // Get config once at the start of the method
        CharacterConfig config = CharacterConfigManager.Config;
        
        // Discrete actions: 0=idle, 1=jump, 2=jog forward, 3=sprint forward
        currentAction = actions.DiscreteActions[0];
        
        // Block sprint (action 3) if no stamina - fall back to jog (action 2)
        if (currentAction == 3 && currentStamina <= 0f)
        {
            currentAction = 2; // Fall back to jog
        }
        
        // Block jump (action 1) if insufficient stamina
        if (currentAction == 1)
        {
            if (currentStamina < config.jumpStaminaCost)
            {
                currentAction = 0; // Block jump, do nothing
            }
        }
        
        // Debug: Check if we're in inference and log model status
        if (Time.frameCount == 1 || Time.frameCount % 500 == 0)
        {
            BehaviorParameters bp = GetComponent<BehaviorParameters>();
            bool hasModel = bp != null && bp.Model != null;
            Debug.Log($"[ParkourAgent] Frame {Time.frameCount}: Action={currentAction}, HasModel={hasModel}, BehaviorType={bp?.BehaviorType}, Deterministic={bp?.DeterministicInference}");
        }
        
        // Debug: Log action distribution every 100 steps
        if (Time.frameCount % 100 == 0)
        {
            Debug.Log($"[ParkourAgent] Action received: {currentAction} (0=idle, 1=jump, 2=jog, 3=sprint). Stamina: {currentStamina:F1}/{CharacterConfigManager.Config.maxStamina:F1}. Total actions - Idle: {idleActionCount}, Jump: {jumpCount}, Jog: {forwardActionCount}, Sprint: {sprintActionCount}");
        }
        
        episodeTimer += Time.fixedDeltaTime;
        
        // Track action distribution (track original action before blocking)
        int originalAction = actions.DiscreteActions[0];
        switch (originalAction)
        {
            case 0: idleActionCount++; break;
            case 1: jumpCount++; break;
            case 2: forwardActionCount++; break;
            case 3: sprintActionCount++; break;
        }
        
        // Handle one-time actions (jump) - only if we have stamina
        if (currentAction == 1 && controller.isGrounded)
        {
            TriggerJump();
        }
        
        // REWARD SHAPING - This is critical
        
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
        // Use X-axis distance only (not 3D) to avoid issues when agent passes target at different Y height
        if (target != null)
        {
            float distanceToTargetX = Mathf.Abs(transform.position.x - target.position.x);
            // Debug: Log target position and distance every 50 steps
            if (Time.frameCount % 50 == 0)
            {
                Debug.Log($"[ParkourAgent] Agent X: {transform.position.x:F1}, Target X: {target.position.x:F1}, Distance X: {distanceToTargetX:F1}");
            }
            
            if (distanceToTargetX < config.targetReachDistance)
            {
                Debug.Log($"[ParkourAgent] REACHED TARGET! Agent X: {transform.position.x:F2}, Target X: {target.position.x:F2}, Distance X: {distanceToTargetX:F2}");
                AddReward(config.targetReachReward);
                episodeReward += config.targetReachReward;
                LogEpisodeStats("Success");
                
                // Flash green screen in demo mode
                if (DemoModeScreenFlash.Instance != null)
                {
                    DemoModeScreenFlash.Instance.FlashGreen();
                }
                
                EndEpisode();
            }
        }
        
        // 4. Fell off / timeout
        bool fell = transform.position.y < config.fallThreshold;
        bool timedOut = episodeTimer > config.episodeTimeout;
        if (fell || timedOut)
        {
            string reason = fell ? $"Fell (y={transform.position.y:F2} < {config.fallThreshold})" : $"Timeout (t={episodeTimer:F2} > {config.episodeTimeout})";
            Debug.LogError($"[ParkourAgent] Episode ending: {reason}. Agent pos: {transform.position}, Timer: {episodeTimer:F2}s, Grounded: {controller.isGrounded}");
            AddReward(config.fallPenalty);
            episodeReward += config.fallPenalty;
            LogEpisodeStats(fell ? "Fell" : "Timeout");
            
            // Flash red screen in demo mode
            if (DemoModeScreenFlash.Instance != null)
            {
                DemoModeScreenFlash.Instance.FlashRed();
            }
            
            EndEpisode();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control for testing (only works in Editor with old Input System)
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0; // Default: nothing
        
        // Try to use old Input System (only works if not using new Input System package)
        // During training, this won't be called anyway (agent uses trained policy)
        try
        {
            #if UNITY_EDITOR
            // Only try input in editor, and only if old Input System is active
            if (UnityEngine.Input.inputString != null) // Check if old Input System is available
            {
                if (UnityEngine.Input.GetKey(KeyCode.Space)) discreteActions[0] = 1; // Jump
                else if (UnityEngine.Input.GetKey(KeyCode.LeftShift) && UnityEngine.Input.GetKey(KeyCode.W)) discreteActions[0] = 3; // Sprint
                else if (UnityEngine.Input.GetKey(KeyCode.W)) discreteActions[0] = 2; // Jog
            }
            #endif
        }
        catch (System.InvalidOperationException)
        {
            // New Input System is active - heuristic not available, just return default (0)
            discreteActions[0] = 0;
        }
    }
    
    /// <summary>
    /// Triggers a jump if the agent is grounded and has sufficient stamina.
    /// </summary>
    private void TriggerJump()
    {
        if (controller.isGrounded)
        {
            CharacterConfig config = CharacterConfigManager.Config;
            // Consume stamina for jump (in physics step, but triggered here)
            if (currentStamina >= config.jumpStaminaCost)
            {
                currentStamina -= config.jumpStaminaCost;
                currentStamina = Mathf.Max(0f, currentStamina); // Clamp to 0
                velocity.y = jumpForce;
                // Set flag to apply horizontal boost in FixedUpdate (for more human-like, horizontal jumps)
                justJumped = true;
            }
            // If insufficient stamina, jump is blocked (already handled in OnActionReceived)
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
        Academy.Instance.StatsRecorder.Add("Actions/JogCount", forwardActionCount);
        Academy.Instance.StatsRecorder.Add("Actions/SprintCount", sprintActionCount);
        Academy.Instance.StatsRecorder.Add("Actions/IdleCount", idleActionCount);
        
        // Calculate action distribution percentages
        int totalActions = jumpCount + forwardActionCount + sprintActionCount + idleActionCount;
        if (totalActions > 0)
        {
            Academy.Instance.StatsRecorder.Add("Actions/JumpPercentage", (float)jumpCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/JogPercentage", (float)forwardActionCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/SprintPercentage", (float)sprintActionCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/IdlePercentage", (float)idleActionCount / totalActions * 100f);
        }
    }
    
}