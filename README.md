# Mirror's Edge Policy Gradient

A Unity ML-Agents project training a parkour agent in a Mirror's Edge-inspired environment. The agent learns to navigate platforms using running, jumping, sprinting, and rolling.

## Features

- **ML-Agents Training**: PPO-based reinforcement learning for parkour navigation
- **Stamina System**: Manages sprint, jump, and roll actions
- **Style Rewards**: Bonus rewards for stylish movement (rolls)
- **Demo Mode**: Visual enhancements and UI for inference viewing
- **Character Animation**: Synchronized animations for all movement states
- **Procedural Levels**: Randomized platform generation for better generalization

## Requirements

- **Unity**: 2022.3+ with ML-Agents package (com.unity.ml-agents 3.0.0+)
- **Python**: 3.10.12 (recommended)
- **ML-Agents**: 1.1.0
- **PyTorch**: 2.2.2 with CUDA 12.1 support (or CPU version)

## Installation

### Option 1: Using Conda (Recommended)

```bash
# Clone the repository
git clone https://github.com/yourusername/mirrorsedgepolicygradient.git
cd mirrorsedgepolicygradient

# Create conda environment from file
conda env create -f environment.yml

# Activate environment
conda activate mlagents
```

### Option 2: Using pip + venv

```bash
# Clone the repository
git clone https://github.com/yourusername/mirrorsedgepolicygradient.git
cd mirrorsedgepolicygradient

# Create virtual environment
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate
# On Linux/Mac:
source venv/bin/activate

# Install requirements
pip install -r requirements.txt

# Note: If on Linux/Mac, remove the Windows-specific packages first:
# pip install -r requirements.txt --no-deps
```

### Verify Installation

```bash
mlagents-learn --help
```

You should see the ML-Agents help message.

## Quick Start

### Training

```bash
# Activate environment
conda activate mlagents  # or: source venv/bin/activate

# Navigate to src folder
cd src

# Start training
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
python run_inference.py --run-id test_v11 --time-scale 1.0
```

This will:
- Load the trained model
- Enable visual enhancements (Faith character, city skyline)
- Show stamina bar and screen flashes
- Display run statistics after each episode

## Project Structure

```
mirrorsedgepolicygradient/
├── src/                          # Unity project and Python scripts
│   ├── Assets/
│   │   ├── Scripts/              # C# scripts (see Scripts/README.md)
│   │   ├── Characters/           # Character models and animations
│   │   └── Scenes/               # Unity scenes
│   ├── parkour_config.yaml       # ML-Agents training configuration
│   ├── train_with_progress.py    # Training wrapper with progress tracking
│   └── run_inference.py          # Inference script with demo mode
├── utils/
│   └── dashboard/                # Training dashboard (Flask app)
├── results/                      # Training run results
├── requirements.txt              # Python dependencies
├── environment.yml               # Conda environment file
└── README.md                     # This file
```

## Configuration

### Training Configuration

Edit `src/parkour_config.yaml` to adjust:
- Learning rate
- Batch size
- Max training steps
- Network architecture
- Reward scaling

### Character Configuration

In Unity, edit the `CharacterConfig` ScriptableObject to adjust:
- Movement speeds
- Jump force and gravity
- Stamina costs and regen
- Reward multipliers
- Camera settings

See `CONFIG_INDEX.md` for complete configuration reference.

## Control Modes

The project supports three control modes:

1. **Player** - Manual keyboard/mouse control (WASD + Space + Shift)
2. **RLAgent** - AI agent control (training or inference)
3. **Heuristic** - Manual control via ML-Agents heuristic

Switch modes by editing `control_config.json` or pressing F1/F2/F3 in Unity Editor.

## Documentation

- `src/Assets/Scripts/README.md` - Complete script documentation
- `CONFIG_INDEX.md` - Configuration reference
- `ARCHITECTURE.md` - System architecture overview
- `MDP.md` - Markov Decision Process definition
- `DEMO_MODE_GUIDE.md` - Demo mode setup
- `TRAINING_LOG.md` - Training experiments and results

## Utilities

This project includes helper scripts for managing training runs:

- **`check_failed_runs.py`** - Find and clean up failed/incomplete training runs
- **`dashboard/`** - Web-based training run visualizer

See [`utils/README.md`](utils/README.md) for detailed documentation and usage examples

## Common Issues

### "Unity ML-Agents Communicator port [5004] already in use"
- Close previous Unity instances or change the port in Unity Project Settings

### Windows: torch installation fails
- Install torch separately: `pip install torch==2.2.2+cu121 -f https://download.pytorch.org/whl/torch_stable.html`

### Linux/Mac: pywin32 errors
- Remove `pywin32` and `pypiwin32` from `requirements.txt` (they're Windows-only)

### Training doesn't start
- Make sure Unity is in Play mode after running `mlagents-learn`
- Check that the scene has TrainingArea objects with agents

## Credits

- Unity ML-Agents Toolkit
- Mirror's Edge (inspiration for visual style)
- Character models from various sources (see `src/Assets/Characters/README.md`)

## License

[Your License Here]
