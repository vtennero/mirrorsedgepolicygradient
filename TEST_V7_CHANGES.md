# Test V7 Changes Summary

## Configuration Changes (parkour_config.yaml)

### Hyperparameter Adjustments
| Parameter | Old Value | New Value | Reason |
|-----------|-----------|-----------|---------|
| `beta` (entropy) | 0.005 | 0.015 | **3x increase** - Better exploration, reduces getting stuck in local optima |
| `num_epoch` | 3 | 12 | **4x increase** - Better data utilization, more gradient updates per batch |
| `time_horizon` | 64 | 192 | **3x increase** - Better credit assignment for longer sequences |

## Unity Agent Enhancements (ParkourAgent.cs)

### New Metrics Tracked
The agent now tracks and logs the following custom statistics to TensorBoard:

#### Episode Metrics
- **Episode/TotalReward**: Cumulative reward for the entire episode
- **Episode/Length**: Duration of the episode in seconds
- **Episode/MaxDistance**: Maximum forward distance achieved before episode end

#### Action Distribution Metrics
- **Actions/JumpCount**: Total number of jump actions taken
- **Actions/ForwardCount**: Total number of forward movement actions
- **Actions/IdleCount**: Total number of idle/no-op actions
- **Actions/JumpPercentage**: Percentage of actions that were jumps
- **Actions/ForwardPercentage**: Percentage of actions that were forward movements
- **Actions/IdlePercentage**: Percentage of actions that were idle

### Implementation Details
- Metrics are logged at the end of each episode via `LogEpisodeStats()`
- Metrics include the end reason (Success, Fell, or Timeout)
- All metrics use Unity's `Academy.Instance.StatsRecorder` for TensorBoard integration

## Expected Training Improvements

### From Entropy Increase (0.005 → 0.015)
- **More exploration**: Agent will try different action sequences
- **Less premature convergence**: Prevents getting stuck in suboptimal policies
- **Better generalization**: Explores more of the state-action space

### From Epoch Increase (3 → 12)
- **Better gradient updates**: More passes through the same data
- **Improved sample efficiency**: Makes better use of collected experience
- **More stable learning**: Smoother policy updates

### From Horizon Increase (64 → 192)
- **Better temporal credit assignment**: Links actions to distant rewards
- **Improved long-term planning**: Understands consequences over longer sequences
- **Better for parkour**: Captures full jump-land-run sequences

## Training Command

```bash
cd src
conda activate mlagents
mlagents-learn parkour_config.yaml --run-id=test_v7_headless --no-graphics --time-scale=100 --force
```

## Monitoring Training

### TensorBoard
```bash
tensorboard --logdir src/results/test_v7_headless
```

### Key Metrics to Watch
1. **Episode/TotalReward**: Should increase over time
2. **Episode/MaxDistance**: Should steadily grow
3. **Actions/ForwardPercentage**: Should increase (more running, less idle)
4. **Actions/JumpPercentage**: Should stabilize around optimal jump frequency
5. **Policy/Entropy**: Should stay around 0.015 (our new beta value)
6. **Losses/Value Loss**: Should decrease over time
7. **Policy/Learning Rate**: Will decrease linearly (using linear schedule)

## Advanced PPO Metrics (Optional)

See `ENABLE_PPO_METRICS.md` for instructions on enabling:
- **Explained Variance**: How well the value function predicts returns
- **Approximate KL**: Magnitude of policy changes between updates
- **Clip Fraction**: Proportion of policy updates being clipped

## Files Modified
- `src/parkour_config.yaml` - Updated hyperparameters
- `src/Assets/Scripts/ParkourAgent.cs` - Added metric tracking
- `ENABLE_PPO_METRICS.md` - Guide for enabling advanced metrics (NEW)
- `TEST_V7_CHANGES.md` - This summary document (NEW)

