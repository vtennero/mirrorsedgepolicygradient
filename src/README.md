# Training and Logging System

This directory contains the training scripts and configuration for the Mirror's Edge Policy Gradient project.

## Quick Start

### Training

```bash
# Activate environment
conda activate mlagents

# Navigate to src folder
cd src

# Start training (auto-generates run-id: training_YYYYMMDD_HHMMSS)
python train_with_progress.py parkour_config.yaml --force
```

Then press **Play** in Unity Editor when prompted.

### Inference (Demo Mode)

```bash
# Activate environment
conda activate mlagents

# Navigate to src folder
cd src

# Run inference with demo visualization
python run_inference.py --run-id training_20251207_210205 --time-scale 1.0
```

**Demo Mode Files:**

- **`demo_mode.env`** - Environment variable file that controls whether Unity runs in demo mode or training mode. Contains `MLAGENTS_DEMO_MODE=false` by default. When set to `true`, Unity enables visual enhancements (Faith model, materials, skyline) and applies custom time scale for slow-motion viewing. Training mode is unaffected when this is `false`.

- **`TIMESCALE.txt`** - Temporary file created automatically by `run_inference.py` in the `src` directory during inference. Contains the time scale value (e.g., "0.3" for slow-motion) that Unity reads to apply `Time.timeScale` in demo mode. This file is created before ML-Agents starts, read by Unity's `ParkourAgent.Initialize()` only when demo mode is enabled, and automatically cleaned up after inference completes. **Note:** ML-Agents doesn't apply `engine_settings.time_scale` in Unity Editor mode, so this file provides a workaround for demo mode visualization. Training is completely unaffected - the code path is skipped during training.

## Training Logging System

The project includes comprehensive logging to track training metrics and enable detailed analysis.

### Automatic Logging (Unity Side)

The `TrainingLogger.cs` class automatically logs data during training:

**Files Created:**
- `src/results/training_*/run_logs/episode_data.json` - Per-episode metrics (length, max distance, success)
- `src/results/training_*/run_logs/stamina_trajectories.json` - Stamina levels over time (sampled every 10 steps)
- `src/results/training_*/run_logs/reward_components.json` - Breakdown of reward components per episode
- `src/results/training_*/metadata.json` - Training configuration metadata (style frequency, start time)

**What Gets Logged:**
- Episode length and success/failure
- Maximum distance reached per episode
- Stamina trajectories (sampled to reduce file size)
- Reward components: progress, roll base/style, target reach, penalties, etc.
- Style episode frequency from config

### TensorBoard Data Extraction

ML-Agents logs additional metrics to TensorBoard that need to be extracted:

**Automatic Extraction:**
- Runs automatically after training completes via `train_with_progress.py`

**Manual Extraction:**
```bash
# Extract from most recent training run
python extract_tensorboard_data.py

# Extract from specific training directory
python extract_tensorboard_data.py training_20251207_210205
```

**Files Created:**
- `src/results/training_*/run_logs/action_distribution_over_time.json` - Action percentages over training steps
- `src/results/training_*/run_logs/losses_over_time.json` - Policy and value loss over training
- `src/results/training_*/run_logs/entropy_over_time.json` - Policy entropy over training

**Requirements:**
```bash
pip install tensorboard
```

### Logging Architecture

```
Training Flow:
1. Unity TrainingLogger.cs initializes on agent startup
2. Logs episode data, stamina, and reward components during training
3. Data flushed to disk every 10 episodes (reduces I/O overhead)
4. After training completes, train_with_progress.py automatically runs extract_tensorboard_data.py
5. TensorBoard event files are parsed and converted to JSON
6. All data available in run_logs/ directory for dashboard/analysis
```

### Data Files Structure

