# RL Agent Integration Guide

## Overview

This guide explains how the Player Control and RL Agent Control systems work together.

## Architecture

### Control Mode System

The game uses a **ControlModeManager** to switch between three control modes:

1. **Player Mode** - Human player control (WASD, mouse look, space to jump)
2. **RL Agent Mode** - Reinforcement Learning agent control (for training)
3. **Heuristic Mode** - Manual control via ML-Agents heuristic (for testing RL behavior)

### Components

- **ControlModeManager.cs** - Manages switching between control modes
- **PlayerController.cs** - Handles human player input and movement
- **ParkourAgent.cs** - ML-Agents RL agent that controls the character
- **CharacterMovement.cs** - Shared movement component (optional, for future use)

## How It Works

### Mode Switching

The `ControlModeManager` automatically:
- Enables/disables `PlayerController` based on the current mode
- Enables/disables `ParkourAgent` based on the current mode
- Manages cursor lock state
- Loads mode from config file on startup

### PlayerController Behavior

- Only processes input when `ControlMode == Player`
- Skips all Update() logic when in RL Agent mode
- Uses Unity's Input System for keyboard/mouse input

### ParkourAgent Behavior

- Only processes actions when `ControlMode == RLAgent` or `Heuristic`
- Implements its own movement physics (gravity, jumping, forward movement)
- Uses FixedUpdate() for consistent physics simulation

## Configuration

### Config File Method

1. Create or edit `control_config.json` in the project root or `StreamingAssets/` folder:
```json
{
  "controlMode": "RLAgent"
}
```

Valid values: `"Player"`, `"RLAgent"`, `"Heuristic"`

2. The config is loaded automatically on game start if `loadFromConfig = true` in ControlModeManager

### Unity Inspector Method

1. Select the GameObject with `ControlModeManager` component
2. Set `Current Mode` in the Inspector
3. Set `Load From Config` to `false` if you want to use Inspector value instead

### Runtime Switching (Debug)

In Editor or Development builds, you can switch modes at runtime:
- **F1** - Switch to Player mode
- **F2** - Switch to RL Agent mode  
- **F3** - Switch to Heuristic mode

## Setup Instructions

### For Training RL Agent

1. **Set Control Mode to RLAgent:**
   - Edit `control_config.json` and set `"controlMode": "RLAgent"`
   - OR set it in Unity Inspector on ControlModeManager

2. **Start Training:**
   ```bash
   mlagents-learn src/parkour_config.yaml --run-id=parkour_v1
   ```

3. **The agent will automatically control the character during training**

### For Playing the Game

1. **Set Control Mode to Player:**
   - Edit `control_config.json` and set `"controlMode": "Player"`
   - OR set it in Unity Inspector

2. **Play the game normally with WASD and mouse**

### For Testing RL Behavior Manually

1. **Set Control Mode to Heuristic:**
   - Edit `control_config.json` and set `"controlMode": "Heuristic"`
   - OR set it in Unity Inspector

2. **Use Space to jump, W to move forward** (same as RL agent actions)

## Important Notes

### Component Setup

- Both `PlayerController` and `ParkourAgent` can be on the same GameObject
- They share the same `CharacterController` component
- Only one is active at a time based on control mode

### Movement Physics

- **PlayerController** uses its own gravity/velocity system in `Update()`
- **ParkourAgent** uses its own gravity/velocity system in `FixedUpdate()`
- Both use the same `CharacterController` but don't interfere because only one is active

### Training Considerations

- **Always set mode to RLAgent before training**
- Player input will interfere with training if Player mode is active
- The agent needs consistent control to learn properly

## Future Enhancements

The system is designed to be extensible:

1. **Menu System** - Add a UI menu to switch modes at runtime
2. **Shared Movement Component** - Refactor to use `CharacterMovement` for both controllers
3. **Mode Persistence** - Save mode preference to PlayerPrefs
4. **Training Mode Detection** - Automatically switch to RLAgent when ML-Agents training starts

## Troubleshooting

### Agent Not Moving During Training

- Check that `ControlModeManager.CurrentMode == RLAgent`
- Verify `ParkourAgent.enabled == true` in Inspector
- Check that `PlayerController.enabled == false` (should be automatic)

### Player Controls Not Working

- Check that `ControlModeManager.CurrentMode == Player`
- Verify `PlayerController.enabled == true` in Inspector
- Check that cursor is locked (should be automatic in Player mode)

### Both Systems Active (Conflicts)

- This shouldn't happen, but if it does:
  - Check ControlModeManager is working correctly
  - Verify only one component is enabled at a time
  - Check for errors in Console

