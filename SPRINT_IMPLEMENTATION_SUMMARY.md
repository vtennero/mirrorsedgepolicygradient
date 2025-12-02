# Sprint Action Implementation Summary

## Overview
Add sprint action (action 3) with stamina system. Sprint moves faster than jog but consumes stamina. Jumping also consumes stamina. Stamina regenerates when not sprinting and not jumping.

**Note:** Action 2 (jog) = existing "forward" action. No changes to jog behavior.

## ⚠️ BACKUP INCOMPATIBLE FILES FIRST

**CRITICAL:** This change breaks all existing trained models. Backup incompatible files before implementation.

**Files to backup (to `backup_before_adding_sprinting/` folder):**
1. `src/Assets/Prefabs/TrainingArea.prefab` - Contains BehaviorParameters with action space 3
2. `src/Assets/Scripts/ParkourAgent.cs` - Action handling logic
3. `src/Assets/Scripts/CharacterConfig.cs` - Will add stamina parameters
4. `src/Assets/Scripts/AgentAnimationSync.cs` - Animation logic
5. `MDP.md` - Will update with new action/observation space

**Backup command:**
```bash
mkdir -p backup_before_adding_sprinting
cp src/Assets/Prefabs/TrainingArea.prefab backup_before_adding_sprinting/
cp src/Assets/Scripts/ParkourAgent.cs backup_before_adding_sprinting/
cp src/Assets/Scripts/CharacterConfig.cs backup_before_adding_sprinting/
cp src/Assets/Scripts/AgentAnimationSync.cs backup_before_adding_sprinting/
cp MDP.md backup_before_adding_sprinting/
```

## Implementation Plan

### 1. Action Space Changes
**Current:** 3 discrete actions (0=idle, 1=jump, 2=jog forward)
**New:** 4 discrete actions
- `0`: Idle/nothing
- `1`: Jump (only if grounded + stamina >= 5, blocked if stamina = 0)
- `2`: Jog forward (normal speed, existing action - unchanged)
- `3`: Sprint forward (faster speed, consumes stamina, blocked if stamina = 0 → falls back to jog)

**Files to modify:**
- `ParkourAgent.cs`: `OnActionReceived()`, `FixedUpdate()`, action tracking
- `TrainingArea.prefab`: BehaviorParameters `BranchSizes` (3 → 4)
- `parkour_config.yaml`: No changes needed (action space auto-detected from prefab)

### 2. Stamina System

**Stamina Parameters (PRECISE VALUES):**
- `maxStamina` = 100.0
- `staminaConsumptionRate` = 33.33 per second (100 / 3 seconds to deplete full bar)
- `jumpStaminaCost` = 5.0 per jump
- `staminaRegenRate` = 20.0 per second (slower than consumption, configurable in CharacterConfig)

**Stamina Variables:**
- `currentStamina` (float): Current stamina value (0.0 to maxStamina)

**Stamina Logic (in FixedUpdate - physics step):**
- **Sprinting (action 3):** 
  - If `currentStamina > 0`: Consume `staminaConsumptionRate * Time.fixedDeltaTime` per physics step
  - If `currentStamina <= 0`: Action 3 is blocked → automatically becomes action 2 (jog)
- **Jumping (action 1):**
  - If `currentStamina < jumpStaminaCost` (5.0): Jump is **BLOCKED** (cannot jump)
  - If `currentStamina >= jumpStaminaCost`: Consume `jumpStaminaCost` when jump is triggered
- **Regeneration:** When NOT sprinting AND NOT jumping, regenerate `staminaRegenRate * Time.fixedDeltaTime` per physics step
- **Clamp:** `currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina)`
- **Blocking behavior:** Agent decides to use stamina. If no stamina, agent cannot make that decision and must jog or wait.

**Files to modify:**
- `CharacterConfig.cs`: Add stamina parameters
- `ParkourAgent.cs`: Add stamina tracking, consumption, regeneration logic
- `ParkourAgent.cs`: `OnEpisodeBegin()` - reset stamina to max

### 3. Movement Speed Changes

**Current:**
- Action 2 uses `moveSpeed` (6f from config)

**New:**
- Action 2 (jog): Uses `moveSpeed` (6f)
- Action 3 (sprint): Uses `sprintSpeed` (12f from config, already exists)

**Files to modify:**
- `ParkourAgent.cs`: `FixedUpdate()` - check action 3, use `sprintSpeed` if stamina > 0, otherwise fall back to jog (action 2)
- `CharacterConfig.cs`: `sprintSpeed` already exists (12f), no changes needed

### 4. State Space (Observations) Changes

**Current:** 13 floats
**New:** 14 floats (add stamina observation)

