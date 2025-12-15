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
    [SerializeField] private Transform target;
    [SerializeField] private CharacterController controller;
    [SerializeField] private TrainingArea trainingArea;
    
    [Header("Movement Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    [SerializeField] private float moveSpeed = -1f;
    [SerializeField] private float jumpForce = -1f;
    [SerializeField] private float gravity = -1f;
    
    private Vector3 startPos;
    private float episodeTimer;
    private float lastProgressZ;
    private Vector3 velocity;
    private int currentAction = 0;
    private bool justJumped = false;
    
    public int CurrentAction => currentAction;
    
    private float episodeReward = 0f;
    private int jumpCount = 0;
    private int forwardActionCount = 0;
    private int idleActionCount = 0;
    private int sprintActionCount = 0;
    private int rollActionCount = 0;
    private float maxDistanceReached = 0f;
    
    private int previousAction = -1;
    
    private float currentStamina = 100f;
    
    private float lastSprintEndTime = -1f;
    private float sprintCooldownDuration = 0.5f;
    
    private bool styleBonusEnabled = false;
    
    private bool isRolling = false;
    private float rollStartTime = 0f;
    private const float ROLL_DURATION = 0.6f;
    
    private int episodeNumber = 0;
    private int episodeTimestep = 0;
    
    public bool IsSprinting => currentAction == 3 && currentStamina > 0f;
    
    public bool IsRolling => isRolling;
    
    void OnDestroy()
    {

        if (TrainingLogger.Instance.IsInitialized())
        {
            TrainingLogger.Instance.FlushAll();
            Debug.Log("[ParkourAgent] Flushed all TrainingLogger data on destroy");
        }
    }
    
    public override void Initialize()
    {

        string debugId = System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ========== PARKOURAGENT INITIALIZE ==========");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Debug ID: {debugId}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.timeScale BEFORE: {Time.timeScale}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.fixedDeltaTime: {Time.fixedDeltaTime}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.deltaTime: {Time.deltaTime}");
        
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
        
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("ParkourAgent: CharacterController not found! Add CharacterController component to this GameObject.");
            }
        }
        
        if (target == null && trainingArea == null)
        {
            Debug.LogWarning("ParkourAgent: Target not assigned. If using TrainingArea, this will be auto-assigned. Otherwise, assign manually in Inspector.");
        }
        
        InitializeFromConfig();
        
        startPos = transform.position;
        lastProgressZ = startPos.x;
        
    }
    
    bool IsDemoMode()
    {

        string demoEnv = System.Environment.GetEnvironmentVariable("MLAGENTS_DEMO_MODE");
        if (!string.IsNullOrEmpty(demoEnv) && (demoEnv.ToLower() == "true" || demoEnv == "1"))
        {
            return true;
        }
        
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

                }
            }
        }
        
        return false;
    }
    
    void ApplyTimeScaleFromConfig(string debugId)
    {

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
        
        Debug.Log($"[ParkourAgent] Initialized - moveSpeed={moveSpeed}, jumpForce={jumpForce}, gravity={gravity}");
    }
    
    public override void OnEpisodeBegin()
    {
        Debug.Log($"[ParkourAgent] OnEpisodeBegin called for agent '{name}'");
        
        Debug.Log($"[TIMESCALE-DEBUG] OnEpisodeBegin - Time.timeScale: {Time.timeScale}, Time.fixedDeltaTime: {Time.fixedDeltaTime}, Time.deltaTime: {Time.deltaTime}");
        
        if (trainingArea != null)
        {
            trainingArea.ResetArea();
            transform.position = trainingArea.GetAgentSpawnPosition();
            target = trainingArea.GetTargetTransform();
            Debug.Log($"[ParkourAgent] Reset to position: {transform.position}, Target: {target?.position}, Target X: {target?.position.x:F1}");
            
            if (IsDemoMode())
            {
                InferenceVisualEnhancer enhancer = FindObjectOfType<InferenceVisualEnhancer>();
                if (enhancer != null)
                {
                    enhancer.UpdateFinishWall();
                    Debug.Log($"[ParkourAgent] Updated finish wall position after episode reset. Target X: {target?.position.x:F1}");
                }
            }
        }
        else
        {
            transform.position = startPos;
            Debug.Log($"[ParkourAgent] No TrainingArea, using startPos: {startPos}");
        }
        
        transform.rotation = Quaternion.Euler(0, 90, 0);
        controller.enabled = false;
        controller.enabled = true;
        
        episodeTimer = 0f;
        lastProgressZ = transform.position.x;
        startPos = transform.position;
        velocity = Vector3.zero;
        justJumped = false;
        
        episodeReward = 0f;
        jumpCount = 0;
        forwardActionCount = 0;
        idleActionCount = 0;
        sprintActionCount = 0;
        rollActionCount = 0;
        maxDistanceReached = 0f;
        
        CharacterConfig config = CharacterConfigManager.Config;
        currentStamina = config.maxStamina;
        
        lastSprintEndTime = -1f;
        previousAction = -1;
        
        styleBonusEnabled = Random.Range(0f, 1f) < config.styleEpisodeFrequency;
        if (styleBonusEnabled)
        {
            Debug.Log($"[ParkourAgent] Style bonus ENABLED for this episode (roll actions will receive +{config.rollStyleBonus} bonus)");
        }
        
        isRolling = false;
        rollStartTime = 0f;
        
        episodeNumber++;
        episodeTimestep = 0;
        
        TrainingLogger.Instance.Initialize();
        
        if (TrainingLogger.Instance.IsInitialized() && episodeNumber == 1)
        {
            TrainingLogger.Instance.SetMetadata(config.styleEpisodeFrequency);
        }
        
        if (TrainingLogger.Instance.IsInitialized())
        {
            int stepCount = Academy.Instance.StepCount;
            TrainingLogger.Instance.StartEpisode(episodeNumber, stepCount);
        }
        else
        {
            Debug.LogWarning($"[ParkourAgent] TrainingLogger not initialized. Episode {episodeNumber} will not be logged.");
        }
        
        Debug.Log($"[ParkourAgent] OnEpisodeBegin completed successfully");
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {

        if (Time.frameCount == 1 || Time.frameCount % 500 == 0)
        {
            if (target != null)
            {
                Vector3 toTarget = target.position - transform.position;
                Debug.Log($"[ParkourAgent] Observations - ToTarget: {toTarget}, Velocity: {controller.velocity}, Grounded: {controller.isGrounded}, Pos: {transform.position}");
            }
        }
        
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            sensor.AddObservation(toTarget);
            
            #if UNITY_EDITOR
            Vector3 toTargetNormalized = toTarget.normalized;
            Vector3 forward = transform.forward;
            float alignment = Vector3.Dot(forward, toTargetNormalized);

            if (alignment < 0.9f)
            {
                Debug.LogWarning($"[ParkourAgent] Raycast misalignment! Forward: {forward}, ToTarget: {toTargetNormalized}, Dot: {alignment:F3}");
            }
            #endif
        }
        else
        {

            sensor.AddObservation(Vector3.zero);
        }
        
        sensor.AddObservation(controller.velocity);
        
        sensor.AddObservation(controller.isGrounded ? 1f : 0f);
        
        CharacterConfig config = CharacterConfigManager.Config;
        float maxRayDist = 10f;
        float[] forwardDistances = { 2f, 4f, 6f, 8f, 10f };
        
        foreach (float forwardDist in forwardDistances)
        {
            Vector3 rayOrigin = transform.position + transform.forward * forwardDist + Vector3.up * 0.5f;
            RaycastHit hit;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxRayDist))
            {

                sensor.AddObservation(hit.distance / maxRayDist);
            }
            else
            {

                sensor.AddObservation(1f);
            }
        }

        RaycastHit obstacleHit;
        float raycastDist = config.obstacleRaycastDistance;
        float obstacleDistance = Physics.Raycast(transform.position, transform.forward, out obstacleHit, raycastDist) 
            ? obstacleHit.distance : raycastDist;
        sensor.AddObservation(obstacleDistance / raycastDist);
        
        float normalizedStamina = currentStamina / config.maxStamina;
        sensor.AddObservation(normalizedStamina);
        
    }
    
    void FixedUpdate()
    {

        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"[TIMESCALE-DEBUG] FixedUpdate Frame {Time.frameCount} - Time.timeScale: {Time.timeScale}, Time.fixedDeltaTime: {Time.fixedDeltaTime}, Time.deltaTime: {Time.deltaTime:F6}, realtimeSinceStartup: {Time.realtimeSinceStartup:F2}");
        }
        
        CharacterConfig config = CharacterConfigManager.Config;
        
        if (currentAction == 3 && currentStamina > 0f)
        {
            currentStamina -= config.staminaConsumptionRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
            
            if (currentStamina <= 0f)
            {
                currentAction = 2;
                lastSprintEndTime = Time.time;
            }
        }

        else if (currentAction != 3 && currentAction != 1 && currentAction != 4)
        {
            currentStamina += config.staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Min(config.maxStamina, currentStamina);
        }
        
        if (isRolling)
        {
            float rollElapsed = Time.time - rollStartTime;
            if (rollElapsed >= ROLL_DURATION)
            {
                isRolling = false;
            }
        }
        
        Vector3 horizontalMove = Vector3.zero;
        float currentMoveSpeed = moveSpeed;
        
        if (currentAction == 2)
        {
            currentMoveSpeed = moveSpeed;
            horizontalMove = transform.forward * currentMoveSpeed * Time.fixedDeltaTime;
        }
        else if (currentAction == 3 && currentStamina > 0f)
        {
            currentMoveSpeed = config.sprintSpeed;
            horizontalMove = transform.forward * currentMoveSpeed * Time.fixedDeltaTime;
        }
        
        if (justJumped)
        {
            Vector3 jumpBoost = transform.forward * config.jumpForwardBoost * Time.fixedDeltaTime;
            horizontalMove += jumpBoost;
            justJumped = false;
        }
        
        if (isRolling)
        {

            float rollSpeed = config.sprintSpeed * 1.5f;
            Vector3 rollMove = transform.forward * rollSpeed * Time.fixedDeltaTime;
            horizontalMove += rollMove;
        }
        
        if (Time.frameCount % 50 == 0 && horizontalMove.magnitude > 0.001f)
        {
            Debug.Log($"[ParkourAgent] Movement active! currentAction={currentAction}, speed={currentMoveSpeed}, stamina={currentStamina:F1}, horizontalMove={horizontalMove}");
        }
        
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }
        else if (velocity.y < 0)
        {

            velocity.y = config.groundedVelocityReset;
        }
        
        Vector3 finalMovement = horizontalMove + (velocity * Time.fixedDeltaTime);
        if (controller != null)
        {
            controller.Move(finalMovement);

            if (Time.frameCount % 100 == 0 && horizontalMove.magnitude > 0.001f)
            {
                Debug.Log($"[ParkourAgent] Movement applied! horizontalMove={horizontalMove}, finalMovement={finalMovement}, pos before={transform.position}");
            }
        }
        else
        {
            Debug.LogError("[ParkourAgent] CharacterController is NULL! Movement cannot be applied!");
        }
        
        if (transform.position.y < config.fallThreshold - 5f)
        {
            Debug.LogWarning($"[ParkourAgent] EMERGENCY FALL RESET! Agent y={transform.position.y:F2} < {config.fallThreshold - 5f}. Position: {transform.position}. Agent is falling through world!");
            
            if (DemoModeScreenFlash.Instance != null)
            {
                DemoModeScreenFlash.Instance.FlashRed();
            }
            
            EndEpisode();
        }
        
        #if UNITY_EDITOR
        DrawDebugRaycasts();
        #endif
    }
    
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

                Debug.DrawLine(rayOrigin, hit.point, Color.green);
                Debug.DrawLine(hit.point, rayOrigin + Vector3.down * maxRayDist, Color.red);
            }
            else
            {

                Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * maxRayDist, Color.red);
            }
        }
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {

        if (actions.DiscreteActions.Length == 0)
        {
            Debug.LogWarning("[ParkourAgent] Received invalid action buffer. Using default action (idle).");
            currentAction = 0;
            return;
        }
        
        CharacterConfig config = CharacterConfigManager.Config;
        
        currentAction = actions.DiscreteActions[0];
        
        if (currentAction == 3 && currentStamina <= 0f)
        {
            currentAction = 2;
        }
        
        if (currentAction == 3 && lastSprintEndTime >= 0f)
        {
            float timeSinceLastSprint = Time.time - lastSprintEndTime;
            if (timeSinceLastSprint < sprintCooldownDuration)
            {
                currentAction = 2;
            }
        }
        
        if (previousAction == 3 && currentAction != 3)
        {
            lastSprintEndTime = Time.time;
        }
        
        previousAction = currentAction;
        
        if (currentAction == 1)
        {
            if (currentStamina < config.jumpStaminaCost)
            {
                currentAction = 0;
            }
        }
        
        if (currentAction == 4)
        {
            if (currentStamina < config.rollStaminaCost)
            {
                currentAction = 0;
            }
            else if (isRolling)
            {
                currentAction = 0;
            }
        }
        
        if (Time.frameCount == 1 || Time.frameCount % 500 == 0)
        {
            BehaviorParameters bp = GetComponent<BehaviorParameters>();
            bool hasModel = bp != null && bp.Model != null;
            Debug.Log($"[ParkourAgent] Frame {Time.frameCount}: Action={currentAction}, HasModel={hasModel}, BehaviorType={bp?.BehaviorType}, Deterministic={bp?.DeterministicInference}");
        }
        
        if (Time.frameCount % 100 == 0)
        {
            Debug.Log($"[ParkourAgent] Action received: {currentAction} (0=idle, 1=jump, 2=jog, 3=sprint, 4=roll). Stamina: {currentStamina:F1}/{CharacterConfigManager.Config.maxStamina:F1}. Total actions - Idle: {idleActionCount}, Jump: {jumpCount}, Jog: {forwardActionCount}, Sprint: {sprintActionCount}, Roll: {rollActionCount}");
        }
        
        episodeTimer += Time.fixedDeltaTime;
        episodeTimestep++;
        
        int originalAction = actions.DiscreteActions[0];
        switch (originalAction)
        {
            case 0: idleActionCount++; break;
            case 1: jumpCount++; break;
            case 2: forwardActionCount++; break;
            case 3: sprintActionCount++; break;
            case 4: rollActionCount++; break;
        }
        
        int stepCount = Academy.Instance.StepCount;
        TrainingLogger.Instance.RecordStamina(episodeTimestep, currentStamina, stepCount);
        
        if (currentAction == 1 && controller.isGrounded)
        {
            TriggerJump();
        }
        
        if (currentAction == 4 && controller.isGrounded && !isRolling)
        {
            TriggerRoll();
        }
        
        float currentX = transform.position.x;
        float progressDelta = currentX - lastProgressZ;
        if (progressDelta > 0)
        {
            float progressReward = progressDelta * config.progressRewardMultiplier;
            AddReward(progressReward);
            episodeReward += progressReward;
            TrainingLogger.Instance.RecordRewardComponent("progress", progressReward);
        }
        lastProgressZ = currentX;
        
        float distanceFromStart = currentX - startPos.x;
        if (distanceFromStart > maxDistanceReached)
        {
            maxDistanceReached = distanceFromStart;
        }
        
        if (controller.isGrounded)
        {
            float groundedReward = 0.001f;
            AddReward(groundedReward);
            episodeReward += groundedReward;
            TrainingLogger.Instance.RecordRewardComponent("grounded", groundedReward);
        }
        
        float normalizedStamina = currentStamina / config.maxStamina;
        if (normalizedStamina < 0.2f)
        {
            AddReward(config.lowStaminaPenalty);
            episodeReward += config.lowStaminaPenalty;
            TrainingLogger.Instance.RecordRewardComponent("low_stamina_penalty", config.lowStaminaPenalty);
        }
        
        if (currentAction == 4 && isRolling)
        {

            AddReward(config.rollBaseReward);
            episodeReward += config.rollBaseReward;
            TrainingLogger.Instance.RecordRewardComponent("roll_base", config.rollBaseReward);
            
            if (styleBonusEnabled)
            {
                AddReward(config.rollStyleBonus);
                episodeReward += config.rollStyleBonus;
                TrainingLogger.Instance.RecordRewardComponent("roll_style", config.rollStyleBonus);
                if (Time.frameCount % 50 == 0)
                {
                    Debug.Log($"[ParkourAgent] Roll reward: Base={config.rollBaseReward}, Style={config.rollStyleBonus}, Total={config.rollBaseReward + config.rollStyleBonus}");
                }
            }
        }
        
        AddReward(config.timePenalty);
        episodeReward += config.timePenalty;
        TrainingLogger.Instance.RecordRewardComponent("time_penalty", config.timePenalty);
        
        if (target != null)
        {
            float distanceToTargetX = Mathf.Abs(transform.position.x - target.position.x);

            if (Time.frameCount % 50 == 0)
            {
                Debug.Log($"[ParkourAgent] Agent X: {transform.position.x:F1}, Target X: {target.position.x:F1}, Distance X: {distanceToTargetX:F1}");
            }
            
            if (distanceToTargetX < config.targetReachDistance)
            {
                Debug.Log($"[ParkourAgent] REACHED TARGET! Agent X: {transform.position.x:F2}, Target X: {target.position.x:F2}, Distance X: {distanceToTargetX:F2}");
                AddReward(config.targetReachReward);
                episodeReward += config.targetReachReward;
                TrainingLogger.Instance.RecordRewardComponent("target_reach", config.targetReachReward);
                
                stepCount = Academy.Instance.StepCount;
                TrainingLogger.Instance.EndEpisode(episodeNumber, episodeTimer, maxDistanceReached, "Success", stepCount);
                
                LogEpisodeStats("Success");
                
                if (DemoModeScreenFlash.Instance != null)
                {
                    DemoModeScreenFlash.Instance.FlashGreen();
                }
                
                if (IsDemoMode() && DemoModeRunCompleteMenu.Instance != null)
                {
                    DemoModeRunCompleteMenu.Instance.OnEpisodeComplete(
                        this, "Success", episodeReward, episodeTimer, maxDistanceReached,
                        jumpCount, forwardActionCount, sprintActionCount, idleActionCount, rollActionCount
                    );
                }
                
                EndEpisode();
            }
        }
        
        bool fell = transform.position.y < config.fallThreshold;
        bool timedOut = episodeTimer > config.episodeTimeout;
        if (fell || timedOut)
        {
            string reason = fell ? $"Fell (y={transform.position.y:F2} < {config.fallThreshold})" : $"Timeout (t={episodeTimer:F2} > {config.episodeTimeout})";
            string endReason = fell ? "Fell" : "Timeout";
            
            string debugInfo = $"[ParkourAgent] Episode ending: {reason}.\n";
            debugInfo += $"  Agent position: {transform.position}\n";
            debugInfo += $"  Timer: {episodeTimer:F2}s, Grounded: {controller.isGrounded}\n";
            
            if (target != null)
            {
                Vector3 toTarget = target.position - transform.position;
                float distanceToTarget = toTarget.magnitude;
                float distanceToTargetX = Mathf.Abs(transform.position.x - target.position.x);
                debugInfo += $"  Target position: {target.position}\n";
                debugInfo += $"  Distance to target (3D): {distanceToTarget:F2} units\n";
                debugInfo += $"  Distance to target (X-axis): {distanceToTargetX:F2} units\n";
                
                Transform finishWall = target.Find("FinishWall");
                if (finishWall == null)
                {

                    finishWall = target.Find("AnimusWall");
                }
                
                if (finishWall != null)
                {
                    Vector3 toWall = finishWall.position - transform.position;
                    float distanceToWall = toWall.magnitude;
                    float distanceToWallX = Mathf.Abs(transform.position.x - finishWall.position.x);
                    debugInfo += $"  Wall position: {finishWall.position}\n";
                    debugInfo += $"  Distance to wall (3D): {distanceToWall:F2} units\n";
                    debugInfo += $"  Distance to wall (X-axis): {distanceToWallX:F2} units\n";
                }
                else
                {

                    debugInfo += $"  Wall position: {target.position} (wall not found, using target position)\n";
                    debugInfo += $"  Distance to wall (X-axis): {distanceToTargetX:F2} units (using target distance)\n";
                }
            }
            else
            {
                debugInfo += $"  Target: NULL (not assigned)\n";
            }
            
            Debug.LogError(debugInfo);
            AddReward(config.fallPenalty);
            episodeReward += config.fallPenalty;
            TrainingLogger.Instance.RecordRewardComponent("fall_penalty", config.fallPenalty);
            
            int finalStepCount = Academy.Instance.StepCount;
            TrainingLogger.Instance.EndEpisode(episodeNumber, episodeTimer, maxDistanceReached, endReason, finalStepCount);
            
            LogEpisodeStats(endReason);
            
            if (DemoModeScreenFlash.Instance != null)
            {
                DemoModeScreenFlash.Instance.FlashRed();
            }
            
            if (IsDemoMode() && DemoModeRunCompleteMenu.Instance != null)
            {
                DemoModeRunCompleteMenu.Instance.OnEpisodeComplete(
                    this, endReason, episodeReward, episodeTimer, maxDistanceReached,
                    jumpCount, forwardActionCount, sprintActionCount, idleActionCount, rollActionCount
                );
            }
            
            EndEpisode();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;
        
        try
        {
            #if UNITY_EDITOR

            if (UnityEngine.Input.inputString != null)
            {
                if (UnityEngine.Input.GetKey(KeyCode.Space)) discreteActions[0] = 1;
                else if (UnityEngine.Input.GetKey(KeyCode.R)) discreteActions[0] = 4;
                else if (UnityEngine.Input.GetKey(KeyCode.LeftShift) && UnityEngine.Input.GetKey(KeyCode.W)) discreteActions[0] = 3;
                else if (UnityEngine.Input.GetKey(KeyCode.W)) discreteActions[0] = 2;
            }
            #endif
        }
        catch (System.InvalidOperationException)
        {

            discreteActions[0] = 0;
        }
    }
    
    private void TriggerJump()
    {
        if (controller.isGrounded)
        {
            CharacterConfig config = CharacterConfigManager.Config;

            if (currentStamina >= config.jumpStaminaCost)
            {
                currentStamina -= config.jumpStaminaCost;
                currentStamina = Mathf.Max(0f, currentStamina);
                velocity.y = jumpForce;

                justJumped = true;
            }

        }
    }
    
    private void TriggerRoll()
    {
        if (controller.isGrounded && !isRolling)
        {
            CharacterConfig config = CharacterConfigManager.Config;

            if (currentStamina >= config.rollStaminaCost)
            {
                currentStamina -= config.rollStaminaCost;
                currentStamina = Mathf.Max(0f, currentStamina);

                isRolling = true;
                rollStartTime = Time.time;
                Debug.Log($"[ParkourAgent] Roll triggered! Stamina cost: {config.rollStaminaCost}, Remaining: {currentStamina:F1}");
            }

        }
    }
    
    private void LogEpisodeStats(string endReason)
    {

        Academy.Instance.StatsRecorder.Add("Episode/TotalReward", episodeReward);
        Academy.Instance.StatsRecorder.Add("Episode/Length", episodeTimer);
        Academy.Instance.StatsRecorder.Add("Episode/MaxDistance", maxDistanceReached);
        Academy.Instance.StatsRecorder.Add("Actions/JumpCount", jumpCount);
        Academy.Instance.StatsRecorder.Add("Actions/JogCount", forwardActionCount);
        Academy.Instance.StatsRecorder.Add("Actions/SprintCount", sprintActionCount);
        Academy.Instance.StatsRecorder.Add("Actions/IdleCount", idleActionCount);
        
        int totalActions = jumpCount + forwardActionCount + sprintActionCount + idleActionCount + rollActionCount;
        if (totalActions > 0)
        {
            Academy.Instance.StatsRecorder.Add("Actions/JumpPercentage", (float)jumpCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/JogPercentage", (float)forwardActionCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/SprintPercentage", (float)sprintActionCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/RollPercentage", (float)rollActionCount / totalActions * 100f);
            Academy.Instance.StatsRecorder.Add("Actions/IdlePercentage", (float)idleActionCount / totalActions * 100f);
        }
        
        Academy.Instance.StatsRecorder.Add("Actions/RollCount", rollActionCount);
    }
    
}