# Gameplay Change Log

> **Note:** Current year is 2025. Historical entries below are from 2024.

> **AI Instructions:** This log tracks all changes to agent behavior, including actions, rewards, observations, and environment parameters. Each entry should include: date, change type (ACTION/REWARD/OBSERVATION/ENVIRONMENT), description, before/after values, and rationale. Use chronological order with most recent at top.

---

## Change Log

### 2025-12-06: Adjusted Platform Gaps and Vertical Variation for Difficulty
**Type:** ENVIRONMENT  
**Status:** ✅ Implemented

**Change:**
- Increased platform gap range: 0.5-2 units → 2-4 units (edge-to-edge)
- Increased vertical height variation: -0.3 to +0.5 units → -0.5 to +1.0 units
- Fixed prefab serialization issue (prefab had old values overriding code)

**Before:**
- Gap range: 0.5-2 units (too easy, agent hitting 18+ reward)
- Height variation: -0.3 to +0.5 units (minimal vertical challenge)

**After:**
- Gap range: 2-4 units (moderate difficulty)
- Height variation: -0.5 to +1.0 units (more vertical jumps)

**Files Modified:**
- `TrainingArea.prefab`: Updated `gapRange` from `{x: 0.5, y: 2}` to `{x: 2, y: 4}`
- `TrainingArea.prefab`: Updated `heightChangeRange` from `{x: -0.3, y: 0.5}` to `{x: -0.5, y: 1.0}`
- `TrainingArea.cs`: Updated code defaults to match

**Rationale:** After fixing gap calculation issues, game was too easy (18+ reward). Increased gaps and vertical variation to provide moderate challenge while remaining feasible (agent can jump ~5 units horizontally, ~1.6 units vertically).

**Note:** Critical lesson learned - Unity prefab serialized values override code defaults. Always update both prefab AND code when changing `[SerializeField]` fields.

---

### 2025-12-06: Fixed Platform Gap Calculation and Prefab Serialization
**Type:** ENVIRONMENT (Bug Fix)  
**Status:** ✅ Fixed

**Change:**
- Fixed gap calculation logic (edge-to-edge tracking)
- Reduced gap range initially: 2-8 units → 0.5-2 units (to ensure feasibility)
- Fixed Unity prefab serialization issue (prefab had old values [2, 8] overriding code)

**Before:**
- Gap range in code: 0.5-2 units
- Gap range in prefab: 2-8 units (OVERRIDING code!)
- Gaps were 4-8 units in practice, making game infeasible

**After:**
- Gap range: 0.5-2 units (both prefab and code)
- Proper edge-to-edge gap calculation
- All gaps verified to be within range

**Files Modified:**
- `TrainingArea.cs`: Rewrote gap calculation logic using first principles (track edges, calculate gaps)
- `TrainingArea.prefab`: Fixed serialized `gapRange` from `{x: 2, y: 8}` to `{x: 0.5, y: 2}`
- `TrainingArea.prefab`: Fixed serialized `heightChangeRange` from `{x: -1.5, y: 2.5}` to `{x: -0.3, y: 0.5}`

**Rationale:** After making platforms 3x longer, gaps became too large and game was infeasible. Root cause was Unity prefab serialization - prefab values override code defaults. Fixed by updating prefab file directly.

**Lesson Learned:** When using `[SerializeField]`, always update BOTH the prefab file AND code defaults. Prefab serialized values take precedence at runtime.

---

### 2025-12-06: Increased Platform Length (Most Platforms 3x Longer)
**Type:** ENVIRONMENT  
**Status:** ✅ Implemented

**Change:**
- Most platforms (80% chance) are now 3x longer than base size
- Base platform width: 20-28 units (randomized)
- Long platforms: 60-84 units (3x base, 80% of platforms)
- Short platforms: 20-28 units (20% of platforms, for variety)
- Gaps between platforms: 2-4 units (edge-to-edge, adjusted for difficulty)
- Target position automatically adjusts further due to longer platforms

**Before:**
- All platforms: 20-28 units width (randomized)
- Average total course length: ~(24 * 20) + (gaps) ≈ 480-560 units

**After:**
- 80% platforms: 60-84 units width (3x longer)
- 20% platforms: 20-28 units width (original size)
- Average total course length: ~(72 * 0.8 * 20) + (24 * 0.2 * 20) + (gaps) ≈ 1248-1344 units
- Target position moves further automatically (uses `lastPlatformEndX + targetOffset`)

**Files Modified:**
- `TrainingArea.cs`: Modified platform generation logic to apply 3x multiplier with 80% probability

**Rationale:** Provide more space for future slide action implementation and enable longer sprinting segments. Longer platforms give agent more room to build speed and prepare for jumps.

**Note:** Target position calculation already handles variable platform sizes correctly via `lastPlatformEndX` tracking.

