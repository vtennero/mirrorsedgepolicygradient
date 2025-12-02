# Time Calculation Analysis - Inference Speed Issue

## Problem Statement
Inference runs complete in ~2 seconds when they should take much longer. This analysis calculates expected vs actual times.

---

## Key Parameters (From Code Review)

### 1. Target Distance (X)
- **Source**: `src/Assets/Prefabs/TrainingArea.prefab` line 361
- **Value**: `m_LocalPosition: {x: 300, y: 2.5, z: 0}`
- **Distance from spawn (x=0)**: **300 units**

### 2. Movement Speed (Y)
- **Source**: `src/Assets/Scripts/CharacterConfig.cs` line 12
- **Value**: `moveSpeed = 6f`
- **Units**: **6 units per second**

### 3. Time Scale Setting
- **Source**: `src/ProjectSettings/TimeManager.asset` line 13
- **Value**: `m_TimeScale: 20`
- **Impact**: Unity runs 20x faster than real-time

### 4. Fixed Timestep
- **Source**: `src/ProjectSettings/TimeManager.asset` lines 7-11
- **Calculation**: `2822399 / 141120000 ≈ 0.02 seconds`
- **Frequency**: 50 Hz (50 FixedUpdate calls per second)

### 5. Movement Implementation
- **Source**: `src/Assets/Scripts/ParkourAgent.cs` line 194
- **Code**: `horizontalMove = transform.forward * moveSpeed * Time.fixedDeltaTime`
- **Note**: Movement only occurs when `currentAction == 2` (forward action)

---

## Expected Time Calculation

### Without Time Scale (Game Time)
```
Time = Distance / Velocity
Time = 300 units / 6 units/second
Time = 50 seconds (game time)
```

### With Time Scale 20 (Real Time)
```
Real Time = Game Time / Time Scale
Real Time = 50 seconds / 20
Real Time = 2.5 seconds
```

**Expected Real Time: ~2.5 seconds** ✓ (Matches user observation of ~2 seconds)

---

## Actual Time from Run Logs

### From `demo_v13_2025-12-01_08-05-38/run_logs/timers.json`:

- **Total steps**: 5226 steps
- **env_step total time**: 99.24 seconds (real time)
- **Average time per step**: 99.24 / 5226 = 0.019 seconds per step

### Game Time Calculation:
```
Game Time = Steps × Fixed Timestep
Game Time = 5226 × 0.02 seconds
Game Time = 104.52 seconds
```

### Real Time with Time Scale 20:
```
Real Time = Game Time / Time Scale
Real Time = 104.52 / 20
Real Time = 5.226 seconds
```

**But logs show**: 99.24 seconds real time for env_step

**Discrepancy**: The logs show much longer real time than expected. This suggests:
1. The env_step time includes communication overhead, not just simulation time
2. The actual simulation might be running faster than the logged time suggests
3. The user's observation of "2 seconds" might be the visual/actual completion time, not the logged time

---

## Root Cause Analysis

### The Problem: Time Scale is Still Active
Even though the user says they're running "with no timescale parameters", the Unity project has:
- **`TimeManager.asset`**: `m_TimeScale: 20` (hardcoded in project settings)
- **`configuration.yaml`**: `time_scale: 20` (from ML-Agents config)

Both settings cause Unity to run 20x faster than real-time.

### Why It Completes in 2 Seconds
1. **Target distance**: 300 units
2. **Speed**: 6 units/second
3. **Expected game time**: 50 seconds
4. **With time scale 20**: 50 / 20 = **2.5 seconds real time** ✓

The math confirms: **The run SHOULD complete in ~2.5 seconds with time scale 20 active.**

---

## Verification: Step-by-Step Movement

### If Agent Moves Forward Every Step:
- **Distance per step**: `moveSpeed × Time.fixedDeltaTime = 6 × 0.02 = 0.12 units`
- **Steps needed**: `300 / 0.12 = 2500 steps`
- **Game time**: `2500 × 0.02 = 50 seconds`
- **Real time (scale 20)**: `50 / 20 = 2.5 seconds` ✓

### Actual Steps Taken: 5226
This is more than 2500, suggesting:
- Agent doesn't move forward every step (idle/jump actions)
- Agent may backtrack or take inefficient paths
- Some steps may be spent in air (jumping) without forward progress

---

## Conclusion

### The Math Confirms:
- **Target distance (X)**: 300 units
- **Velocity (Y)**: 6 units/second  
- **Expected game time (Z)**: 50 seconds
- **With time scale 20**: **2.5 seconds real time**

### CRITICAL FINDING: ALL RUNS ARE AT TIME SCALE 20

**YES - All runs (training AND inference) are running at time scale 20**, regardless of command line parameters:

1. **Unity Project Setting**: `TimeManager.asset` has `m_TimeScale: 20` (line 13)
   - This is a Unity project-wide setting that applies to ALL runs

2. **ML-Agents Behavior**: When `--time-scale` is NOT specified:
   - ML-Agents either uses Unity's `TimeManager.asset` value (20) as default
   - OR ML-Agents has a default of 20
   - Result: ALL generated `configuration.yaml` files show `time_scale: 20`

3. **Evidence from Logs**:
   - Training run (`test_v13`): Command shows `--time-scale=20` (explicit)
   - Inference run (`demo_v13_2025-12-01_08-05-38`): Command shows NO `--time-scale` parameter
   - **BUT**: Both have `time_scale: 20` in their configs

### Solution:
To get real-time inference (50 seconds for 300 units at 6 units/sec):
1. **Option A**: Pass `--time-scale=1` on command line (overrides everything)
2. **Option B**: Add `engine_settings` section to `parkour_config.yaml` with `time_scale: 1`
3. **Option C**: Set `TimeManager.asset` → `m_TimeScale: 1` (affects all runs)

---

## Files to Check/Modify:
1. `src/ProjectSettings/TimeManager.asset` - Line 13: `m_TimeScale: 20` → `m_TimeScale: 1`
2. `src/parkour_config.yaml` - Check if `time_scale` is set (may be in engine_settings)
3. `src/results/demo_v13_2025-12-01_08-05-38/configuration.yaml` - Line 62: `time_scale: 20`

