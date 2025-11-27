# Quick Setup - Control Mode System

## Problem: Config file not working

If you set `control_config.json` to `"RLAgent"` but it's still player controlled, the `ControlModeManager` is probably not in your scene.

## Solution: Add ControlModeManager to Scene

### Option 1: Auto-Setup (Easiest)

1. In Unity, go to your scene
2. Create an empty GameObject (Right-click in Hierarchy → Create Empty)
3. Name it "ControlModeManager" (or anything)
4. Add the `AutoSetupControlMode` component to it
5. This will automatically create the ControlModeManager on startup

### Option 2: Manual Setup

1. In Unity, go to your scene
2. Create an empty GameObject (Right-click in Hierarchy → Create Empty)
3. Name it "ControlModeManager"
4. Add the `ControlModeManager` component to it
5. In the Inspector, you can:
   - Set `Current Mode` directly (overrides config)
   - Or leave it and it will load from `control_config.json`

### Option 3: Use Debug Keys (Quick Test)

Even without the config file, you can test by:
1. Adding ControlModeManager to scene (Option 1 or 2)
2. Press **F2** in Play mode to switch to RL Agent mode
3. Press **F1** to switch back to Player mode

## Verify It's Working

1. Open Unity Console (Window → General → Console)
2. Look for these messages:
   - `✓ Loaded control mode from config (...): RLAgent` (if config loaded)
   - `Control Mode set to: RLAgent` (when mode is set)
   - If you see warnings about components not found, make sure PlayerController and ParkourAgent are in the scene

## Config File Location

The config file should be in one of these locations:
- `Assets/StreamingAssets/control_config.json` (preferred)
- Project root: `control_config.json` (where you have it now)

The system will try both locations automatically.

## Still Not Working?

1. Check Unity Console for error messages
2. Verify `ControlModeManager` GameObject exists in scene
3. Verify `PlayerController` and `ParkourAgent` exist in scene
4. Check that `control_config.json` has correct JSON format (no extra commas, etc.)
5. Try setting mode directly in Inspector (set `Load From Config` to `false`)

