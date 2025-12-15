# Missing Data Points for Analysis Graphs

This document lists the data points that need to be tracked during training to create the graphs specified in `improvements.md`.

## Current Data Availability

### Available Data:
- **Checkpoint rewards over time** (from `training_status.json`) - Used for Graph 1
- **Final action distribution percentages** (from `timers.json`) - Only final values, not time series
- **Final policy/value loss** (from `timers.json`) - Only final values, not time series
- **Final entropy** (from `timers.json`) - Only final value, not time series

### Missing Data:
Most graphs require **time series data** (metrics tracked at regular intervals during training), but we only have:
- Checkpoint snapshots (every 500k steps)
- Final aggregated values

---

## Graph 2: Action Distribution Over Time

**Required Data:**
- Action distribution percentages tracked at regular intervals (e.g., every 20k steps or every summary period)
- For each interval, record:
  - Training step number
  - Percentage of Idle actions
  - Percentage of Jump actions
  - Percentage of Jog actions
  - Percentage of Sprint actions
  - Percentage of Roll actions

**Current Status:** ✅ Available via TensorBoard extraction
- ML-Agents logs action percentages to TensorBoard via `Academy.Instance.StatsRecorder.Add()` in `ParkourAgent.cs`
- Extract using: `python src/extract_tensorboard_data.py [training_dir]`
- Output: `src/results/training_*/run_logs/action_distribution_over_time.json`

**File Location:**
- Logged in: `src/Assets/Scripts/ParkourAgent.cs` (lines 904-908)
- Extracted from: TensorBoard event files in training directory
- Saved to: `src/results/training_*/run_logs/action_distribution_over_time.json`

---

## Graph 4: Roll Usage vs Style Episode Frequency

**Required Data:**
- For each training run with different style frequencies:
  - Style episode frequency (0%, 15%, 40%)
  - Final roll usage percentage
  - Error bars if multiple runs with same frequency

**Current Status:** ⚠️ Partially Available
- Style frequency is stored in `src/Assets/Scripts/CharacterConfig.cs` (Unity ScriptableObject)
- Final roll usage is available in `timers.json` as `Actions.RollPercentage.mean.current`
- **Problem:** Style frequency is not stored in the results directory - it's in the Unity asset file
- **Solution:** Need to either:
  1. Export style frequency to `configuration.yaml` or a metadata file during training
  2. Or manually track which runs used which frequency

**Where to Track:**
- Style frequency: `src/Assets/Scripts/CharacterConfig.cs` → `styleEpisodeFrequency` (line 139)
- Roll usage: Already tracked in `timers.json`
- **Action Needed:** Add style frequency to training metadata (e.g., in `configuration.yaml` or a new `metadata.json`)

---

## Graph 5: Episode Length Distribution

**Required Data:**
- Per-episode episode lengths (not just mean)
- For each episode:
  - Episode length in steps
  - Success/failure indicator (or episode reward to infer success)

**Current Status:** ❌ Not Available
- Only mean episode length is available in `timers.json`
- Need per-episode data to create histogram

**Where to Track:**
- Episode length is logged per episode in `ParkourAgent.cs` (line 893)
- Need to aggregate per-episode data, not just mean
- **Action Needed:** Modify logging to save per-episode lengths to a file (e.g., `episode_data.json`)

**File Location (per CONFIG_INDEX.md):**
- Logged in: `src/Assets/Scripts/ParkourAgent.cs` (line 893)
- Should be saved to: `src/results/training_*/run_logs/episode_data.json` (needs to be created)

---

## Graph 6: Stamina Management Over Episode Timeline

**Required Data:**
- Per-timestep stamina levels during episodes
- For multiple episodes (to show variation):
  - Episode identifier
  - Timestep (0 to ~850)
  - Stamina level (0-100)

**Current Status:** ❌ Not Available
- Stamina is tracked in-game but not logged to files
- Need per-timestep logging during episodes

**Where to Track:**
- Stamina is managed in `ParkourAgent.cs` but not logged
- **Action Needed:** Add logging to record stamina at each timestep during episodes
- Save to: `src/results/training_*/run_logs/stamina_trajectories.json`

**File Location (per CONFIG_INDEX.md):**
- Stamina system: `src/Assets/Scripts/CharacterConfig.cs` (stamina parameters)
- Stamina logic: `src/Assets/Scripts/ParkourAgent.cs` (consumption/regen logic)
- **Action Needed:** Add logging in `ParkourAgent.cs` to track stamina over time

---

## Graph 7: Policy Loss and Value Loss Over Training

**Required Data:**
- Policy loss tracked at regular intervals (e.g., every 20k steps or every update)
- Value loss tracked at regular intervals
- For each interval:
  - Training step number
  - Policy loss value
  - Value loss value

**Current Status:** ✅ Available via TensorBoard extraction
- ML-Agents PPO trainer logs policy and value losses to TensorBoard
- Extract using: `python src/extract_tensorboard_data.py [training_dir]`
- Output: `src/results/training_*/run_logs/losses_over_time.json`

**File Location:**
- Computed by: ML-Agents PPO trainer (Python side)
- Extracted from: TensorBoard event files in training directory
- Saved to: `src/results/training_*/run_logs/losses_over_time.json`

---

## Graph 8: Entropy Over Training

**Required Data:**
- Policy entropy tracked at regular intervals (e.g., every 20k steps)
- For each interval:
  - Training step number
  - Policy entropy value

