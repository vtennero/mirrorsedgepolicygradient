# Architecture Documentation - Mirror's Edge Policy Gradient Project

**Last Updated:** Current State Snapshot  
**Purpose:** Complete reference documenting the current state of the codebase

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [File Structure & Script Inventory](#file-structure--script-inventory)
3. [Unity Scene Architecture](#unity-scene-architecture)
4. [Component Dependencies](#component-dependencies)
5. [Configuration System](#configuration-system)
6. [Control Flow & Execution Order](#control-flow--execution-order)
7. [Known Issues](#known-issues)

---

## Project Overview

**Project Type:** Unity 3D Parkour Environment  
**Unity Version:** 2022.3 LTS

**Key Systems:**
- Character movement (player-controlled and agent-controlled)
- Procedural level generation
- Control mode switching (Player/RLAgent/Heuristic)
- Centralized configuration system
- Camera follow system

---

## File Structure & Script Inventory

### Core Scripts (`src/Assets/Scripts/`)

#### **Character Movement & Control**

**`PlayerController.cs`** (370 lines)
- **Purpose:** Human player input handling and movement
- **Dependencies:** `CharacterController`, `CharacterConfigManager`, `ControlModeManager`, `CameraFollow`
- **Key Features:**
  - WASD/Arrow key movement
  - Mouse look (third-person camera sync)
  - Sprint with progressive acceleration
  - Jump with forward momentum boost
  - Animation state machine integration
- **Config Values Used:**
  - `moveSpeed`, `sprintSpeed`, `sprintAccelTime`, `jumpForce`, `jumpForwardBoost`
  - `gravity`, `groundCheckDistance`, `mouseSensitivity`, `verticalLookLimit`
  - `sprintDecelerationRate`, `jumpMomentumDecayRate`, `movementThreshold`
  - `sprintAnimationThreshold`, `baseAnimationSpeed`, `maxSprintAnimationSpeed`
- **Control Mode:** Only active when `ControlModeManager.CurrentMode == Player`

**`ParkourAgent.cs`** (204 lines)
- **Purpose:** Agent implementation inheriting from Unity.MLAgents.Agent
- **Inherits:** `Unity.MLAgents.Agent`
- **Dependencies:** `CharacterController`, `CharacterConfigManager`, `ControlModeManager`
- **Action Space:**
  - Type: Discrete, 1 branch with 3 actions
  - Actions: 0=nothing, 1=jump (if grounded), 2=run forward
- **Observation Space:**
  - Total: 8 floats
  - Target position relative to agent (3 floats)
  - Agent velocity (3 floats)
  - Grounded state (1 float: 1.0 if grounded, 0.0 if not)
  - Obstacle distance normalized (1 float: distance / raycastDistance)
- **Reward Calculation:**
  - Forward progress: `progressDelta * progressRewardMultiplier`
  - Time penalty: `timePenalty` per fixed update
  - Target reached: `targetReachReward` if distance < `targetReachDistance`
  - Fall/timeout: `fallPenalty` if y < `fallThreshold` or timeout > `episodeTimeout`
- **Episode Management:**
  - Begin: Resets position, velocity, timer
  - End: Triggered by reaching target, falling, or timeout
- **Config Values Used:**
  - `moveSpeed`, `jumpForce`, `gravity`, `groundedVelocityReset`
  - `progressRewardMultiplier`, `timePenalty`, `targetReachDistance`, `targetReachReward`
  - `fallThreshold`, `fallPenalty`, `episodeTimeout`, `obstacleRaycastDistance`
- **Control Mode:** Active when `ControlModeManager.CurrentMode == RLAgent || Heuristic`
- **Required Field:** `target` Transform reference

**`CharacterMovement.cs`** (106 lines)
- **Purpose:** Shared movement component (currently not actively used, may be legacy)
- **Dependencies:** `CharacterController`, `CharacterConfigManager`
- **Status:** Exists but `PlayerController` and `ParkourAgent` implement their own movement

#### **Configuration System**

**`CharacterConfig.cs`** (119 lines)
- **Purpose:** ScriptableObject containing ALL character/gameplay parameters
- **Type:** `ScriptableObject`
- **Sections:**
  - Movement Speed (moveSpeed, sprintSpeed, sprintAccelTime, etc.)
  - Jumping (jumpForce, jumpForwardBoost, jumpMomentumDecayRate)
  - Physics (gravity, groundedVelocityReset)
  - Ground Detection (groundCheckDistance)
  - Camera (rotationSpeed, limits, offsets, mouse sensitivity)
  - Animation (baseAnimationSpeed, maxSprintAnimationSpeed)
  - Agent Settings (rewards, penalties, timeouts, raycast distances)
  - Game Settings (spawn position, reset thresholds)
- **Usage:** All scripts read from this via `CharacterConfigManager.Config`
- **Override:** Individual scripts can override values in Inspector (set to -1 to use config default)

**`CharacterConfigManager.cs`** (67 lines)
- **Purpose:** Singleton manager providing access to CharacterConfig
- **Pattern:** Singleton MonoBehaviour
- **Behavior:**
  - Auto-creates default config if none assigned
  - Can assign CharacterConfig asset in Inspector
  - Provides static `Config` property for global access
- **Scene Behavior:** Auto-created by GameManager if missing

#### **Control System**

**`ControlModeManager.cs`** (244 lines)
- **Purpose:** Manages switching between Player/RL Agent/Heuristic control modes
- **Pattern:** Singleton MonoBehaviour
- **Modes:**
  - `Player`: Human control via PlayerController
  - `RLAgent`: Agent control via ParkourAgent
  - `Heuristic`: Manual control via ParkourAgent heuristic
- **Features:**
  - Loads mode from `control_config.json` (in StreamingAssets or project root)
  - Runtime switching via F1/F2/F3 keys (dev builds only)
  - Enables/disables PlayerController and ParkourAgent based on mode
  - Manages cursor lock state
- **Config File Format:** JSON with `controlMode` field ("Player", "RLAgent", or "Heuristic")
- **Auto-Creation:** GameManager creates if missing

#### **Scene Management**

**`GameManager.cs`** (90 lines)
- **Purpose:** Game initialization and player management
- **Features:**
  - Auto-creates ControlModeManager if missing
  - Spawns player from prefab if not in scene
  - Monitors player position and resets if falls below threshold
- **Dependencies:** `LevelGenerator`, `CharacterConfigManager`
- **Inspector Fields:**
  - `playerPrefab`: Optional prefab to spawn
  - `levelGenerator`: Reference to LevelGenerator component
  - `playerSpawnPosition`: Override spawn position (or use config default)

**`LevelGenerator.cs`** (94 lines)
- **Purpose:** Procedural platform generation
- **Features:**
  - Generates platforms in straight line
  - Random height variation
  - Can use prefab or create primitives
  - Gizmo preview in editor
- **Inspector Fields:**
  - `platformPrefab`: Optional prefab (creates primitives if null)
  - `platformCount`: Number of platforms (default: 8)
  - `spacing`: Distance between platforms (default: 15f)
  - `minHeight`, `maxHeight`: Height range (default: 0-4)
  - `platformSize`: Platform dimensions (default: 12x0.5x6)
  - `platformMaterial`: Optional material

**`SetupDemo.cs`** (325 lines)
- **Purpose:** One-click demo scene setup with Mirror's Edge aesthetic
- **Features:**
  - Creates 36 platforms with Mirror's Edge color palette
  - Generates city skyline background
  - Auto-sets up player, camera, components
  - Handles Faith GLB model loading
- **Behavior:** Auto-creates platforms, player, camera, and components when scene runs

#### **Camera System**

**`CameraFollow.cs`** (66 lines)
- **Purpose:** Third-person camera following player
- **Features:**
  - Mouse-controlled rotation (horizontal and vertical)
  - Configurable offset, rotation speed, limits
  - Syncs with player rotation for third-person feel
- **Dependencies:** `CharacterConfigManager`
- **Required Field:** `player` Transform reference
- **Config Values Used:**
  - `cameraRotationSpeed`, `cameraVerticalRotationLimit`, `cameraOffset`
  - `cameraLookHeightOffset`, `mouseInputMultiplier`

#### **Utility Scripts**

**`AutoSetupControlMode.cs`**
- **Purpose:** Auto-creates ControlModeManager (early execution)
- **Execution Order:** -100 (runs before other scripts)

**`ControlModeDebugger.cs`**
- **Purpose:** Debug logging for control mode state
- **Usage:** Optional, for debugging

**`QuickCharacterSetup.cs`**
- **Purpose:** Simple platform + character setup (legacy/alternative)
- **Status:** May be superseded by SetupDemo

**`SimpleMaggieSetup.cs`**
- **Purpose:** Platform-only generator
- **Status:** Alternative to SetupDemo

---

## Unity Scene Architecture

### Required GameObjects

#### **Main Camera**
- **Components:**
  - `Camera` (Unity built-in)
  - `CameraFollow` script
- **CameraFollow Fields:**
  - `player`: Player GameObject Transform reference
  - `offset`: Vector3 (uses config default if set to MinValue)
  - `rotationSpeed`: float (uses config default if -1)
  - `verticalRotationLimit`: float (uses config default if -1)

#### **Player GameObject** (name: "Player" or similar)
- **Required Components:**
  - `CharacterController` (Unity built-in)
    - Height: 1.8
    - Radius: 0.4
    - Center: (0, 0.9, 0)
  - `PlayerController` script
  - `ParkourAgent` script
  - `Animator` (optional, for animations)
- **Optional Components:**
  - `CharacterMovement` (legacy, not actively used)
- **Position:** Typically (0, 2.5, 0) or from config
- **Rotation:** (0, 90, 0) to face along platform line
- **ParkourAgent Fields:**
  - `target`: Transform reference (required, must be assigned)
  - `controller`: CharacterController reference (auto-finds if null)
  - Movement settings: float fields (use config defaults if -1)

#### **Target GameObject** (for ParkourAgent)
- **Purpose:** Finish line/goal position for agent
- **Type:** Empty GameObject
- **Usage:** Transform reference used by ParkourAgent's `target` field

#### **LevelGenerator GameObject** (optional)
- **Components:**
  - `LevelGenerator` script
- **Purpose:** Procedural platform generation at runtime

#### **SetupDemo GameObject** (optional)
- **Components:**
  - `SetupDemo` script
- **Purpose:** Auto-creates demo scene with platforms, player, camera
- **Fields:**
  - `faithPrefab`: GameObject reference (Faith GLB model)

#### **ControlModeManager GameObject** (auto-created if missing)
- **Components:**
  - `ControlModeManager` script
- **Auto-Creation:** GameManager creates if not found
- **Settings:**
  - `currentMode`: Player/RLAgent/Heuristic
  - `loadFromConfig`: Load from JSON file
  - `configFileName`: "control_config.json"
  - `playerController`: Auto-finds
  - `parkourAgent`: Auto-finds

#### **CharacterConfigManager GameObject**
- **Components:**
  - `CharacterConfigManager` script
- **Fields:**
  - `config`: CharacterConfig ScriptableObject reference (creates default if null)

#### **GameManager GameObject** (optional)
- **Components:**
  - `GameManager` script
- **Fields:**
  - `playerPrefab`: GameObject reference (optional)
  - `levelGenerator`: LevelGenerator component reference
  - `playerSpawnPosition`: Vector3 (uses config default if set to MinValue)

### Scene Hierarchy Example

```
SampleScene
├── Main Camera (Camera + CameraFollow)
├── Directional Light
├── Global Volume
├── Player (CharacterController + PlayerController + ParkourAgent + Animator)
├── Target (Empty GameObject at end of course)
├── ControlModeManager (ControlModeManager)
├── CharacterConfigManager (CharacterConfigManager)
├── GameManager (GameManager) [optional]
├── LevelGenerator (LevelGenerator) [optional]
└── SetupDemo (SetupDemo) [optional]
```

---

## Component Dependencies

### Dependency Graph

```
CharacterConfigManager (Singleton)
    └── Provides: CharacterConfig.Config
        ├── Used by: PlayerController
        ├── Used by: ParkourAgent
        ├── Used by: CharacterMovement
        ├── Used by: CameraFollow
        └── Used by: GameManager

ControlModeManager (Singleton)
    ├── Controls: PlayerController.enabled
    ├── Controls: ParkourAgent.enabled
    └── Reads: control_config.json

PlayerController
    ├── Requires: CharacterController
    ├── Uses: CharacterConfigManager.Config
    ├── Checks: ControlModeManager.CurrentMode
    └── Syncs with: CameraFollow.HorizontalRotation

ParkourAgent
    ├── Requires: CharacterController
    ├── Requires: Transform target (MUST BE ASSIGNED)
    ├── Uses: CharacterConfigManager.Config
    └── Checks: ControlModeManager.CurrentMode

CameraFollow
    ├── Requires: Transform player
    └── Uses: CharacterConfigManager.Config

GameManager
    ├── Creates: ControlModeManager (if missing)
    ├── Uses: CharacterConfigManager.Config
    └── Optional: LevelGenerator reference
```

### Execution Order

1. **Awake() Phase:**
   - `AutoSetupControlMode` (order: -100) - Creates ControlModeManager
   - `CharacterConfigManager.Awake()` - Initializes singleton
   - `ControlModeManager.Awake()` - Initializes singleton, finds components
   - `GameManager.Awake()` - Initializes spawn position, creates ControlModeManager if needed
   - `PlayerController.Awake()` - Initializes components, loads config values
   - `ParkourAgent.Initialize()` - Finds CharacterController, validates target
   - `CameraFollow.Awake()` - Loads config values

2. **Start() Phase:**
   - `ControlModeManager.Start()` - Loads config, applies control mode
   - `GameManager.Start()` - Spawns player if needed
   - `LevelGenerator.Start()` - Generates platforms
   - `SetupDemo.Start()` - Creates demo scene

3. **Update/FixedUpdate Phase:**
   - `PlayerController.Update()` - Only if mode == Player
   - `ParkourAgent.FixedUpdate()` - Only if mode == RLAgent || Heuristic
   - `CameraFollow.LateUpdate()` - Always active
   - `GameManager.Update()` - Monitors player position

---

## Configuration System

### CharacterConfig Asset

**Type:** ScriptableObject (`CharacterConfig.cs`)

**All Configurable Values:**

#### Movement Speed
- `moveSpeed` (6f): Normal walking/running speed
- `sprintSpeed` (12f): Maximum sprint speed
- `sprintAccelTime` (3f): Time to reach max sprint
- `sprintDecelerationRate` (2f): Sprint decay rate
- `movementThreshold` (0.1f): Minimum input to be "moving"
- `sprintAnimationThreshold` (0.5f): Sprint animation activation threshold

#### Jumping
- `jumpForce` (16f): Vertical jump force
- `jumpForwardBoost` (6f): Horizontal boost when jumping
- `jumpMomentumDecayRate` (2f): Air resistance multiplier

#### Physics
- `gravity` (-20f): Gravity force per second
- `groundedVelocityReset` (-2f): Velocity when grounded

#### Ground Detection
- `groundCheckDistance` (0.1f): Ground check distance

#### Camera
- `cameraRotationSpeed` (2f): Camera rotation multiplier
- `cameraVerticalRotationLimit` (80f): Up/down look limit
- `cameraOffset` (0, 2, -5): Camera position relative to player
- `cameraLookHeightOffset` (1.5f): Look-at target height
- `mouseSensitivity` (2f): Mouse sensitivity
- `mouseInputMultiplier` (0.1f): Mouse input scaling
- `playerVerticalLookLimit` (80f): Player look limit (first-person)

#### Animation
- `baseAnimationSpeed` (1f): Normal animation speed
- `maxSprintAnimationSpeed` (1.5f): Max sprint animation speed

#### Agent Settings
- `progressRewardMultiplier` (0.1f): Forward progress reward
- `timePenalty` (-0.001f): Time penalty per fixed update
- `targetReachDistance` (2f): Distance to consider "reached"
- `targetReachReward` (10f): Reward for reaching target
- `fallThreshold` (-5f): Y position for fall detection
- `fallPenalty` (-1f): Penalty for falling
- `episodeTimeout` (30f): Max episode time in seconds
- `obstacleRaycastDistance` (10f): Obstacle detection raycast distance

#### Game Settings
- `playerResetThreshold` (-10f): Y position for player reset
- `defaultSpawnPosition` (0, 1, 0): Default spawn position

### Control Config File

**Location:** `StreamingAssets/control_config.json` or project root

**Format:**
```json
{
  "controlMode": "RLAgent"
}
```

**Valid Values:** `"Player"`, `"RLAgent"`, `"Heuristic"`

**Runtime Switching:**
- F1: Player mode
- F2: RL Agent mode
- F3: Heuristic mode
- (Only in Editor/Development builds)

---

## Control Flow & Execution Order

### Scene Initialization Flow

```
1. AutoSetupControlMode.Awake() [Order: -100]
   └── Creates ControlModeManager if missing

2. CharacterConfigManager.Awake()
   └── Initializes singleton, creates default config if none assigned

3. ControlModeManager.Awake()
   ├── Sets singleton instance
   ├── Auto-finds PlayerController and ParkourAgent
   └── Warns if components not found

4. GameManager.Awake()
   ├── Loads spawn position from config
   └── Creates ControlModeManager if missing

5. PlayerController.Awake()
   ├── Gets CharacterController
   ├── Gets Animator
   └── InitializesFromConfig() - Loads all config values

6. ParkourAgent.Initialize()
   ├── Finds CharacterController
   ├── Validates target is assigned
   └── InitializesFromConfig() - Loads movement config

7. CameraFollow.Awake()
   └── InitializeFromConfig() - Loads camera config

8. ControlModeManager.Start()
   ├── Loads control mode from JSON config
   └── ApplyControlModeDelayed() coroutine
       └── SetControlMode() - Enables/disables components

9. GameManager.Start()
   └── InitializeGame()
       ├── Checks for existing player
       └── Spawns player from prefab if needed

10. LevelGenerator.Start() / SetupDemo.Start()
    └── Generates platforms
```

### Runtime Control Flow

**Player Mode:**
```
Update() Loop:
  PlayerController.Update()
    ├── HandleInput() - WASD, mouse, jump, sprint
    ├── HandleGroundDetection()
    ├── HandleMovement() - Apply movement, sprint, jump momentum
    ├── UpdateAnimations() - Animation state machine
    └── HandleJumping() - Gravity, jump logic

  CameraFollow.LateUpdate()
    └── Mouse rotation, camera positioning

  GameManager.Update()
    └── Monitor player position, reset if fallen
```

**RLAgent/Heuristic Mode:**
```
FixedUpdate() Loop:
  ParkourAgent.FixedUpdate()
    └── Apply gravity, vertical movement

  ParkourAgent (Agent base class):
    ├── CollectObservations() - Gather state
    ├── OnActionReceived() - Process actions, calculate rewards
    └── Heuristic() - Manual control input

  CameraFollow.LateUpdate()
    └── Follow agent (same as player)

  GameManager.Update()
    └── Monitor position (same as player)
```

---

## Known Issues

1. **Deprecated API Warnings:**
   - `FindObjectOfType` used in multiple scripts
   - Files: GameManager.cs, ControlModeManager.cs, SetupDemo.cs

2. **Unused Component:**
   - `CharacterMovement.cs` exists but not actively used
   - PlayerController and ParkourAgent implement their own movement

3. **Target Position:**
   - ParkourAgent requires target Transform to be assigned
   - No automatic generation of target position from level

---

## File Locations Reference

### Scripts
- `src/Assets/Scripts/CharacterConfig.cs`
- `src/Assets/Scripts/CharacterConfigManager.cs`
- `src/Assets/Scripts/PlayerController.cs`
- `src/Assets/Scripts/ParkourAgent.cs`
- `src/Assets/Scripts/CharacterMovement.cs`
- `src/Assets/Scripts/ControlModeManager.cs`
- `src/Assets/Scripts/GameManager.cs`
- `src/Assets/Scripts/LevelGenerator.cs`
- `src/Assets/Scripts/CameraFollow.cs`
- `src/Assets/Scripts/SetupDemo.cs`

### Configuration
- `src/parkour_config.yaml` - Configuration file (PPO hyperparameters)
- `control_config.json` - Control mode config (located in StreamingAssets or project root)
- CharacterConfig asset - ScriptableObject asset in Unity Project

### Documentation
- `ARCHITECTURE.md` - This file
- `README.md` - Project overview
- `project_outline.md` - Project specifications
- `ADD_PARKOUR_AGENT_STEPS.md` - Agent setup documentation
- `RL_INTEGRATION_GUIDE.md` - Integration guide

---

## Quick Reference: Key Values

### Default Character Config Values
- Move Speed: 6 units/sec
- Sprint Speed: 12 units/sec
- Jump Force: 16
- Gravity: -20
- Target Reach Distance: 2 units
- Episode Timeout: 30 seconds
- Fall Threshold: -5 units

### Control Mode Runtime Switching
- F1: Player mode
- F2: RLAgent mode
- F3: Heuristic mode
- (Only active in Editor/Development builds)

---

**End of Architecture Documentation**