---

### 2025-12-06: Adjusted Jump Physics for More Horizontal Movement
**Type:** ACTION (Physics)  
**Status:** ✅ Implemented

**Change:**
- Reduced vertical jump force: 12f → 8f (33% reduction)
- Increased horizontal jump boost: 6f → 10f (67% increase)
- Jump forward boost now properly applied in FixedUpdate when jumping
- Max jump height reduced: ~3.6 units → ~1.6 units
- Jumps now travel further horizontally than vertically (more human-like)

**Before:**
- `jumpForce`: 12f (vertical)
- `jumpForwardBoost`: 6f (horizontal, but not implemented)
- Jump trajectory: More vertical, less horizontal

**After:**
- `jumpForce`: 8f (vertical, reduced)
- `jumpForwardBoost`: 10f (horizontal, increased and now implemented)
- Jump trajectory: More horizontal, less vertical (human-like)

**Files Modified:**
- `CharacterConfig.cs`: Updated `jumpForce` (12f → 8f) and `jumpForwardBoost` (6f → 10f)
- `ParkourAgent.cs`: Implemented `jumpForwardBoost` application in `FixedUpdate()` via `justJumped` flag

**Rationale:** Make jumps feel more natural and human-like. Real parkour jumps prioritize horizontal distance over vertical height. This also better matches the longer platform layout.

**Impact:** Jumps will travel further horizontally, making gap traversal more efficient. Lower vertical component reduces risk of overshooting platforms.

---

### 2024-11-29: Added Sprint Action (Action 3)
**Type:** ACTION  
**Status:** ✅ Implemented

**Change:**
- Added action 3: Sprint forward (faster speed, consumes stamina)
- Action space expanded from 3 → 4 discrete actions
- Sprint speed: 12f (2x jog speed of 6f)
- Sprint consumes stamina at 33.33/second
- Sprint blocked if stamina <= 0 (falls back to jog)

**Before:**
- Actions: 0=idle, 1=jump, 2=jog forward

**After:**
- Actions: 0=idle, 1=jump, 2=jog forward, 3=sprint forward

**Files Modified:**
- `ParkourAgent.cs`: Added sprint handling in `OnActionReceived()` and `FixedUpdate()`
- `CharacterConfig.cs`: Added `sprintSpeed`, `sprintAccelTime`, stamina system parameters
- `TrainingArea.prefab`: BehaviorParameters `BranchSizes` updated (3 → 4)

**Rationale:** Enable faster movement for better performance, with stamina system to prevent constant sprinting.

**Breaking Change:** ⚠️ All existing trained models incompatible (action space changed)

---

### 2024-11-29: Added Stamina System
**Type:** OBSERVATION + ACTION CONSTRAINT  
**Status:** ✅ Implemented

**Change:**
- Added stamina observation (1 float): normalized stamina `currentStamina / maxStamina`
- Observation space expanded from 13 → 14 floats
- Jump requires stamina >= 5.0 (blocked if insufficient)
- Sprint requires stamina > 0 (falls back to jog if depleted)
- Stamina regenerates at 20/second when not sprinting/jumping
- Stamina depletes at 33.33/second when sprinting