**Current Status:** ✅ Available via TensorBoard extraction
- ML-Agents PPO trainer logs entropy to TensorBoard
- Extract using: `python src/extract_tensorboard_data.py [training_dir]`
- Output: `src/results/training_*/run_logs/entropy_over_time.json`

**File Location:**
- Computed by: ML-Agents PPO trainer (Python side)
- Extracted from: TensorBoard event files in training directory
- Saved to: `src/results/training_*/run_logs/entropy_over_time.json`

---

## Graph 9: Distance Traveled Distribution

**Required Data:**
- Per-episode maximum distance reached
- For each episode:
  - Maximum distance traveled (units)
  - Success/failure indicator (optional, for overlay)

**Current Status:** ⚠️ Partially Available
- Mean max distance is available in `timers.json` as `Episode.MaxDistance.mean.current`
- Need per-episode data to create histogram

**Where to Track:**
- Max distance is logged per episode in `ParkourAgent.cs` (line 894)
- Need to aggregate per-episode data, not just mean
- **Action Needed:** Modify logging to save per-episode max distances to a file

**File Location (per CONFIG_INDEX.md):**
- Logged in: `src/Assets/Scripts/ParkourAgent.cs` (line 894)
- Should be saved to: `src/results/training_*/run_logs/episode_data.json` (needs to be created)

---

## Graph 10: Reward Component Breakdown Over Time

**Required Data:**
- Per-timestep or per-episode reward components
- For each interval:
  - Training step number (or episode number)
  - Progress reward contribution
  - Roll reward contribution (base + style)
  - Target reach reward
  - Penalties (fall, timeout, etc.)

**Current Status:** ❌ Not Available
- Only total episode reward is tracked
- Individual reward components are not logged separately

**Where to Track:**
- Reward components are computed in `ParkourAgent.cs` but not logged separately
- **Action Needed:** Add logging to track each reward component separately
- Save to: `src/results/training_*/run_logs/reward_components.json`

**File Location (per CONFIG_INDEX.md):**
- Reward values: `src/Assets/Scripts/CharacterConfig.cs` (reward parameters)
- Reward computation: `src/Assets/Scripts/ParkourAgent.cs` (reward logic)
- **Action Needed:** Add component-level logging in `ParkourAgent.cs`

---

## Summary of Implementation Status

### ✅ Implemented (All Graphs Now Supported):

1. **Action Distribution Over Time (Graph 2):** ✅
   - Extracted from TensorBoard logs using `extract_tensorboard_data.py`
   - Output: `action_distribution_over_time.json`

2. **Policy/Value Loss Over Training (Graph 7):** ✅
   - Extracted from TensorBoard logs using `extract_tensorboard_data.py`
   - Output: `losses_over_time.json`

3. **Entropy Over Training (Graph 8):** ✅
   - Extracted from TensorBoard logs using `extract_tensorboard_data.py`
   - Output: `entropy_over_time.json`

4. **Roll Usage vs Style Frequency (Graph 4):** ✅
   - Style frequency exported to `metadata.json` by `TrainingLogger.cs`
   - Roll usage available in `timers.json`

5. **Episode Length Distribution (Graph 5):** ✅
   - Logged by `TrainingLogger.cs` in `ParkourAgent.cs`
   - Output: `episode_data.json`

6. **Stamina Management (Graph 6):** ✅
   - Logged by `TrainingLogger.cs` in `ParkourAgent.cs`
   - Output: `stamina_trajectories.json` (sampled every 10 steps)

7. **Distance Traveled Distribution (Graph 9):** ✅
   - Logged by `TrainingLogger.cs` in `ParkourAgent.cs`
   - Output: `episode_data.json` (same file as Graph 5)

8. **Reward Component Breakdown (Graph 10):** ✅
   - Logged by `TrainingLogger.cs` in `ParkourAgent.cs`
   - Output: `reward_components.json`

---

## Implementation Notes

### Where Data is Currently Logged:
- **TensorBoard:** ML-Agents logs many metrics to TensorBoard by default
- **timers.json:** Final aggregated metrics
- **training_status.json:** Checkpoint rewards

### Implementation Complete:
1. ✅ **TensorBoard Extraction:** `extract_tensorboard_data.py` extracts Graphs 2, 7, 8
2. ✅ **Unity Logging:** `TrainingLogger.cs` logs Graphs 4, 5, 6, 9, 10
3. ✅ **Automatic Extraction:** `train_with_progress.py` automatically runs extraction after training

### File Structure (Implemented):
```
src/results/training_*/
├── configuration.yaml (existing)
├── metadata.json (Graph 4 - style frequency)
├── run_logs/
│   ├── training_status.json (existing)
│   ├── timers.json (existing)
│   ├── episode_data.json (Graphs 5, 9)
│   ├── stamina_trajectories.json (Graph 6)
│   ├── reward_components.json (Graph 10)
│   ├── action_distribution_over_time.json (Graph 2 - from TensorBoard)
│   ├── losses_over_time.json (Graph 7 - from TensorBoard)
│   └── entropy_over_time.json (Graph 8 - from TensorBoard)
└── ParkourRunner/ (TensorBoard event files)
```

### Usage:
- **During Training:** Data is automatically logged by `TrainingLogger.cs`
- **After Training:** Run `python src/extract_tensorboard_data.py` (or it runs automatically via `train_with_progress.py`)
- **Manual Extraction:** `python src/extract_tensorboard_data.py [training_dir]`
