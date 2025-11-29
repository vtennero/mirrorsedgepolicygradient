# Enabling Advanced PPO Metrics in ML-Agents

This guide explains how to enable logging of **Explained Variance**, **Approximate KL Divergence**, and **Clip Fraction** in ML-Agents.

## Overview

These metrics are computed internally by the PPO algorithm but not logged by default. To enable them, you need to modify the ML-Agents Python package.

## Option 1: Quick Patch (Recommended)

### Step 1: Locate Your ML-Agents Installation

Find where ML-Agents is installed:

```bash
python -c "import mlagents; print(mlagents.__file__)"
```

This will show something like: `C:\Users\...\site-packages\mlagents\__init__.py`

Navigate to the parent directory (the one containing `mlagents_envs` and `mlagents`).

### Step 2: Find the PPO Optimizer File

The file you need to modify is typically located at:
```
mlagents/trainers/ppo/optimizer.py
```

Or:
```
mlagents/trainers/optimizer/torch_optimizer.py
```

### Step 3: Add Metric Logging

In the PPO update function (usually named `update()` or `update_policy()`), add the following after policy updates:

```python
# After computing policy loss and updating
with torch.no_grad():
    # Approximate KL Divergence
    approx_kl = ((old_log_probs - log_probs) ** 2).mean().item()
    self.stats_reporter.add_stat("Policy/Approximate KL", approx_kl)
    
    # Clip Fraction
    clip_fraction = ((torch.abs(ratio - 1.0) > self.epsilon)).float().mean().item()
    self.stats_reporter.add_stat("Policy/Clip Fraction", clip_fraction)
    
    # Explained Variance (for value function)
    y_pred = value_estimates
    y_true = returns
    var_y = torch.var(y_true)
    explained_var = 1 - torch.var(y_true - y_pred) / (var_y + 1e-8)
    self.stats_reporter.add_stat("Policy/Explained Variance", explained_var.item())
```

## Option 2: Use Development Version

Install ML-Agents from source and modify before installing:

```bash
# Clone ML-Agents
git clone https://github.com/Unity-Technologies/ml-agents.git
cd ml-agents

# Checkout the version you're using (e.g., release_20)
git checkout release_20

# Edit the files as described in Option 1
# Then install from source
pip install -e ./ml-agents-envs
pip install -e ./ml-agents
```

## Option 3: Custom Wrapper (No ML-Agents Modification)

While you can't directly access PPO internals, you can approximate some metrics from Unity:

**Already implemented in ParkourAgent.cs** (test_v7):
- Episode Total Reward
- Episode Length
- Max Distance Reached
- Action Distribution (Jump/Forward/Idle counts and percentages)

These custom metrics will appear in TensorBoard under:
- `Episode/TotalReward`
- `Episode/Length`
- `Episode/MaxDistance`
- `Actions/JumpCount`, `Actions/ForwardCount`, `Actions/IdleCount`
- `Actions/JumpPercentage`, `Actions/ForwardPercentage`, `Actions/IdlePercentage`

## Verifying Metrics in TensorBoard

After implementing the changes and running training:

```bash
tensorboard --logdir src/results/test_v7_headless
```

You should see additional metrics in the "Policy" section:
- **Approximate KL**: Should stay small (< 0.01 typically)
- **Clip Fraction**: Higher values (0.1-0.3) indicate policy is changing rapidly
- **Explained Variance**: Should increase towards 1.0 as training progresses

## What These Metrics Tell You

### Explained Variance (0 to 1, higher is better)
- **Close to 1.0**: Value function accurately predicts returns
- **Close to 0**: Value function is not learning well
- **Negative**: Value function is worse than predicting the mean

### Approximate KL Divergence (lower is better)
- **< 0.01**: Policy updates are conservative (good)
- **0.01 - 0.05**: Moderate policy changes
- **> 0.05**: Policy is changing too rapidly (may be unstable)

### Clip Fraction (0 to 1)
- **0.0 - 0.1**: Small policy updates (may be too conservative)
- **0.1 - 0.3**: Healthy policy updates
- **> 0.3**: Large policy changes (may indicate instability)

## Troubleshooting

If you see import errors after modifying ML-Agents:
```bash
pip uninstall mlagents mlagents-envs
pip install mlagents==0.30.0  # Or your version
```

Then reapply the modifications.

## Note for Test V7

For the current test_v7_headless run, the following changes have been applied:

**Config Changes (parkour_config.yaml):**
- `beta` (entropy): 0.005 → 0.015 (3x increase for better exploration)
- `num_epoch`: 3 → 12 (4x increase for better data utilization)
- `time_horizon`: 64 → 192 (3x increase for better credit assignment)

**Unity Agent Changes (ParkourAgent.cs):**
- Added episode reward tracking
- Added action distribution tracking
- Added max distance tracking
- Custom stats logged to TensorBoard

These changes should significantly improve training diagnostics even without the PPO-specific metrics.