**Before:**
- Observations: 13 floats (no stamina)
- Jump: Always allowed if grounded
- Sprint: N/A (didn't exist)

**After:**
- Observations: 14 floats (includes stamina)
- Jump: Requires stamina >= 5.0
- Sprint: Requires stamina > 0

**Files Modified:**
- `ParkourAgent.cs`: Added stamina tracking, consumption, regeneration
- `CharacterConfig.cs`: Added stamina parameters (`maxStamina`, `staminaConsumptionRate`, `jumpStaminaCost`, `staminaRegenRate`)

**Rationale:** Balance sprint/jump usage, prevent constant sprinting, add resource management.

---

### 2024-11-28: Platform Size Doubled
**Type:** ENVIRONMENT  
**Status:** ✅ Implemented

**Change:**
- Platform length (X-axis) doubled: 12f → 24f
- Platform width range doubled: 10-14 → 20-28 units
- Platform height unchanged: 0.5f

**Before:**
- `platformSize`: `Vector3(12f, 0.5f, 6f)`
- `platformWidthRange`: `Vector2(10f, 14f)`

**After:**
- `platformSize`: `Vector3(24f, 0.5f, 6f)`
- `platformWidthRange`: `Vector2(20f, 28f)`

**Files Modified:**
- `TrainingArea.cs`: Line 19, 28

**Rationale:** Larger platforms provide more landing area, reduce difficulty, improve training stability.

---

### 2024-11-28: Added Platform Height Randomization
**Type:** ENVIRONMENT  
**Status:** ✅ Implemented

**Change:**
- Added vertical height variation between platforms
- Height change range: -1.5 to +2.5 units per platform
- Absolute height bounds: -0.5 to 5.0 units
- Jump feasibility validation added (editor-only warnings)

**Before:**
- All platforms at same height (flat)

**After:**
- Platforms can vary in height with safe randomization
- Height changes respect agent jump capabilities (~6.4 units max height)

**Files Modified:**
- `TrainingArea.cs`: Added `randomizeHeights`, `heightChangeRange`, `absoluteHeightRange`, `IsJumpFeasible()`

**Rationale:** Increase environment diversity, improve generalization, more realistic parkour scenarios.

---

### 2024-11-28: Added Platform Detection Raycasts
**Type:** OBSERVATION  
**Status:** ✅ Implemented

**Change:**
- Added 5 downward raycasts at forward distances: 2m, 4m, 6m, 8m, 10m
- Observation space expanded from 8 → 13 floats
- Raycasts normalized by max distance (10f)

**Before:**
- Observations: 8 floats
- No platform geometry perception

**After:**
- Observations: 13 floats (includes 5 platform raycasts)
- Agent can detect gaps and platform edges ahead

**Files Modified:**
- `ParkourAgent.cs`: Added `CollectPlatformObservations()` method

**Rationale:** Required for randomized platforms - agent needs to perceive geometry to navigate variable gaps/widths. Without this, test_v9 showed 60% performance drop.

---

### 2024-11-27: Fixed Progress Reward Axis
**Type:** REWARD  
**Status:** ✅ Fixed

**Change:**
- Progress reward now tracks X-axis (correct axis for platform layout)
- Previously tracked Z-axis (wrong axis)

**Before:**
- Progress reward: `progressDelta * 0.1` on Z-axis ❌

**After:**
- Progress reward: `progressDelta * 0.1` on X-axis ✅

**Files Modified:**
- `ParkourAgent.cs`: Fixed axis tracking in `OnActionReceived()`

**Rationale:** Platforms are arranged along X-axis, not Z-axis. Wrong axis caused agent to learn "do nothing" strategy.

**Impact:** test_v4 → test_v5: Performance improved from -2.497 → +5.976 reward.

---

### 2024-11-27: Added Grounded Reward
**Type:** REWARD  
**Status:** ✅ Implemented

**Change:**
- Added small reward per step when agent is grounded: +0.001

**Before:**
- No reward for staying on platforms

**After:**
- Grounded reward: +0.001 per step when `controller.isGrounded`

**Files Modified:**
- `ParkourAgent.cs`: Added grounded check in reward calculation

**Rationale:** Encourage agent to stay on platforms, reduce falling, improve stability.

**Impact:** test_v5 → test_v6: Performance improved from +5.976 → +8.478 reward (+42%).

---

## Current State Summary

### Actions (4 total)
- **0:** Idle/nothing
- **1:** Jump (requires grounded + stamina >= 5.0)
- **2:** Jog forward (6f speed)
- **3:** Sprint forward (12f speed, consumes stamina, requires stamina > 0)

### Observations (14 total)
- Target relative position (3 floats)
- Velocity (3 floats)
- Grounded state (1 float)
- Platform detection raycasts (5 floats: distances at 2m, 4m, 6m, 8m, 10m ahead)
- Forward obstacle raycast (1 float)
- Stamina normalized (1 float)

### Rewards
- **Progress:** `+0.1` per unit forward (X-axis)
- **Grounded:** `+0.001` per step (when on ground)
- **Time penalty:** `-0.001` per step
- **Target reach:** `+10.0` (if distance < 2.0 units)
- **Fall/timeout:** `-1.0` (if y < -5.0 or timeout > 90s)

### Environment
- **Platform count:** 20
- **Platform size:** 24f × 0.5f × 6f (base length × height × depth)
- **Platform width range:** 
  - 80% of platforms: 60-84 units (3x longer, randomized)
  - 20% of platforms: 20-28 units (original size, randomized)
- **Platform gap range:** 2-4 units (randomized, edge-to-edge)
- **Platform height randomization:** Enabled (-0.5 to +1.0 units change, bounds: -0.5 to 5.0)
- **Target offset:** 5 units beyond last platform
- **Average course length:** ~1248-1344 units (increased from ~480-560 units due to longer platforms)

---

## Template for New Entries

```markdown
### YYYY-MM-DD: [Brief Description]
**Type:** ACTION | REWARD | OBSERVATION | ENVIRONMENT  
**Status:** ✅ Implemented | ⏳ Planned | ❌ Reverted

**Change:**
- Detailed description of what changed

**Before:**
- Previous state/value

**After:**
- New state/value

**Files Modified:**
- List of files changed

**Rationale:** Why this change was made

**Impact:** (Optional) Performance impact or test results

**Breaking Change:** ⚠️ (If applicable) Note if this breaks existing models
```

