# Demo Mode Guide

## Quick Start

**One-time setup:** Create `src/demo_mode.env` file with:
```
MLAGENTS_DEMO_MODE=true
```

**Run inference:**
```bash
cd src
mlagents-learn parkour_config.yaml --run-id=demo --initialize-from=test_v11 --inference
mlagents-learn parkour_config.yaml --run-id=demo_$(date +%Y-%m-%d_%H-%M-%S) --initialize-from=test_v11 --inference


```

Then press **Play** in Unity Editor.

**To disable demo mode:** Delete or rename `demo_mode.env` file.

## Unity Setup

1. Open `TrainingScene`
2. Create empty GameObject → name it `VisualEnhancer`
3. Add component: `InferenceVisualEnhancer`

Optional: Assign `idle.glb` and `MaggieAnimationController.controller` in Inspector (auto-loads if not set).

## What It Does

- Swaps capsule → Faith model
- Applies Mirror's Edge materials to platforms
- Generates city skyline (no colliders)
- Disables extra TrainingAreas (single agent mode)

## Training (No Visuals)

Just run normally without the env var:
```bash
cd src
mlagents-learn parkour_config.yaml --run-id=training --force --no-graphics --time-scale=50
```