```
src/results/training_*/
├── configuration.yaml          # ML-Agents training config
├── metadata.json               # Training metadata (style frequency, etc.)
├── run_logs/
│   ├── training_status.json   # Checkpoint rewards (ML-Agents)
│   ├── timers.json             # Final aggregated metrics (ML-Agents)
│   ├── episode_data.json       # Per-episode metrics (TrainingLogger)
│   ├── stamina_trajectories.json  # Stamina over time (TrainingLogger)
│   ├── reward_components.json  # Reward breakdown (TrainingLogger)
│   ├── action_distribution_over_time.json  # From TensorBoard
│   ├── losses_over_time.json  # From TensorBoard
│   └── entropy_over_time.json # From TensorBoard
└── ParkourRunner/              # TensorBoard event files
    └── events.out.tfevents.*
```

### Monitoring Training

**TensorBoard (Real-time):**
```bash
cd src
tensorboard --logdir results/
# Open: http://localhost:6006
```

**Dashboard (Post-training analysis):**
```bash
cd utils/dashboard
python app.py
# Open: http://localhost:5000
```

### Logging Details

**Episode Data (`episode_data.json`):**
- Tracks every episode with: episode number, step count, length, max distance, success/failure
- Used for: Episode length distribution, distance traveled distribution

**Stamina Trajectories (`stamina_trajectories.json`):**
- Samples stamina every 10 timesteps to reduce file size
- Tracks: timestep, stamina level, training step count
- Used for: Stamina management analysis over episode timeline

**Reward Components (`reward_components.json`):**
- Breaks down total reward into components per episode
- Tracks: progress, roll base, roll style bonus, target reach, grounded, penalties
- Used for: Reward component breakdown analysis

**Action Distribution (`action_distribution_over_time.json`):**
- Extracted from TensorBoard logs
- Tracks action percentages (jump, jog, sprint, roll, idle) over training steps
- Used for: Action distribution over time analysis

**Losses (`losses_over_time.json`):**
- Extracted from TensorBoard logs
- Tracks policy loss and value loss over training steps
- Used for: Loss convergence analysis

**Entropy (`entropy_over_time.json`):**
- Extracted from TensorBoard logs
- Tracks policy entropy over training steps
- Used for: Entropy decay analysis

## Configuration

### Training Configuration

Edit `parkour_config.yaml` to modify:
- Learning rate, batch size, buffer size
- Network architecture
- Max training steps
- PPO hyperparameters (beta, epsilon, etc.)

### Unity Configuration

Edit `Assets/Scripts/CharacterConfig.cs` (ScriptableObject) to modify:
- Movement speeds (jog, sprint)
- Jump physics
- Stamina system parameters
- Reward values
- Style episode frequency

## Troubleshooting

### Logging Not Working

1. **Check Unity Console** for `[TrainingLogger]` messages
2. **Verify results directory** exists: `src/results/`
3. **Check file permissions** - ensure Unity can write to results directory
4. **Look for errors** in Unity console about file I/O

### TensorBoard Extraction Fails

1. **Install tensorboard**: `pip install tensorboard`
2. **Verify event files exist**: Check `src/results/training_*/ParkourRunner/` for `.tfevents` files
3. **Check training completed**: Event files are only created during active training
4. **Run manually**: `python extract_tensorboard_data.py [training_dir]`

### Missing Data Files

- **Episode data**: Check that `TrainingLogger.cs` is initialized in `ParkourAgent.cs`
- **TensorBoard data**: Ensure training ran long enough to generate event files
- **Metadata**: Should be created automatically on first episode

## Files

- `train_with_progress.py` - Training wrapper with progress tracking and auto-extraction
- `extract_tensorboard_data.py` - TensorBoard data extraction script
- `run_inference.py` - Inference/demo mode runner
- `parkour_config.yaml` - ML-Agents training configuration
- `demo_mode.env` - Environment variable file controlling demo mode (`MLAGENTS_DEMO_MODE`)
- `TIMESCALE.txt` - Temporary file (auto-created/cleaned) containing time scale value for demo mode visualization
- `Assets/Scripts/TrainingLogger.cs` - Unity-side logging system
- `Assets/Scripts/ParkourAgent.cs` - Main agent script (includes logging calls)
