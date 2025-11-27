CODE CHANGES (C# Scripts)
New Script: TrainingArea.cs
Self-contained training environment
Spawns its own platforms (or references them as children)
Auto-assigns target to agent
Handles episode reset
Effort: ~100-150 lines, straightforward
Modify: ParkourAgent.cs
Remove ControlModeManager checks (lines that check CurrentMode)
Add public TrainingArea trainingArea; reference
Change target assignment to get from TrainingArea
Make sure OnEpisodeBegin() resets position correctly
Effort: Delete ~5-10 lines, add ~10-20 lines
Optional: Mode Selection Script
Simple scene loader: "Play Game" button → SampleScene, "Train" button → TrainingScene
Effort: ~30 lines + UI setup
UNITY EDITOR WORK (Scene/Prefab Setup)
Create New Scene: TrainingScene.unity
Empty scene (no GameManager, no SetupDemo, no ControlModeManager)
Effort: 2 minutes
Create Prefab: TrainingArea.prefab
Root GameObject with TrainingArea.cs script
Child: Static platforms (not procedural - fixed layout)
Child: Agent spawn point (empty GameObject)
Child: Target position (empty GameObject)
Child: Your agent prefab (with ParkourAgent)
Effort: 15-20 minutes to build and test
Place Multiple TrainingArea Instances
Drag TrainingArea prefab into TrainingScene
Duplicate it (Ctrl+D) 10-20 times
Space them out (e.g., 50 units apart on X-axis)
Effort: 5 minutes
Configure Agent Prefab
Make sure agent has ParkourAgent + CharacterController
Remove PlayerController from training agent variant
Remove camera references
Effort: 5 minutes
Optional: Main Menu Scene
UI Canvas with two buttons
Hook up scene loading
Effort: 10-15 minutes
👁️ MONITORING TRAINING (STANDARD PRACTICES)
During Training:
TensorBoard (Standard Tool):
ML-Agents automatically logs to TensorBoard
Run: tensorboard --logdir results/
Shows real-time graphs: reward, episode length, loss
This is the standard way - everyone uses this
You see metrics, not visuals
Unity Editor Monitoring (Optional):
You CAN run training in the Editor (not headless)
Graphics render, you see all agents moving
Massive performance hit - training is ~10x slower
Useful for: debugging why agents are stuck/falling
Command: mlagents-learn parkour_config.yaml --run-id=debug (then press Play in Editor)
Standard Practice:
Most training: headless mode (no graphics, TensorBoard only)
Debugging: run in Editor with graphics if something is broken
You DON'T watch agents in real-time normally - too slow
After Training:
Inference Test (Standard):
Load the trained model (.onnx file)
Run in single-player scene with graphics
See the trained agent perform
Command: Agent loads model via Model field in inspector
How This Works:
Train headless: mlagents-learn parkour_config.yaml --run-id=parkour_v1
Training runs for hours, you watch TensorBoard graphs
Training completes, saves parkour_v1.onnx model
Create "Demo Scene" with single agent + graphics
Assign parkour_v1.onnx to ParkourAgent's Model field
Press Play - watch trained agent perform
Realistic Expectations:
You will NOT watch training visually (too slow)
You WILL run quick Editor tests to debug if metrics look wrong
You WILL test final model in pretty 3D scene after training
📊 WORK BREAKDOWN
Task	Type	Effort	Critical?
Create TrainingArea.cs	Code	2-3 hours	YES
Modify ParkourAgent.cs	Code	30 min	YES
Build TrainingArea prefab	Unity	30 min	YES
Create TrainingScene	Unity	5 min	YES
Place multiple areas	Unity	5 min	YES
Remove ControlMode checks	Code	15 min	YES
Mode selection menu	Both	1 hour	NO (nice to have)
Demo/inference scene	Unity	20 min	NO (do after training)
Total Critical Work: ~4-5 hours
What's Code vs Unity:
Code: ~30% (TrainingArea script, modify ParkourAgent)
Unity Editor: ~70% (scene setup, prefabs, placement, configuration)
⚠️ HONEST WARNINGS
First training run will probably fail
Reward function might be wrong
Agent might get stuck
Hyperparameters might need tuning
This is normal - everyone iterates
You'll need to iterate on TrainingArea
Platform spacing might be too hard/easy
Agent spawn position might need adjustment
You'll rebuild this prefab 5-10 times
TensorBoard is your main tool
If you need to "see what's happening" every 5 minutes, training is too slow
Standard workflow: check TensorBoard every 30-60 min
Only open Unity Editor if graphs look completely broken
The "glimpse" you want doesn't really exist mid-training
You either: train headless (fast, no visuals) OR train in Editor (slow, visual)
No "take a 10-second visual peek then go back to fast training"
Best compromise: checkpoint every N steps, test checkpoint in demo scene
🎯 BOTTOM LINE
Your approach is exactly correct:
Keep single-player scene as-is ✓
Build separate training infrastructure ✓
Reuse components (ParkourAgent, CharacterController, configs) ✓

---

# STEP-BY-STEP IMPLEMENTATION PLAN (NO BULLSHIT)

## 📋 PROGRESS TRACKER

**Status Legend:**
- [ ] = Not started
- 🔄 = Ongoing/In progress
- ✅ = Complete and verified

**Overall Progress:**
- [ ] Phase 1: Code Changes (3 tasks)
- [ ] Phase 2: Unity Editor Setup (5 tasks)
- [ ] Phase 3: Verification Checklist (2 tasks)
- [ ] Phase 4: Run Training (2 tasks)

**Quick Status:** Change [ ] to 🔄 when you start a task, then to ✅ when complete.

**Example Usage:**
```
Before starting: [ ] 1.1 Remove ControlModeManager Checks
While working:   🔄 1.1 Remove ControlModeManager Checks
After complete:  ✅ 1.1 Remove ControlModeManager Checks
```

---

## MISSING CRITICAL PIECE: ENDPOINT
**Current Problem:** ParkourAgent line 10 requires `target` Transform, but there's no endpoint in your scene.
**Solution:** Create endpoint GameObject in training scene.

---

## PHASE 1: CODE CHANGES (DO THESE FIRST)

**Progress Legend:**
- [ ] = Not started
- 🔄 = Ongoing
- ✅ = Complete

---

### [ ] 1.1 Remove ControlModeManager Checks from ParkourAgent.cs

**File:** `src/Assets/Scripts/ParkourAgent.cs`

**Delete lines 101-109** (entire FixedUpdate ControlMode check):
```csharp
        // Only process movement if in RL Agent mode
        if (ControlModeManager.Instance != null && 
            ControlModeManager.Instance.CurrentMode != ControlModeManager.ControlMode.RLAgent &&
            ControlModeManager.Instance.CurrentMode != ControlModeManager.ControlMode.Heuristic)
        {
            return; // Skip movement if not in RL mode
        }
```

**Delete lines 132-141** (entire OnActionReceived ControlMode check):
```csharp
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
```

**Result:** ParkourAgent now works standalone without ControlModeManager.

---

### [ ] 1.2 Add TrainingArea Reference to ParkourAgent.cs

**File:** `src/Assets/Scripts/ParkourAgent.cs`

**After line 11** (after `private CharacterController controller;`), add:
```csharp
    [SerializeField] private TrainingArea trainingArea; // Auto-assigned by TrainingArea script
```

**Replace lines 58-68** (entire OnEpisodeBegin method) with:
```csharp
    public override void OnEpisodeBegin()
    {
        // Get spawn position from TrainingArea if available, otherwise use stored startPos
        if (trainingArea != null)
        {
            transform.position = trainingArea.GetAgentSpawnPosition();
            target = trainingArea.GetTargetTransform(); // Update target reference
        }
        else
        {
            transform.position = startPos; // Fallback for non-training scenarios
        }
        
        transform.rotation = Quaternion.identity;
        controller.enabled = false; // Prevent physics glitch
        controller.enabled = true;
        
        episodeTimer = 0f;
        lastProgressZ = transform.position.z;
        velocity = Vector3.zero;
    }
```

---

### [ ] 1.3 Create TrainingArea.cs Script

**File:** `src/Assets/Scripts/TrainingArea.cs` (NEW FILE)

```csharp
using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Self-contained training environment for a single agent.
/// Contains platforms, spawn point, and target position.
/// </summary>
public class TrainingArea : MonoBehaviour
{
    [Header("Training Area Configuration")]
    [SerializeField] private Transform agentSpawnPoint;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private ParkourAgent agent;
    
    [Header("Platform Generation")]
    [SerializeField] private bool generatePlatformsOnStart = true;
    [SerializeField] private int platformCount = 8;
    [SerializeField] private float platformSpacing = 15f;
    [SerializeField] private Vector3 platformSize = new Vector3(12f, 0.5f, 6f);
    [SerializeField] private float[] platformHeights; // Fixed heights for deterministic training
    
    private GameObject platformsContainer;
    
    void Start()
    {
        // Validate references
        if (agentSpawnPoint == null)
        {
            Debug.LogError($"TrainingArea '{name}': agentSpawnPoint not assigned!");
        }
        
        if (targetPosition == null)
        {
            Debug.LogError($"TrainingArea '{name}': targetPosition not assigned!");
        }
        
        if (agent == null)
        {
            Debug.LogError($"TrainingArea '{name}': agent not assigned!");
        }
        else
        {
            // Auto-assign this TrainingArea to the agent
            agent.GetType().GetField("trainingArea", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance)?.SetValue(agent, this);
        }
        
        // Generate platforms if enabled
        if (generatePlatformsOnStart)
        {
            GeneratePlatforms();
        }
    }
    
    void GeneratePlatforms()
    {
        // Create container for organization
        platformsContainer = new GameObject("Platforms");
        platformsContainer.transform.SetParent(transform);
        platformsContainer.transform.localPosition = Vector3.zero;
        
        // If no heights array provided, create flat platforms
        if (platformHeights == null || platformHeights.Length == 0)
        {
            platformHeights = new float[platformCount];
            for (int i = 0; i < platformCount; i++)
            {
                platformHeights[i] = 0f; // Flat for initial testing
            }
        }
        
        // Generate platforms at fixed positions
        for (int i = 0; i < platformCount && i < platformHeights.Length; i++)
        {
            Vector3 localPosition = new Vector3(i * platformSpacing, platformHeights[i], 0f);
            CreatePlatform(i, localPosition);
        }
    }
    
    void CreatePlatform(int index, Vector3 localPosition)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = $"Platform_{index}";
        platform.transform.SetParent(platformsContainer.transform);
        platform.transform.localPosition = localPosition;
        platform.transform.localScale = platformSize;
        
        // Ensure collider exists (primitives come with BoxCollider)
        // Add layer or tag if needed for identification
        platform.layer = LayerMask.NameToLayer("Default");
    }
    
    /// <summary>
    /// Called by agent during OnEpisodeBegin to get spawn position.
    /// </summary>
    public Vector3 GetAgentSpawnPosition()
    {
        if (agentSpawnPoint != null)
        {
            return agentSpawnPoint.position;
        }
        
        // Fallback: spawn at training area origin + offset
        return transform.position + new Vector3(0, 2.5f, 0);
    }
    
    /// <summary>
    /// Called by agent to get target transform reference.
    /// </summary>
    public Transform GetTargetTransform()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Optional: Reset any environment state (for advanced training scenarios).
    /// </summary>
    public void ResetArea()
    {
        // Currently no dynamic elements to reset
        // Future: moving platforms, randomized obstacles, etc.
    }
}
```

---

## PHASE 2: UNITY EDITOR SETUP (DO AFTER CODE)

### [ ] 2.1 Create TrainingScene

1. **File → New Scene**
2. **Save as:** `src/Assets/Scenes/TrainingScene.unity`
3. **Delete:** All GameObjects except Main Camera, Directional Light
4. **Main Camera:** Remove CameraFollow script (not needed for training)

---

### [ ] 2.2 Create TrainingArea Prefab

#### Step 1: Create Root GameObject
1. **Hierarchy → Right-click → Create Empty**
2. **Name:** "TrainingArea"
3. **Add Component:** TrainingArea script
4. **Position:** (0, 0, 0)

#### Step 2: Create Child Objects
1. **Right-click TrainingArea → Create Empty** 
   - Name: "AgentSpawnPoint"
   - Position: (0, 2.5, 0) [local]
   
2. **Right-click TrainingArea → Create Empty**
   - Name: "TargetPosition"
   - Position: (105, 0, 0) [local] ← This is your ENDPOINT (after 7 platforms × 15 spacing)
   - **Add Component:** Add a small sphere for visual reference
     - Right-click TargetPosition → 3D Object → Sphere
     - Scale: (2, 2, 2)
     - Material: Make it bright (red/green)

3. **Drag your agent prefab into TrainingArea as child**
   - Name: "Agent"
   - Position: (0, 2.5, 0) [will match spawn point]
   - **Ensure it has:** ParkourAgent + CharacterController
   - **Remove:** PlayerController component (if present)

#### Step 3: Configure TrainingArea Script
In Inspector for TrainingArea GameObject:
- **Agent Spawn Point:** Drag "AgentSpawnPoint" GameObject
- **Target Position:** Drag "TargetPosition" GameObject
- **Agent:** Drag "Agent" GameObject
- **Generate Platforms On Start:** ✓ (checked)
- **Platform Count:** 8
- **Platform Spacing:** 15
- **Platform Heights:** Array size 8, all values = 0 (flat for now)

#### Step 4: Save as Prefab
1. **Drag TrainingArea** from Hierarchy → Assets/Prefabs folder
2. **Name:** TrainingArea.prefab

---

### [ ] 2.3 Place Multiple Training Areas

1. **Open TrainingScene.unity**
2. **Drag TrainingArea prefab** into scene
3. **Position:** (0, 0, 0)
4. **Duplicate:** Ctrl+D (or Cmd+D)
5. **Position second area:** (0, 0, 200) [200 units on Z-axis to avoid overlap]
6. **Repeat:** Create 10-20 instances, spacing them 200 units apart on Z-axis
   - Area 1: (0, 0, 0)
   - Area 2: (0, 0, 200)
   - Area 3: (0, 0, 400)
   - etc.

**Alternatively:** Space them on X-axis:
   - Area 1: (0, 0, 0)
   - Area 2: (200, 0, 0)
   - Area 3: (400, 0, 0)

---

### [ ] 2.4 Configure Build Settings

1. **File → Build Settings**
2. **Add Open Scenes:** Add TrainingScene.unity
3. **Note scene index** (probably index 1 if SampleScene is index 0)

---

### [ ] 2.5 Update parkour_config.yaml

**File:** `src/parkour_config.yaml`

**Verify line 2:** Behavior name must match agent's Behavior Name parameter
```yaml
behaviors:
  ParkourRunner:  # ← This must match ParkourAgent's "Behavior Name" in Inspector
```

**In Unity:** Select Agent GameObject → ParkourAgent component → Behavior Parameters section → Behavior Name = "ParkourRunner"

---

## PHASE 3: VERIFICATION CHECKLIST

### [ ] Pre-Training Checklist:
- [ ] ParkourAgent.cs: ControlModeManager checks removed
- [ ] ParkourAgent.cs: trainingArea field added
- [ ] ParkourAgent.cs: OnEpisodeBegin updated to use TrainingArea
- [ ] TrainingArea.cs: Script created and no errors
- [ ] TrainingScene.unity: Scene created
- [ ] TrainingArea.prefab: Prefab created with all children
- [ ] TrainingArea prefab: AgentSpawnPoint assigned
- [ ] TrainingArea prefab: TargetPosition assigned (ENDPOINT EXISTS)
- [ ] TrainingArea prefab: Agent assigned
- [ ] TrainingScene: 10+ TrainingArea instances placed
- [ ] Agent: Behavior Name = "ParkourRunner"
- [ ] parkour_config.yaml: Behavior name matches
- [ ] Build Settings: TrainingScene added

### [ ] Test Before Training:
1. **Open TrainingScene**
2. **Press Play in Editor**
3. **Check Console:** No errors
4. **Check Scene:** Platforms generated, agents at spawn points, target spheres visible
5. **Check Agent:** Can see 8 platforms ahead, target at end

---

## PHASE 4: RUN TRAINING

### [ ] First Test Run (In Editor, Visual):
```bash
conda activate mlagents
mlagents-learn parkour_config.yaml --run-id=test_v1
```
- When prompted "Press Play in Unity Editor"
- Press Play in Unity
- **Watch:** Do agents move? Do platforms exist? Any errors?
- **Stop after 1-2 minutes** if it looks correct

### [ ] Full Training Run (Headless):
```bash
conda activate mlagents
mlagents-learn parkour_config.yaml --run-id=parkour_v1 --force
```
- **Don't press Play** - runs headless
- **Monitor:** TensorBoard at `http://localhost:6006`
```bash
tensorboard --logdir results/
```

---

## WHAT COULD GO WRONG (SPECIFIC ISSUES)

### Issue 1: "No ParkourRunner found"
**Symptom:** ML-Agents can't find agents
**Fix:** Agent's Behavior Name doesn't match YAML file
**Solution:** Inspector → ParkourAgent → Behavior Parameters → Behavior Name = "ParkourRunner"

### Issue 2: Agents fall through platforms
**Symptom:** Agents immediately fall, episode ends
**Fix:** Platform colliders not set up
**Solution:** Platforms must have BoxCollider (primitives include this)

### Issue 3: Agents don't move
**Symptom:** Agents stand still, no actions
**Fix:** ControlModeManager check still present, or agent disabled
**Solution:** Verify ControlModeManager checks deleted from ParkourAgent.cs

### Issue 4: Target not assigned error
**Symptom:** Console error "Target not assigned"
**Fix:** TrainingArea script didn't auto-assign
**Solution:** Manually assign in TrainingArea inspector, or check reflection code in TrainingArea.Start()

### Issue 5: Agents reach target instantly
**Symptom:** Episodes end in 0.1 seconds with max reward
**Fix:** Target position too close (less than 2 units away)
**Solution:** Move TargetPosition to (105, 0, 0) local position

---

## ENDPOINT SUMMARY (YOUR SPECIFIC QUESTION)

**Where is endpoint now?** DOESN'T EXIST in current setup.

**Where will endpoint be?** 
- TargetPosition GameObject (child of TrainingArea)
- Local position: (105, 0, 0) [after 7 platforms]
- Visual: Bright colored sphere
- Assigned to: ParkourAgent.target field (via TrainingArea script)

**How agent reaches it:**
- Agent spawns at (0, 2.5, 0)
- Platforms go from x=0 to x=105 (8 platforms × 15 spacing)
- Target at x=105
- Agent must run/jump across platforms to reach target
- When distance < 2 units → Episode ends with +10 reward

---

# CRITICAL ISSUES YOU CAUGHT (FIXING NOW)

## ISSUE 1: Target Position Math is WRONG

**Current claim:** Target at (105, 0, 0) local

**Let me verify:**
- 8 platforms total (indices 0-7)
- Spacing = 15 units
- Platform positions: 0, 15, 30, 45, 60, 75, 90, 105
- Platform size = 12 units wide (X dimension)
- Platform extends ±6 units from center

**Platform layout:**
- Platform 0: center at x=0, spans -6 to +6
- Platform 1: center at x=15, spans 9 to 21
- Platform 7: center at x=105, spans 99 to 111

**Agent spawn:** (0, 2.5, 0) - center of Platform 0 ✓

**Target position should be:**
- OPTION A: On last platform (x=105, y=0, z=0) - agent stops on platform 7
- OPTION B: After last platform (x=120, y=0, z=0) - agent must jump to reach beyond platforms

**CORRECT ANSWER: Use (105, 2.5, 0)** - same height as spawn, on platform 7 center.

**Why 2.5 height?** Target at y=0 is below platform surface (platform height varies). Using y=2.5 (spawn height) means target is at agent's height level, regardless of platform height.

**FIXED:** Update instruction in Section 2.2, Step 2.

---

## ISSUE 2: Map Generation is RANDOM (Can't Train on RNG)

**Current problem:**
- `SetupDemo.cs` line 44: `Random.Range(-1.2f, 1.2f)` - RANDOM heights every time
- `LevelGenerator.cs` line 40: `Random.Range(minHeight, maxHeight)` - RANDOM heights
- TrainingArea.cs I wrote also allows variable heights BUT doesn't enforce deterministic

**Why this breaks training:**
- Agent trains on Layout A (easy jumps)
- Next episode: Layout B (impossible jumps)
- Agent can't learn consistent strategy
- Training will fail or be extremely slow

**SOLUTION: Fixed Height Array**

### In TrainingArea.cs - ALREADY DONE (but need to verify)

The code I wrote (section 1.3) has:
```csharp
[SerializeField] private float[] platformHeights; // Fixed heights for deterministic training
```

And in `GeneratePlatforms()`:
```csharp
if (platformHeights == null || platformHeights.Length == 0)
{
    platformHeights = new float[platformCount];
    for (int i = 0; i < platformCount; i++)
    {
        platformHeights[i] = 0f; // Flat for initial testing
    }
}
```

**This is CORRECT but needs proper setup instructions.**

### HOW TO SET UP DETERMINISTIC MAP:

**In Unity Editor, TrainingArea prefab:**

1. **Expand Platform Heights array:**
   - TrainingArea script → Platform Heights → Size: 8

2. **Set fixed heights (OPTION 1: Flat - easiest):**
   - Element 0: 0
   - Element 1: 0
   - Element 2: 0
   - Element 3: 0
   - Element 4: 0
   - Element 5: 0
   - Element 6: 0
   - Element 7: 0
   
   **Result:** All platforms at same height, agent just runs forward.

3. **Set fixed heights (OPTION 2: Progressive difficulty):**
   - Element 0: 0
   - Element 1: 0.5 (small step up)
   - Element 2: 0.8
   - Element 3: 1.2
   - Element 4: 1.5
   - Element 5: 1.2 (down)
   - Element 6: 0.8
   - Element 7: 0.5
   
   **Result:** Agent must learn to jump up and down.

4. **Set fixed heights (OPTION 3: Challenge course):**
   - Element 0: 0
   - Element 1: 1.0 (jump up)
   - Element 2: 1.0 (flat)
   - Element 3: 0.5 (down)
   - Element 4: 0.5 (flat)
   - Element 5: 1.5 (big jump up)
   - Element 6: 1.5 (flat)
   - Element 7: 0.0 (jump down to finish)
   
   **Result:** Varied challenge, tests multiple skills.

**RECOMMENDATION: Start with OPTION 1 (flat) to verify training works, then increase difficulty.**

### What About SetupDemo and LevelGenerator?

**For TrainingScene:** DON'T USE SetupDemo or LevelGenerator at all.
- SetupDemo = random heights, 36 platforms, designed for player mode
- LevelGenerator = random heights, configurable but still random
- TrainingArea = fixed heights, deterministic, self-contained

**For SampleScene (player mode):** Keep SetupDemo/LevelGenerator as-is. Random is fine for human play.

**UPDATED INSTRUCTIONS:** Section 2.1 already says "Delete all GameObjects except Camera and Light" - this means SetupDemo won't run. ✓

---

## ISSUE 3: Fall Logic is BROKEN (Infinite Fall)

**Current situation:**

Looking at `ParkourAgent.cs` lines 182-186:
```csharp
if (transform.position.y < config.fallThreshold || episodeTimer > config.episodeTimeout)
{
    AddReward(config.fallPenalty);
    EndEpisode();
}
```

This SHOULD work:
- Agent falls below y=-5
- `EndEpisode()` called
- ML-Agents automatically calls `OnEpisodeBegin()`
- Position resets

**But you say it doesn't work. Why?**

**PROBLEM 1: ControlModeManager blocks OnActionReceived**

Lines 132-141 (which I said to delete):
```csharp
if (ControlModeManager.Instance != null)
{
    var mode = ControlModeManager.Instance.CurrentMode;
    if (mode != ControlModeManager.ControlMode.RLAgent && 
        mode != ControlModeManager.ControlMode.Heuristic)
    {
        return; // ← EXITS EARLY, NEVER CHECKS FALL
    }
}
```

If mode is NOT set to RLAgent, the fall check never executes. Agent falls forever.

**After deleting those lines (section 1.1), this is fixed.**

**PROBLEM 2: GameManager interferes**

`GameManager.Update()` lines 73-81:
```csharp
PlayerController player = FindObjectOfType<PlayerController>();
if (player != null)
{
    if (player.transform.position.y < config.playerResetThreshold)
    {
        ResetPlayer(player);
    }
}
```

This only checks for `PlayerController`, not `ParkourAgent`. So in agent mode, no reset happens from GameManager.

**This is fine** because agent has its own reset logic in `OnActionReceived`. BUT if ControlModeManager blocks `OnActionReceived`, fall detection never happens.

**PROBLEM 3: OnEpisodeBegin might not reset properly**

Looking at `OnEpisodeBegin()` lines 58-68:
```csharp
transform.position = startPos; // ← What if startPos is wrong?
controller.enabled = false;
controller.enabled = true;
velocity = Vector3.zero;
```

If `startPos` was set during `Initialize()` (line 45), it stores initial position. But what if agent was placed at wrong position in scene?

**After my changes:** OnEpisodeBegin uses `trainingArea.GetAgentSpawnPosition()`, which gets position from AgentSpawnPoint GameObject. This is deterministic and correct.

---

### VERIFICATION: Fall Reset Logic

**Add explicit test to TrainingArea.cs:**

In TrainingArea.cs, after line 48 (in Start method), add fallback reset:

```csharp
void Start()
{
    // ... existing validation code ...
    
    if (agent == null)
    {
        Debug.LogError($"TrainingArea '{name}': agent not assigned!");
    }
    else
    {
        // Auto-assign this TrainingArea to the agent
        agent.GetType().GetField("trainingArea", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance)?.SetValue(agent, this);
        
        // ADD THIS: Verify agent has fall detection
        StartCoroutine(MonitorAgentFalls());
    }
    
    // ... rest of Start ...
}

// ADD THIS METHOD:
private System.Collections.IEnumerator MonitorAgentFalls()
{
    while (true)
    {
        yield return new WaitForSeconds(1f);
        
        if (agent != null && agent.transform.position.y < -10f)
        {
            Debug.LogWarning($"TrainingArea '{name}': Agent fell below y=-10! Episode should have ended. Check ParkourAgent.OnActionReceived()");
        }
    }
}
```

**NO WAIT - This is hacky monitoring, not a fix.**

**ACTUAL FIX:**

The problem is that ML-Agents' `OnActionReceived` is only called during training when the agent receives actions. If:
- Training isn't running
- Or agent isn't properly set up
- Or behavior name doesn't match

Then `OnActionReceived` never fires, fall detection doesn't happen.

**PROPER SOLUTION: Add emergency reset in FixedUpdate**

### Update ParkourAgent.cs FixedUpdate

**Replace the FixedUpdate method (lines 101-128) entirely:**

```csharp
void FixedUpdate()
{
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
    
    // EMERGENCY FALL RESET (backup if OnActionReceived isn't being called)
    // This should NEVER trigger during proper training, but prevents infinite fall during testing
    CharacterConfig configCheck = CharacterConfigManager.Config;
    if (transform.position.y < configCheck.fallThreshold - 5f) // -10 if fallThreshold is -5
    {
        Debug.LogWarning($"ParkourAgent: Emergency fall reset at y={transform.position.y}. OnActionReceived may not be firing!");
        EndEpisode(); // Force episode end
    }
}
```

**Why this works:**
- FixedUpdate always runs (not blocked by mode checks)
- Normal fall detection at y < -5 happens in OnActionReceived during training
- Emergency reset at y < -10 catches infinite falls during testing/debugging
- The warning tells you if something is misconfigured

---

## UPDATED IMPLEMENTATION STEPS

### [ ] Section 1.1: KEEP AS-IS (delete ControlModeManager checks)

### [ ] Section 1.2: ADD TARGET HEIGHT FIX

**After line 11** in ParkourAgent.cs, add:
```csharp
    [SerializeField] private TrainingArea trainingArea; // Auto-assigned by TrainingArea script
```

**Replace OnEpisodeBegin (lines 58-68):**
```csharp
    public override void OnEpisodeBegin()
    {
        // Get spawn position from TrainingArea if available, otherwise use stored startPos
        if (trainingArea != null)
        {
            transform.position = trainingArea.GetAgentSpawnPosition();
            target = trainingArea.GetTargetTransform(); // Update target reference
        }
        else
        {
            transform.position = startPos; // Fallback for non-training scenarios
        }
        
        transform.rotation = Quaternion.identity;
        controller.enabled = false; // Prevent physics glitch
        controller.enabled = true;
        
        episodeTimer = 0f;
        lastProgressZ = transform.position.z;
        velocity = Vector3.zero;
    }
```

### [ ] Section 1.1 ADDITION: Fix FixedUpdate Fall Logic

**REPLACE entire FixedUpdate method (lines 101-128):**

```csharp
    void FixedUpdate()
    {
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
        
        // Apply vertical movement (gravity/jumping)
        if (controller != null)
        {
            controller.Move(velocity * Time.fixedDeltaTime);
        }
        
        // Emergency fall reset (backup if OnActionReceived not called)
        CharacterConfig configCheck = CharacterConfigManager.Config;
        if (transform.position.y < configCheck.fallThreshold - 5f)
        {
            Debug.LogWarning($"ParkourAgent '{name}': Emergency fall reset at y={transform.position.y}. Check training setup!");
            EndEpisode();
        }
    }
```

### [ ] Section 2.2 FIX: Target Position Height

**Step 2, update TargetPosition:**
   - Name: "TargetPosition"
   - Position: **(105, 2.5, 0) [local]** ← CHANGED from (105, 0, 0)
   - **Why:** Same height as spawn point (2.5), on center of platform 7

**Step 3, add Platform Heights configuration:**

After "Configure TrainingArea Script" section, add:

**Platform Heights Setup (Deterministic Map):**

Choose one option:

**OPTION 1 - FLAT (Start here):**
- Platform Heights → Size: 8
- Set all elements to: **0**

**OPTION 2 - PROGRESSIVE (After flat works):**
- Platform Heights → Size: 8
- Values: 0, 0.5, 0.8, 1.2, 1.5, 1.2, 0.8, 0.5

**OPTION 3 - CHALLENGE (Advanced):**
- Platform Heights → Size: 8
- Values: 0, 1.0, 1.0, 0.5, 0.5, 1.5, 1.5, 0.0

**Start with FLAT to verify everything works.**

---

## 🚀 PRE-FLIGHT CHECKLIST (UPDATED)

**Use these status indicators as you work:**
- [ ] = Not started
- 🔄 = Currently working on this
- ✅ = Verified complete

### Before pressing Play:

**Math verification:**
- [ ] Agent spawn: (0, 2.5, 0)
- [ ] Target position: (105, 2.5, 0) 
- [ ] Platform 0 center: (0, 0, 0) [+ agent at y=2.5 is above it]
- [ ] Platform 7 center: (105, 0, 0) [+ target at y=2.5 is above it]
- [ ] Distance to travel: 105 units across 8 platforms

**Map determinism:**
- [ ] Platform Heights array: Size = 8, all values set (not empty)
- [ ] Generate Platforms On Start: Checked
- [ ] SetupDemo script: NOT in TrainingScene
- [ ] LevelGenerator: NOT in TrainingScene

**Fall reset:**
- [ ] ControlModeManager checks deleted from ParkourAgent
- [ ] Emergency fall reset added to FixedUpdate
- [ ] OnEpisodeBegin uses trainingArea.GetAgentSpawnPosition()

**Visual check in Scene view:**
- [ ] Platforms visible in straight line along X-axis
- [ ] Target sphere visible at end (x=105)
- [ ] Agent positioned on first platform
- [ ] Multiple TrainingAreas spaced 200 units apart

### Test in Editor:

1. Open TrainingScene
2. Press Play
3. **Watch for 10 seconds:**
   - [ ] Platforms generated
   - [ ] Agents on platforms (not falling through)
   - [ ] No infinite fall (check Console for emergency reset warnings)
4. **Select one agent in Hierarchy**
5. **Inspector → ParkourAgent → Show debug info**
   - [ ] Target assigned (not null)
   - [ ] Controller assigned
   - [ ] Training Area assigned
6. **Stop Play**

If any checkbox fails, DO NOT proceed to training. Fix it first.

---

END OF UPDATED PLAN