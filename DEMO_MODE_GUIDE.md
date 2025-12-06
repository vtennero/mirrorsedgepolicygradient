# Demo Mode Guide



## Running Demo/Inference

Use the wrapper script:
```bash
cd src
python run_inference.py                    # Default: time-scale=1.0 (normal speed)
python run_inference.py --time-scale=0.1   # 10x slower
python run_inference.py --time-scale=0.3   # optimal for demo
python run_inference.py --time-scale=0.01  # 100x slower
```

The script will:
1. Show list of training runs (select by number)
2. Auto-generate run ID with timestamp
3. Create temp config with your time scale
4. Start ML-Agents inference

Then press **Play** in Unity Editor.

## Why TIMESCALE.txt File?

**Problem:** ML-Agents doesn't apply `engine_settings.time_scale` in Unity Editor mode. The config file has the correct value, but Unity ignores it.

**Solution:** 
1. Python script writes `TIMESCALE.txt` with the time scale value **before** starting ML-Agents
2. Unity reads `TIMESCALE.txt` in `ParkourAgent.Initialize()` **only when demo mode is enabled**
3. Unity applies it to `Time.timeScale` manually
4. Script cleans up the file after

**Safety:** This **ONLY** works in demo mode (`MLAGENTS_DEMO_MODE=true`). Training is completely unaffected - the code path is skipped during training.

## What Demo Mode Does

- Visual enhancements (Faith model, materials, skyline)
- Custom time scale for slow-motion viewing
- Single agent mode (disables extra TrainingAreas)

## Training

Just run normally (demo mode disabled):
```bash
cd src
mlagents-learn parkour_config.yaml --run-id=training --force --no-graphics --time-scale=50
```
