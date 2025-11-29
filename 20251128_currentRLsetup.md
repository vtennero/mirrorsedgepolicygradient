I'll check the configuration files and training setup to give you precise details about the RL system being used.

[4 tools called]

Based on the configuration files and training setup, here's the precise RL system and technique you're using:

## **RL Framework & Algorithm**

**Framework:** Unity ML-Agents Toolkit

**Algorithm:** **PPO (Proximal Policy Optimization)** - An on-policy, model-free actor-critic algorithm

## **Specific PPO Configuration**

### Hyperparameters:
- **Learning Rate:** 3.0e-4 (0.0003) with linear decay schedule
- **Batch Size:** 1024
- **Buffer Size:** 10,240 (10× batch size)
- **Epochs per Update:** 3
- **Time Horizon:** 64 steps
- **Discount Factor (γ):** 0.99
- **GAE Lambda (λ):** 0.95
- **Clipping Epsilon (ε):** 0.2 with linear decay
- **Entropy Coefficient (β):** 0.005 with linear decay

### Neural Network Architecture:
- **Type:** Fully-connected feedforward neural network
- **Hidden Layers:** 2 layers
- **Hidden Units:** 256 per layer
- **Input Normalization:** Enabled
- **Input Size:** 8 observations (relative target position, velocity, grounded state, obstacle distance)
- **Output:** 3 discrete actions (do nothing, jump, move forward)

### Training Setup:
- **Max Steps:** 2,000,000
- **Parallel Agents:** 9 (for faster data collection)
- **Reward Signal:** Single extrinsic reward (no curiosity or other auxiliary rewards)
- **Goal Conditioning:** Hyper network type

## **Key Technical Details**

This is a **discrete action space** PPO implementation with:
- Standard PPO clipping objective
- Generalized Advantage Estimation (GAE)
- Linear learning rate annealing
- No shared critic between policies
- No LSTM/memory component (Markovian policy)

The current best model (test_v6) achieved +8.478 reward after 2M training steps across 9 parallel agents.