# ML-Agents Quick Reference

## Training Commands

### With Progress Percentage (Recommended)

```bash
cd src

conda activate mlagents

# Start new training with progress %
python train_with_progress.py parkour_config.yaml --run-id=test_vX --force

# Run headless with progress %
python train_with_progress.py parkour_config.yaml --run-id=test_v7_headless --no-graphics --time-scale=100 --force

# Resume training with progress %
python train_with_progress.py parkour_config.yaml --run-id=test_vX --resume
```

### Standard Commands (without progress %)

```bash
# Start new training
mlagents-learn parkour_config.yaml --run-id=test_vX --force

# Run headless
mlagents-learn parkour_config.yaml --run-id=test_v7_headless --no-graphics --time-scale=100 --force

# Resume training
mlagents-learn parkour_config.yaml --run-id=test_vX --resume

# Inference/Testing
mlagents-learn parkour_config.yaml --run-id=test_vX --resume --inference
```

## Important Notes

### "Headless" Training
- **`--no-graphics` is NOT truly headless!**
- You still need Unity Editor open and must press Play
- It only reduces rendering quality
- For true headless: Build standalone executable first

### Speed Up Training
**Best method: Use multiple training areas (18 recommended)**
- Duplicate TrainingArea GameObject 17 times in Unity
- Space them out to avoid overlap
- 18x more experience per second!
- See `MULTI_AGENT_SETUP.md` for details

## TensorBoard Monitoring

```bash
cd src
tensorboard --logdir results/
# Open: http://localhost:6006
```

## Experiment Tracking Workflow

**Before Each Training Run:**
1. Update `TRAINING_LOG.md` with new run entry
2. Document config changes
3. Note hypothesis/expected outcome

**After Each Run:**
1. Record final reward and behavior
2. Note issues/conclusions
3. Decide next experiment

**See `TRAINING_LOG.md` for full experiment history**

## Current Status

- **Latest run:** test_v7 (corrected config + 18 agents recommended)
- **Best result:** test_v5 (+5.976 at 500k steps)
- **Model location:** `src/results/test_vX/ParkourRunner.onnx`

## Recommended v7 Setup

1. **Set up 18 training areas** (see MULTI_AGENT_SETUP.md)
2. **Use balanced config:** epoch=5, horizon=128, beta=0.015
3. **Run command:**
   ```bash
   cd src
   conda activate mlagents
   python train_with_progress.py parkour_config.yaml --run-id=test_v7_18agents --time-scale=20 --force
   ```
4. **Press Play** in Unity
5. **Expect:** ~30 min for 500k steps (vs 8+ hours with 1 agent!)

## Training Output with Progress

When using `train_with_progress.py`, you'll see output like:

```
[INFO] ParkourRunner. Step: 680000. [34.0%] Time Elapsed: 735.333 s. Mean Reward: 9.899. Std of Reward: 3.424. Training.
```

The percentage shows your progress towards the `max_steps` configured in `parkour_config.yaml`.
