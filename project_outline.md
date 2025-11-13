# Mirror's Edge RL Project Specifications

## Project Overview
AI agent learns parkour navigation from A→B with human audience feedback for style rewards. User selects a level and watches the agent loop through it repeatedly, learning both speed optimization and crowd-pleasing moves through reinforcement learning. Human presses a key (crowd ovation trigger) when impressed by moves, teaching the AI which stylish actions audiences love.

## Core Systems

### Unity Environment Setup
- [ ] Install Unity Hub and Unity 2022.3 LTS
- [ ] Create new 3D project
- [ ] Install ML-Agents Unity package
- [ ] Install ML-Agents Python package (`pip install mlagents`)
- [ ] Verify setup with ML-Agents 3DBall example
- [ ] Configure project settings for ML-Agents

### Level Design System
- [ ] Create basic platform prefab (cube primitive)
- [ ] Implement manual level editor (drag-drop platforms)
- [ ] Design initial straight-line parkour course
- [ ] Add start position marker
- [ ] Add target/end position marker
- [ ] Implement camera follow system for character
- [ ] Test level traversability manually

### Character Controller
- [ ] Import character model from Mixamo or Unity Asset Store
- [ ] Implement basic movement (WASD/arrow keys)
- [ ] Implement basic jump mechanics
- [ ] Test manual character control in level
- [ ] Ensure character can complete course manually

### Animation System
- [ ] Import parkour animation set from Mixamo (jump, run, salto, wall-run, etc.)
- [ ] Set up Unity Animator with animation states
- [ ] Create animation triggers for each parkour move
- [ ] Implement animation blending and transitions
- [ ] Test all animations trigger correctly from code
- [ ] Ensure smooth animation transitions

### Action Space Definition
- [ ] Define discrete action space (move forward/back/left/right, jump, salto, wall-run, etc.)
- [ ] Map each action to corresponding animation trigger
- [ ] Implement action execution system
- [ ] Test all actions work independently
- [ ] Verify actions chain together smoothly

### Core RL Integration
- [ ] Set up ML-Agents Agent script
- [ ] Define observation space (agent position, target position, obstacle positions, velocities)
- [ ] Implement CollectObservations() method
- [ ] Implement OnActionReceived() method
- [ ] Define basic reward function (reach target +10, time penalty -0.01)
- [ ] Set up episode reset system
- [ ] Test agent can be controlled by ML-Agents
- [ ] Verify training loop works

### Human Feedback System
- [ ] Implement crowd ovation key press detection
- [ ] Capture key press events during agent performance with timestamps
- [ ] Link key presses to current agent actions/moves
- [ ] Store human feedback data (timestamp, action, move type)
- [ ] Integrate crowd feedback into reward function
- [ ] Add user-configurable weight setting for objective vs subjective rewards
- [ ] Test that crowd feedback affects agent learning behavior
- [ ] Implement feedback visualization/logging

### Training System
- [ ] Implement level selection UI for user
- [ ] Configure ML-Agents training parameters
- [ ] Set up training loop for selected level (agent repeats same level)
- [ ] Set up TensorBoard logging
- [ ] Implement training session management
- [ ] Test single-agent training works on looped level
- [ ] Optimize training performance (local 3060 + cloud GPU options)
- [ ] Add training progress visualization for watching agent learn

## Advanced Features

### Procedural Level Generation
- [ ] Implement basic procedural platform placement
- [ ] Add randomization for platform heights and gaps
- [ ] Ensure generated levels are always completable
- [ ] Add difficulty scaling parameters
- [ ] Test procedural levels work with RL training

### LLM Level Generation
- [ ] Design JSON schema for level descriptions
- [ ] Implement LLM API integration (OpenAI/Anthropic)
- [ ] Create prompt templates for level generation
- [ ] Add level validation system
- [ ] Test LLM-generated levels are playable

### Performance Optimization
- [ ] Implement multiple environment instances for faster training
- [ ] Add Unity Job System integration (if needed)
- [ ] Optimize rendering for training (reduce visual quality during training)
- [ ] Add cloud training configuration

### Polish & UI
- [ ] Add main menu system
- [ ] Implement training progress display
- [ ] Add agent performance metrics display
- [ ] Create simple UI for human feedback
- [ ] Add visual feedback for impressive moves
- [ ] Implement replay system for best runs

## Technical Requirements
- Unity 2022.3 LTS (~4.5GB total install)
- Python 3.8+ with ML-Agents
- Local GPU: RTX 3060 for development/testing
- Cloud GPU options: vast.ai, runpod for intensive training
- Basic development possible on CPU-only for Unity work

## Success Criteria
- [ ] Agent successfully learns to navigate A→B course
- [ ] Agent learns to perform stylish moves based on human feedback
- [ ] Training system is stable and repeatable
- [ ] Human can observe and influence agent behavior in real-time
- [ ] System demonstrates both speed optimization and style learning

## Implementation Order
1. Unity setup and basic level
2. Character controller and manual testing
3. Animation system integration
4. Core RL implementation
5. Human feedback system
6. Training optimization
7. Advanced features (procedural/LLM generation)