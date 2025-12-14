# Configuration Files Index

Quick reference guide to where parameters are defined in the codebase.

## Training Configuration

**`src/parkour_config.yaml`** - ML-Agents PPO hyperparameters: learning rate (3.0e-4), batch size (1024), buffer size (10240), beta (0.1), epsilon (0.2), network architecture (2×256 actor, 2×128 critic), time horizon (128), max steps (2M).

**`src/Assets/Scripts/CharacterConfig.cs`** - Unity ScriptableObject: movement speeds (jog 6, sprint 12), jump physics (force 8, boost 10), stamina system (max 100, consumption 20/sec, regen 30/sec, jump cost 20, roll cost 60), reward values (progress 0.1/unit, target reach +10, roll base +0.5, roll style +1.5), episode timeout (100s), style frequency (40%).

**`src/Assets/Scripts/TrainingArea.cs`** - Environment generation: platform count (20), spacing (15), size (24×10×6), randomization (gaps 2.5-4.5, widths 20-28, heights -0.5 to 5.0), target offset (5 units beyond last platform).

## Environment Files

**`src/demo_mode.env`** - Demo/inference mode flags (MLAGENTS_DEMO_MODE=true).

## Results & Logs

**`src/results/training_*/run_logs/training_status.json`** - Checkpoint rewards and step progression.

**`src/results/training_*/run_logs/timers.json`** - Final metrics: policy loss, value loss, action distributions (jog/sprint/jump/roll/idle %), episode stats (mean reward, max distance, length).

