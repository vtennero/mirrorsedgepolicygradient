# Mirror's Edge Policy Gradient

[![Watch the video](https://img.youtube.com/vi/geAkthimaI0/maxresdefault.jpg)](https://www.youtube.com/watch?v=geAkthimaI0&list=PLA6Fy7UcWH-V7lt9u1fXue51Syps4l0Gp&index=4)

A Unity ML-Agents project training a parkour agent in a Mirror's Edge-inspired environment. The agent learns to navigate platforms using running, jumping, sprinting, and rolling.

## Summary

This project explores autonomous navigation in procedurally generated parkour environments where agents must balance speed, stamina, and aesthetic movement (dynamic rolls). We implement **episodic stochastic reward shaping** as a baseline for future human preference learning: at episode initialization, a 40% probability determines whether style bonuses are active, encouraging roll actions without requiring them in every situation.

**Key Results:**
- **Roll Usage:** 10.21% of actions (14.8× increase from baseline 0.69%)
- **Final Performance:** +78.52 average reward (16% improvement over baseline)
- **Training:** 2M steps using PPO with 28 parallel agents
- **State Space:** 14-dimensional observations (target position, velocity, platform raycasts, stamina)
- **Action Space:** 5 discrete actions (idle, jump, jog, sprint, roll)

The agent learns strategic roll timing despite high stamina cost (60 per roll), demonstrating that reward modulation can successfully encourage stylistic behaviors while maintaining task performance. This establishes training infrastructure for future RLHF integration where human preferences can replace stochastic bonuses.

## Features

- **ML-Agents Training**: PPO-based reinforcement learning for parkour navigation
- **Stamina System**: Manages sprint, jump, and roll actions
- **Style Rewards**: Bonus rewards for stylish movement (rolls)
- **Demo Mode**: Visual enhancements and UI for inference viewing
- **Character Animation**: Synchronized animations for all movement states
- **Procedural Levels**: Randomized platform generation for better generalization

## Project Structure

```
mirrorsedgepolicygradient/
├── src/                          # Unity project and Python scripts
│   ├── Assets/
│   │   ├── Scripts/              # C# gameplay, training, demo, and UI logic
│   │   ├── Characters/           # Models and animations
│   │   └── Scenes/               # Unity scenes
│   ├── parkour_config.yaml       # ML-Agents training configuration
│   ├── train_with_progress.py    # Training wrapper with progress tracking
│   └── run_inference.py          # Inference script with demo mode
├── report/                       # LaTeX report and training summaries
├── results/                      # Training run outputs (logs, models, metrics)
├── utils/
│   └── dashboard/                # Training dashboard (Flask app)
├── CONFIG_INDEX.md               # Config reference
├── ARCHITECTURE.md               # System architecture overview
├── MDP.md                        # MDP definition
├── DEMO_MODE_GUIDE.md            # Demo mode setup
├── TRAINING_LOG.md               # Training experiments and results
├── requirements.txt              # Python dependencies
├── environment.yml               # Conda environment file
└── README.md                     # This file
```
