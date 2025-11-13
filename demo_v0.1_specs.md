# Demo v0.1 - Quick Parkour Prototype Specifications

## Overview
1-hour rapid prototype demonstrating basic parkour mechanics with procedural level generation. Focus on code-heavy implementation with minimal Unity UI dependency.

**Tech Stack:**
- Unity 2022.3 LTS
- Cinemachine package
- New Input System
- Mixamo character (FBX format)
- CharacterController for player physics

---

## Module 1: Project Setup (10 minutes)

### Unity Project Configuration
- [ ] Create new 3D Unity project
- [ ] Install Cinemachine package via Package Manager
- [ ] Install Input System package via Package Manager
- [ ] Configure project to use New Input System (Player Settings)

### Asset Acquisition
- [ ] Download character from Mixamo.com (Recommended: "Remy" or "Ty")
  - Export as FBX format
  - Include T-Pose animation
- [ ] Import character FBX into Unity Assets/Characters/ folder
- [ ] Create character prefab from imported model

### Basic Scene Setup
- [ ] Create empty scene
- [ ] Add directional light
- [ ] Set up basic skybox (optional)

---

## Module 2: Level Generation System (15 minutes)

### Procedural Platform Generator
**File:** `Scripts/LevelGenerator.cs`

**Requirements:**
- Generate straight line of cube platforms
- Random height variation per platform
- Configurable platform count and spacing
- Spawn platforms at runtime via code

**Algorithm:**
```
For each platform (0 to platformCount):
  X = platform index * spacing (3-5 units)
  Y = Random height (0-3 units)
  Z = 0 (straight line)
  Instantiate cube at position
```

**Configuration Parameters:**
- Platform count: 20
- Spacing: 4 units
- Height range: 0-3 units
- Platform size: 1x1x1 unit cubes

### Platform Prefab
- [ ] Create cube prefab for platforms
- [ ] Add BoxCollider component
- [ ] Material: Simple colored material (Unity default)
- [ ] Save as `Prefabs/Platform.prefab`

---

## Module 3: Player Controller (20 minutes)

### Character Setup
**File:** `Scripts/PlayerController.cs`

**Components Required:**
- CharacterController component
- Transform for movement
- Input System integration

**Movement Specifications:**
- WASD movement (forward/back/left/right)
- Space bar for jumping
- Movement speed: 6 units/second
- Jump force: 8 units
- Gravity: -20 units/second²

**Key Features:**
- Ground detection via CharacterController.isGrounded
- Smooth movement with Time.deltaTime
- Jump buffering (prevent infinite jumping)
- Simple physics simulation for jumping arc

### Input System Configuration
**File:** `InputActions/PlayerInput.inputactions`

**Action Map:**
- Movement: Vector2 (WASD/Arrow keys)
- Jump: Button (Space bar)

### Animation Integration (Basic)
- [ ] Set up Animator Controller for character
- [ ] Basic states: Idle, Running, Jumping
- [ ] Animation triggers from movement script
- [ ] Use Mixamo animations (walk, idle, jump)

---

## Module 4: Camera System (10 minutes)

### Cinemachine Follow Camera
**Configuration:**
- Virtual Camera following player
- Third-person rear view
- Smooth follow with damping
- Fixed distance and angle

**Camera Settings:**
- Follow offset: (0, 2, -5) - behind and above player
- Look at: Player transform
- Damping: Medium smoothing for natural feel
- FOV: 60 degrees

**Implementation:**
- [ ] Add Cinemachine Virtual Camera to scene
- [ ] Set Follow target to player GameObject
- [ ] Configure Body: 3rd Person Follow
- [ ] Configure Aim: Composer with player as Look At target

---

## Module 5: Integration & Testing (5 minutes)

### Scene Assembly
- [ ] Place player at start position (0, 1, 0)
- [ ] Generate level platforms via LevelGenerator script
- [ ] Position camera system
- [ ] Test player movement and jumping

### Basic Game Loop
- [ ] Player spawns at level start
- [ ] Can move and jump on procedural platforms
- [ ] Camera follows smoothly
- [ ] No win/lose conditions (just exploration)

---

## File Structure

```
Assets/
├── Scripts/
│   ├── PlayerController.cs
│   ├── LevelGenerator.cs
│   └── GameManager.cs
├── Prefabs/
│   ├── Player.prefab
│   └── Platform.prefab
├── Characters/
│   └── [Mixamo_Character].fbx
├── InputActions/
│   └── PlayerInput.inputactions
└── Scenes/
    └── Demo.unity
```

---

## Success Criteria

**Functional Requirements:**
- [ ] Player can move with WASD
- [ ] Player can jump with Space
- [ ] Camera follows player smoothly
- [ ] Procedural platforms generate in straight line
- [ ] Player can jump between platforms
- [ ] No crashes or major bugs

**Code-Heavy Implementation:**
- [ ] Level generation fully scripted
- [ ] Player movement fully scripted
- [ ] Minimal Unity UI usage (only for prefab creation)
- [ ] All gameplay logic in C# scripts

**Performance Targets:**
- [ ] Runs at 60+ FPS on RTX 3060
- [ ] Smooth player movement
- [ ] Responsive input handling

---

## Implementation Notes

**Code Priority:**
- Maximize scriptable components
- Use Unity UI only for essential prefab setup
- All gameplay parameters configurable in code
- Easy to modify via script changes

**Quick Wins:**
- Use Unity primitives (cubes) for platforms
- Simple materials and lighting
- Focus on functionality over visual polish
- Defer visual improvements to later versions

**Future Extension Points:**
- Easy to add more complex level generation
- Player controller ready for ML-Agents integration
- Modular design for adding parkour moves
- Camera system ready for different view modes