**New observation:**
- `stamina` (1 float): Normalized stamina `currentStamina / maxStamina` (0.0 to 1.0)

**Files to modify:**
- `ParkourAgent.cs`: `CollectObservations()` - add stamina observation
- `TrainingArea.prefab`: BehaviorParameters `VectorObservationSize` (13 → 14)

### 5. Animation Changes

**Current behavior:**
- `AgentAnimationSync.cs` line 75-78: Action 2 sets both `IsJogging=true` and `IsSprinting=true`

**New behavior:**
- Action 2 (jog): `IsJogging=true`, `IsSprinting=false`
- Action 3 (sprint): `IsJogging=false`, `IsSprinting=true` (only if stamina > 0)
- Action 3 with no stamina: Fall back to jog animation (instant switch, no transition)
- In air when stamina depletes: Nothing special, go back to jog animation when grounded

**Files to modify:**
- `AgentAnimationSync.cs`: Update animation logic to distinguish jog vs sprint
- `ParkourAgent.cs`: Expose stamina state or sprint state to `AgentAnimationSync`

### 6. Reward Considerations

**Current rewards unchanged:**
- Progress reward: Still based on distance moved (sprint = faster progress = more reward per time)
- Time penalty: Still applies
- Target reach: Still applies
- Fall penalty: Still applies

**Potential additions (not required initially):**
- Stamina efficiency reward? (reward for using stamina wisely)
- Sprint penalty? (penalty for sprinting when unnecessary)

**Files to modify:**
- `ParkourAgent.cs`: `OnActionReceived()` - potentially add stamina-based rewards (optional)

### 7. Episode Reset

**Stamina reset:**
- `OnEpisodeBegin()`: Set `currentStamina = maxStamina`

**Files to modify:**
- `ParkourAgent.cs`: `OnEpisodeBegin()` - reset stamina

### 8. Action Tracking & Logging

**Current metrics:**
- `jumpCount`, `forwardActionCount`, `idleActionCount`

**New metrics:**
- `sprintActionCount`
- Update `LogEpisodeStats()` to include sprint count/percentage

**Files to modify:**
- `ParkourAgent.cs`: Add `sprintActionCount` tracking
- `ParkourAgent.cs`: `LogEpisodeStats()` - add sprint metrics

---

## Blind Spots & Risks

### 🔴 CRITICAL: Breaking Existing Models

**Risk:** All existing trained models (test_v6, test_v13, etc.) will be **completely incompatible** because:
1. Action space changed from 3 → 4 actions
2. Observation space changed from 13 → 14 floats
3. Neural network input/output dimensions don't match

**Impact:** Must retrain from scratch. Cannot use existing checkpoints.

**Mitigation:**
- Document this as a breaking change
- Consider versioning (v0.1 → v0.2)
- Keep old model files for reference

### ✅ RESOLVED: Stamina Parameter Values

**Values (PRECISE):**
1. `maxStamina` = 100.0
2. `staminaConsumptionRate` = 33.33 per second (100 / 3 seconds to deplete full bar)
3. `jumpStaminaCost` = 5.0 per jump
4. `staminaRegenRate` = 20.0 per second (slower than consumption, configurable in CharacterConfig)
5. Stamina regenerates when NOT sprinting AND NOT jumping (works in air and on ground)

### ✅ RESOLVED: Sprint Behavior When Out of Stamina

**Behavior:**
1. When action 3 is selected but stamina = 0: Automatically fall back to jog (action 2)
2. No penalty for trying to sprint with no stamina - it's simply impossible (blocked)
3. Agent cannot make decision to sprint if no stamina - must jog or wait

### ✅ RESOLVED: Jump Behavior with Stamina

**Behavior:**
1. Agent **CANNOT** jump when stamina = 0 - jump is **BLOCKED**
2. Jump requires `stamina >= jumpStaminaCost` (5.0)
3. If stamina < 5.0, jump action is blocked (agent cannot make decision to jump)
4. No penalty for attempting jump with no stamina - it's simply impossible (blocked)

### ✅ RESOLVED: Animation Transition Timing

**Behavior:**
1. Animation switches to jog **immediately** when stamina hits 0 (instant switch, no transition)
2. If agent is in air when stamina depletes: Nothing special, go back to jog animation when grounded

### 🟡 Risk: AgentAnimationSync Access to Stamina

**Current:** `AgentAnimationSync` reads `agent.CurrentAction` but doesn't know about stamina.

**Problem:** If action 3 is selected but stamina = 0, animation should show jog, not sprint.

**Solutions:**
1. Expose `IsSprinting` property from `ParkourAgent` (considers stamina)
2. Expose `currentStamina` property from `ParkourAgent`
3. Have `ParkourAgent` override action 3 → 2 when stamina depleted (before animation sync reads it)

