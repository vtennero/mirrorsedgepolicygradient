# Test V7 - CORRECTED Configuration

## What Went Wrong Initially

### The Problem
Training was **extremely slow** (1+ hour, still not done) because:
1. **`num_epoch: 12`** - Made each training update 4x slower
2. **`time_horizon: 192`** - Made it collect 3x more data before training
3. **Only 1 agent** - Limited experience collection
4. Combined effect: ~12x slower than before!

### The Misunderstanding About Headless
- `--no-graphics` **does NOT** make it truly headless
- You still need Unity Editor open and must press Play
- True headless requires building a standalone executable
- For now: Just run without `--no-graphics` since editor is required anyway

## CORRECTED Configuration

### Updated parkour_config.yaml

```yaml
behaviors:
  ParkourRunner:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 0.015          # ✅ Keep: 3x increase for exploration
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 5         # ✅ FIXED: Was 12 (too slow), now balanced
      learning_rate_schedule: linear
    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 2000000
    time_horizon: 128      # ✅ FIXED: Was 192 (too slow), now balanced
    summary_freq: 20000
```

### Changes From Original v6
| Parameter | v6 Value | v7 Initial | v7 CORRECTED | Reasoning |
|-----------|----------|------------|--------------|-----------|
| `beta` | 0.005 | 0.015 | **0.015** ✅ | Better exploration (keep) |
| `num_epoch` | 3 | 12 | **5** ✅ | Balance speed/learning |
| `time_horizon` | 64 | 192 | **128** ✅ | Balance credit/speed |

## THE KEY FIX: Multiple Agents

### Why This Matters Most
- **1 agent** = ~1,000 steps/minute = SLOW
- **18 agents** = ~18,000 steps/minute = 18x FASTER

### How to Set Up (Easy!)
1. Open `TrainingScene` in Unity
2. Select `TrainingArea` in Hierarchy
3. **Ctrl+D** to duplicate 17 times (18 total)
4. Space them out (e.g., 140 units apart in X, 30 units in Z)
5. That's it! Each area is self-contained

See `MULTI_AGENT_SETUP.md` for detailed instructions.

## Optimal Training Command

### Recommended Setup
```bash
cd src
conda activate mlagents

# Simple command (no --no-graphics needed since editor is required)
mlagents-learn parkour_config.yaml --run-id=test_v7_18agents --force
```

Then **press Play in Unity**.

### Time Scale Recommendations
- **With 1 agent:** `--time-scale=100` is fine
- **With 18 agents:** Start with `--time-scale=20`, increase if Unity handles it
- Monitor Unity performance (should stay >30 FPS)

## Expected Performance

### With 18 Agents + Corrected Config
| Metric | Expected Value |
|--------|---------------|
| Training time to 500k | ~30 minutes |
| First positive rewards | ~100k-200k steps |
| Decent parkour behavior | ~500k steps |
| Good performance | ~1M steps |

### Compare to Original Attempt
| Setup | Time to 500k |
|-------|-------------|
| 1 agent + slow config | 8+ hours ❌ |
| 1 agent + fast config | 2-3 hours ⚠️ |
| 18 agents + fast config | 30 min ✅ |

## Monitoring Training

### TensorBoard
```bash
tensorboard --logdir src/results/
# Open http://localhost:6006
```

### Key Metrics to Watch
1. **Environment/Cumulative Reward** - Should trend upward
2. **Episode/MaxDistance** (custom) - Should increase steadily
3. **Actions/ForwardPercentage** (custom) - Should increase (more running)
4. **Policy/Entropy** - Should stay around 1.5-2.0
5. **Losses/Policy Loss** - Should stabilize/decrease

### When to Stop Training
- When reward plateaus for 200k+ steps
- When behavior looks good in Unity
- Usually around 1-2M steps

## Files Modified in v7
- ✅ `src/parkour_config.yaml` - Updated hyperparameters (corrected)
- ✅ `src/Assets/Scripts/ParkourAgent.cs` - Added custom metrics logging
- ✅ `TEST_V7_CORRECTED.md` - This document
- ✅ `MULTI_AGENT_SETUP.md` - Guide for 18 agents setup
- ✅ `ENABLE_PPO_METRICS.md` - Advanced metrics guide

## Next Steps

1. **Set up 18 training areas** in Unity (see MULTI_AGENT_SETUP.md)
2. **Run the corrected training:**
   ```bash
   cd src
   conda activate mlagents
   mlagents-learn parkour_config.yaml --run-id=test_v7_18agents --force
   ```
3. **Press Play** in Unity
4. **Monitor** with TensorBoard
5. **Wait ~30-60 minutes** for meaningful results

## Lesson Learned

**More agents >> Better hyperparameters** for training speed!

- Fancy hyperparameters: 4x-12x slower per step
- 18 agents: 18x more steps per second
- Result: Multi-agent setup is the bottleneck solver! 🚀

