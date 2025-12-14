# Scripts Documentation

This folder contains all C# scripts for the Mirror's Edge-inspired parkour agent training system.

## Table of Contents

- [Core Systems](#core-systems)
- [Training & Environment](#training--environment)
- [Configuration Management](#configuration-management)
- [Control & Input](#control--input)
- [Animation](#animation)
- [Camera](#camera)
- [Demo & Inference Mode](#demo--inference-mode)
- [Setup & Utilities](#setup--utilities)

---

## Core Systems

### `ParkourAgent.cs`
**Purpose**: Main RL agent that learns parkour movement using Unity ML-Agents.

**Key Features**:
- Discrete action space: idle, jump, jog, sprint, roll
- Stamina system limiting sprint/jump/roll actions
- Observations: position relative to target, velocity, ground detection, platform raycasts
- Reward shaping: progress rewards, time penalties, style bonuses for rolls
- Handles episode lifecycle and automatic resets

**Usage**: Attach to the agent GameObject. Requires CharacterController component.

### `PlayerController.cs`
**Purpose**: Manual player control for testing and gameplay.

**Key Features**:
- WASD movement with mouse look (third-person)
- Progressive sprint system with acceleration
- Jump mechanics with forward momentum
- Animation state management
- Integrates with ControlModeManager for mode switching

**Usage**: Attach to player GameObject. Disabled when in RL Agent mode.

---

## Training & Environment

### `TrainingArea.cs`
**Purpose**: Self-contained training environment for a single agent.

**Key Features**:
- Procedural platform generation with configurable randomization
- Height and gap variation for better generalization
- Jump feasibility validation (physics-based)
- Target position management
- Episode reset with platform regeneration

**Usage**: Create empty GameObject, attach script, assign agent/spawn/target references. Platforms generate automatically.

### `LevelGenerator.cs`
**Purpose**: Simple procedural level generator for platforms.

**Key Features**:
- Creates platforms in a linear sequence
- Configurable spacing, height variation, and platform size
- Can use prefabs or generate primitives
- Gizmo visualization in editor

**Usage**: Attach to GameObject in scene. Platforms generate on Start().

---

## Configuration Management

### `CharacterConfig.cs`
**Purpose**: ScriptableObject defining all character parameters (movement, jumping, rewards, etc.).

**Key Features**:
- Centralized configuration for all character-related values
- Movement speeds, jump force, gravity settings
- Stamina system parameters
- RL reward multipliers and penalties
- Camera settings
- Style action rewards

**Usage**: Create via `Assets > Create > Parkour > Character Config`. Reference in CharacterConfigManager.

### `CharacterConfigManager.cs`
**Purpose**: Singleton manager providing global access to CharacterConfig.

**Key Features**:
- Automatically finds or creates config instance
- Provides `CharacterConfigManager.Config` for easy access
- Creates default config if none assigned

**Usage**: Automatically initialized. Access config via `CharacterConfigManager.Config` anywhere in code.

---

## Control & Input

### `ControlModeManager.cs`
**Purpose**: Manages switching between Player, RL Agent, and Heuristic control modes.

**Key Features**:
- Loads control mode from `control_config.json`
- Runtime mode switching (F1/F2/F3 keys in editor)
- Enables/disables PlayerController and ParkourAgent based on mode
- Cursor lock management

**Usage**: Attach to GameObject in scene. Loads config automatically on Start().

### `ControlModeDebugger.cs`
**Purpose**: Debugging tool to diagnose control mode issues.

**Key Features**:
- Logs current mode and component states every 2 seconds
- Context menu functions to force mode changes
- Validates PlayerController and ParkourAgent status

**Usage**: Attach to any GameObject for debugging. Check console for status logs.

### `AutoSetupControlMode.cs`
**Purpose**: Automatically creates ControlModeManager if it doesn't exist.

**Key Features**:
- Runs before other scripts (execution order -100)
- Creates ControlModeManager if missing
- Ensures control system works even if manually forgotten

**Usage**: Attach to any GameObject in scene as a safety net.

---

## Animation

### `AgentAnimationSync.cs`
**Purpose**: Synchronizes agent actions with character animations.

**Key Features**:
- Maps ParkourAgent actions to Animator parameters
- Handles idle, jog, sprint, jump, roll animations
- Jump state machine (start → loop → land)
- Smooth animation speed scaling based on movement
- Works with both RL agent and player controller

**Usage**: Attach to agent GameObject with Animator. Automatically detects ParkourAgent or PlayerController.

---

## Camera

### `CameraFollow.cs`
**Purpose**: Third-person camera that follows the player/agent.

**Key Features**:
- Smooth mouse-look rotation (horizontal & vertical)
- Configurable offset and rotation speed
- Clamps vertical rotation
- Loads settings from CharacterConfig

**Usage**: Attach to Main Camera. Assign player Transform in inspector.

---

## Demo & Inference Mode

### `InferenceVisualEnhancer.cs`
**Purpose**: Enhances scene visuals during demo/inference mode.

**Key Features**:
- Replaces agent capsule with Faith character model
- Applies Mirror's Edge-style platform materials
- Generates city skyline backdrop
- Creates translucent finish wall at target
- Disables extra training areas for single-agent view
- Only activates when `demo_mode.env` exists

**Usage**: Attach to GameObject in scene. Automatically detects demo mode via environment file.

### `DemoModeRunCompleteMenu.cs`
**Purpose**: Shows statistics menu when episode completes in demo mode.

**Key Features**:
- Displays success/failure, time, distance, actions taken
- Pauses inference loop during menu display
- Auto-resumes after countdown
- Plays audio based on result and style (uses roll percentage threshold)
- Background image fade effect

**Usage**: Singleton, auto-creates. Only activates in demo mode.

### `DemoModeScreenFlash.cs`
**Purpose**: Provides screen flash effects (red for failure, green for success).

**Key Features**:
- Red flash when agent falls
- Green flash when agent reaches target
- Configurable flash duration and opacity
- Only activates in demo mode

**Usage**: Singleton, auto-creates. Called by ParkourAgent on episode end.

### `DemoModeStaminaBar.cs`
**Purpose**: Displays stamina bar UI in demo mode.

**Key Features**:
- Shows current stamina as percentage
- Color changes based on stamina level (green → yellow → red)
- Updates 10 times per second
- Uses reflection to access agent's private stamina field

**Usage**: Singleton, auto-creates. Only visible in demo mode.

---

## Setup & Utilities

### `SetupDemo.cs`
**Purpose**: Comprehensive demo scene setup with platforms, character, and city skyline.

**Key Features**:
- Creates 36 platforms with Mirror's Edge materials
- Loads Faith character model (GLB)
- Generates atmospheric city skyline
- Sets up camera and character controller
- Weighted color selection for authentic Mirror's Edge aesthetic

**Usage**: Attach to GameObject in scene. Runs on Start(). Drag Faith GLB to inspector.

### `QuickCharacterSetup.cs`
**Purpose**: Quick test setup with basic character and platforms.

**Key Features**:
- Creates 10 simple platforms
- Generates capsule-based character (body + head)
- Sets up camera follow
- Fast iteration for testing

**Usage**: Attach to GameObject in scene. Runs on Start().

### `SimpleMaggieSetup.cs`
**Purpose**: Minimal setup script with instructions for manual character setup.

**Key Features**:
- Creates platforms only
- Logs step-by-step instructions for manual character setup
- Useful for GLB import workflow

**Usage**: Attach to GameObject in scene. Follow console instructions.

### `GameManager.cs`
**Purpose**: High-level game state management.

**Key Features**:
- Player spawning and reset
- Level generator integration
- Auto-creates ControlModeManager if missing
- Handles player fall reset

**Usage**: Attach to GameObject in scene. Optional - most functionality is self-contained in other scripts.

### `AnimusWallSetup.cs`
**Purpose**: Creates Animus-style transparent wall material (legacy).

**Key Features**:
- Configurable red glow color
- Transparency and emission settings
- Works with URP and Standard render pipelines
- Context menu function for manual setup

**Usage**: Attach to GameObject to apply Animus material. Runs automatically on Awake/Start. **Note**: Mostly replaced by InferenceVisualEnhancer's finish wall.

### `TimeScaleDiagnostic.cs`
**Purpose**: Diagnostic tool to check Unity's Time.timeScale at runtime.

**Key Features**:
- Logs time scale on Start() and every 60 frames
- Generates unique debug ID for log filtering
- Helps diagnose time scale issues during inference

**Usage**: Attach to any GameObject for debugging. Check console for time scale values.

---

## Script Dependencies

### Required Components
- **ParkourAgent**: CharacterController, DecisionRequester (auto-added by ML-Agents)
- **PlayerController**: CharacterController
- **AgentAnimationSync**: Animator, ParkourAgent or PlayerController
- **CameraFollow**: Transform (player reference)
- **TrainingArea**: Transform (spawn point and target)

### Singleton Scripts (Auto-Initialize)
- CharacterConfigManager
- ControlModeManager
- DemoModeRunCompleteMenu
- DemoModeScreenFlash
- DemoModeStaminaBar
- InferenceVisualEnhancer (not a singleton but auto-activates in demo mode)

### Configuration Files
- `control_config.json` - Control mode setting (Player/RLAgent/Heuristic)
- `demo_mode.env` - Enables demo mode features (visual enhancements, UI, audio)
- `CharacterConfig` ScriptableObject - All character parameters

---

## Common Usage Patterns

### Setting Up a Training Scene
1. Create TrainingArea GameObject
2. Add child objects: AgentSpawnPoint, Target
3. Create agent GameObject with ParkourAgent + CharacterController
4. Assign references in TrainingArea inspector
5. Configure platform generation settings
6. Create CharacterConfig asset and assign to CharacterConfigManager

### Setting Up Player Control
1. Create player GameObject with PlayerController + CharacterController
2. Create/assign CharacterConfig
3. Set up camera with CameraFollow script
4. Create `control_config.json` with `"controlMode": "Player"`

### Enabling Demo Mode
1. Create `src/demo_mode.env` file with `MLAGENTS_DEMO_MODE=true`
2. Add InferenceVisualEnhancer to scene
3. Assign Faith model prefab and animator controller
4. Run inference with `python run_inference.py`

### Switching Between Modes
- **Editor**: Press F1 (Player), F2 (RL Agent), F3 (Heuristic)
- **Config File**: Edit `control_config.json` and restart
- **Code**: `ControlModeManager.Instance.SetControlMode(mode)`

---

## Tips & Best Practices

1. **Always use CharacterConfig**: Avoid hardcoding values. Use `CharacterConfigManager.Config` for all parameters.

2. **Training vs Inference**: Keep training and demo mode separate. Use TrainingArea for training (multi-agent), InferenceVisualEnhancer for demo (single-agent).

3. **Animation Sync**: AgentAnimationSync works automatically with both RL agent and player. Just attach it to the character with an Animator.

4. **Platform Generation**: Use TrainingArea's randomization for training (better generalization), disable it for evaluation (consistent testing).

5. **Debug Tools**: Use ControlModeDebugger and TimeScaleDiagnostic when troubleshooting mode switching or time scale issues.

6. **Stamina System**: Stamina costs are balanced for gameplay. Adjust in CharacterConfig if agent struggles with stamina management.