**Recommendation:** Option 3 (override in `ParkourAgent`) - cleaner separation of concerns.

### ✅ RESOLVED: FixedUpdate vs Update Timing

**Implementation:**
1. Stamina consumption happens in `FixedUpdate()` (physics step), NOT in `OnActionReceived()`
2. Agent decides to use stamina in `OnActionReceived()`, but actual consumption happens in `FixedUpdate()` based on current action state
3. If action 3 is selected but stamina depletes mid-step, action 3 becomes action 2 (jog) in next physics step
4. **Not ambiguous:** Agent can decide to use stamina. If no stamina, agent cannot make that decision and has to jog or wait.

### 🟡 Risk: Heuristic Function

**Current:** `Heuristic()` only handles actions 0, 1, 2.

**Problem:** Need to add action 3 (sprint) to heuristic for manual testing.

**Files to modify:**
- `ParkourAgent.cs`: `Heuristic()` - add sprint key binding (e.g., LeftShift+W)

### 🟡 Risk: Progress Reward Scaling

**Current:** Progress reward = `progressDelta * 0.1`

**Problem:** Sprinting moves faster, so progress reward per step is higher. This may create unintended incentive to always sprint.

**Questions:**
1. Should progress reward be normalized by speed? (reward per unit distance, not per step)
2. Should there be a stamina efficiency reward? (reward for reaching target with stamina remaining)
3. Should sprinting have a small penalty to encourage strategic use?

**Recommendation:** Keep current reward structure initially. Monitor training. Sprint's speed advantage is the reward - if agent learns to sprint unnecessarily, add penalty later.

### 🟡 Risk: Training Config Compatibility

**Current:** `parkour_config.yaml` doesn't specify action space (auto-detected from prefab).

**Problem:** If prefab is updated but old config is used, mismatch may occur.

**Mitigation:** Config should auto-detect, but verify after prefab changes.

### 🟡 Missing: Stamina Visualization

**Questions:**
1. Should stamina bar be visible during training? (probably not needed)
2. Should stamina bar be visible during inference/demo? (probably yes, for debugging)
3. Where should stamina bar be displayed? (UI overlay, world space above agent?)

**Recommendation:** Add optional stamina bar UI component, disabled by default during training, enabled during demo mode.

### 🟡 Risk: Episode Timeout with Stamina

**Current:** Episode timeout = 90s

**Problem:** If agent learns to sprint constantly and runs out of stamina early, it may be forced to jog slowly for rest of episode, making timeout more likely.

**Questions:**
1. Should episode timeout be adjusted?
2. Should stamina regeneration be faster to prevent this?

**Recommendation:** Monitor training. If timeout becomes common issue, increase regen rate or timeout.

---

## Implementation Order

0. **⚠️ BACKUP FIRST:** Run `backup_before_sprint.sh` (Linux/Mac) or `backup_before_sprint.bat` (Windows)
1. **Add stamina parameters to `CharacterConfig.cs`**
2. **Add stamina tracking to `ParkourAgent.cs`** (variables, reset in `OnEpisodeBegin()`)
3. **Update action space** (3 → 4) in `ParkourAgent.cs` and prefab
4. **Add stamina observation** to `CollectObservations()`
5. **Implement stamina consumption/regeneration** in `FixedUpdate()` (physics step)
6. **Update movement logic** (action 2 = jog unchanged, action 3 = sprint with blocking)
7. **Update animation sync** (`AgentAnimationSync.cs`)
8. **Update action tracking** (sprint count, logging)
9. **Update heuristic** (add sprint key: LeftShift+W)
10. **Test with manual control** (heuristic)
11. **Retrain from scratch** (existing models incompatible)

---

## Testing Checklist

- [ ] Manual control: All 4 actions work (idle, jump, jog, sprint)
- [ ] Stamina depletes when sprinting (33.33 per second, full bar in 3 seconds)
- [ ] Stamina depletes when jumping (5.0 per jump)
- [ ] Stamina regenerates when not sprinting/jumping (20.0 per second)
- [ ] Sprint blocked/falls back to jog when stamina = 0 (instant, no penalty)
- [ ] Jump blocked when stamina < 5.0 (cannot jump with insufficient stamina)
- [ ] Animation shows jog for action 2, sprint for action 3
- [ ] Animation switches to jog instantly when stamina depletes during sprint (no transition)
- [ ] In air stamina depletion: nothing special, goes to jog when grounded
- [ ] Observation space = 14 floats (verify in BehaviorParameters - was 13)
- [ ] Action space = 4 actions (verify in BehaviorParameters - was 3)
- [ ] Episode reset sets stamina to max (100.0)
- [ ] Training starts successfully (no dimension mismatches)
- [ ] Agent can learn to use sprint strategically

