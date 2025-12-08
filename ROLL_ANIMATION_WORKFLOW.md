# Forward Roll Animation Workflow Guide

**Quick Setup**: Your `roll` animation is already imported. Just configure the Animator Controller.

## Step 1: Open Animator Controller

1. **In Unity Project window** (bottom panel):
   - Navigate: `Assets` → `MaggieAnimationController.controller`
   - **Double-click** `MaggieAnimationController.controller`
2. **Animator window opens** (if not: Window → Animation → Animator)

## Step 2: Add Roll Animation Parameters

**In Animator window** (left side, top):
1. Click **Parameters** tab (next to "Layers")
2. Click **+** button (top-right of Parameters panel)
3. Select **Trigger**
4. Name it: `rollstart`
5. Repeat: Click **+** → **Trigger** → Name: `rollend`
6. Repeat: Click **+** → **Bool** → Name: `IsRolling`

You should now see 3 new parameters in the Parameters list.

## Step 3: Create Roll State

**In Animator window** (main area, center):
1. **Right-click empty space** (not on any existing state)
2. Select **Create State** → **Empty**
3. **Name it**: `roll` (double-click the state name to rename)
4. **Click the `roll` state** to select it
5. **In Inspector window** (right side):
   - Find **Motion** field (near top)
   - Click the **circle icon** (target icon) next to Motion
   - **Search box appears**: Type `roll` and find your roll animation
   - **Click your roll animation** to assign it
   - **Speed**: Leave at `1.0` (default)

2. **Create Roll Animation States**
   - Right-click in Animator window → **Create State** → **Empty**
   - Name it `RollStart` (or `RollForwardStart`)
   - Select the state, then in Inspector:
     - **Motion**: Drag your roll start animation clip
     - **Speed**: Set to `1.0` (adjust if needed)
   
   - Repeat for:
     - `RollLoop` (if you have a looping roll animation)
     - `RollEnd` (roll recovery/end animation)

3. **Set up Animation Parameters**
   - In Animator window, click **Parameters** tab
   - Add the following parameters:
     - `IsRolling` (Bool) - Controls roll state
     - `rollstart` (Trigger) - Triggers roll start animation
     - `rollend` (Trigger) - Triggers roll end animation

## Step 4: Create Animation Transitions

### Transition 1: Any State → Roll (Start)
1. **In Animator window**: Find the **orange "Any State" box** (usually top-left)
2. **Right-click "Any State"** → **Make Transition**
3. **Click the `roll` state** (arrow appears)
4. **Click the arrow line** to select the transition
5. **In Inspector window** (right side):
   - Scroll to **Conditions** section
   - Click **+** button (adds a condition)
   - **Parameter dropdown**: Select `rollstart`
   - **Has Exit Time**: ❌ **UNCHECK** this box
   - **Transition Duration**: `0.1`

### Transition 2: Roll → Ground States (End)
1. **In Animator window**: Find your `jog` or `idle` state (ground movement states)
2. **Right-click the `roll` state** → **Make Transition**
3. **Click your `jog` or `idle` state** (arrow appears)
4. **Click the arrow line** to select the transition
5. **In Inspector window** (right side):
   - **Conditions**: Click **+** button
   - **Parameter**: Select `IsRolling`
   - **Value**: ❌ **Unchecked** (false)
   - **Has Exit Time**: ✅ **CHECK** this box
   - **Exit Time**: `0.9`
   - **Transition Duration**: `0.2`

**Note**: The code triggers `rollstart` when roll begins, and `rollend` when it completes. The `IsRolling` bool controls the return transition.

## Step 5: Verify Code Integration

The code is already set up! `AgentAnimationSync.cs` will:
- Set `IsRolling` = `true` when action 4 is active and rolling
- Trigger `rollstart` when roll begins (line 83)
- Trigger `rollend` when roll completes (line 109)
- Set `IsRolling` = `false` when roll ends

**No code changes needed** - just make sure your Animator Controller has the parameters and states above.

## Step 6: Test in Unity Editor

1. **Open your training scene**
2. **Select the agent GameObject** (with ParkourAgent component)
3. **Check Animator**:
   - Verify **Animator** component is attached
   - Verify **Controller** field points to `MaggieAnimationController`
   - Verify `AgentAnimationSync` component is attached

4. **Test with Heuristic**:
   - Find **Behavior Parameters** component on agent
   - Set **Behavior Type** to **Heuristic**
   - Press **Play** in Unity
   - Press **R** key to trigger roll (action 4)
   - Watch **Animator** window (Window → Animation → Animator) to see state transitions

5. **Verify it works**:
   - `roll` state should activate when you press R
   - Roll animation should play
   - Agent should move forward
   - Stamina should decrease by 150

## Step 7: Tune Roll Duration (Optional)

The roll duration is in `ParkourAgent.cs` line ~27:
```csharp
private const float ROLL_DURATION = 0.6f; // Adjust to match your animation
```

**To match your animation:**
1. Select your `roll` animation in Project
2. Check **Length** in Inspector (Animation tab)
3. Update `ROLL_DURATION` in `ParkourAgent.cs` to match
4. Recompile

## Step 8: Update Behavior Parameters (Action Space) ⚠️ CRITICAL

**⚠️ CRITICAL**: Action space changed from 4 → 5 actions!

1. **In Project window**: Find your agent prefab (e.g., `TrainingArea.prefab` in `Assets/Prefabs/`)
2. **Double-click the prefab** to open it (or select it and click "Open Prefab" button)
3. **In Hierarchy** (left panel): Select the **agent GameObject** (child of prefab)
4. **In Inspector** (right panel): Find **Behavior Parameters** component
5. **Expand "Branch Sizes"** (click the arrow to expand)
6. **Change "Element 0"** from `4` to `5`
   - This tells ML-Agents there are now 5 discrete actions (0-4)
7. **Save**: Click the **back arrow** (top-left) to exit prefab mode, or press **Ctrl+S**

## Step 9: Retrain Model

⚠️ **BREAKING CHANGE**: All existing trained models are incompatible.

The action space changed from 4 → 5 actions, so:
1. **Backup/rename old model checkpoints** (if you want to keep them)
2. **Start fresh training** with new action space
3. Agent will learn to use rolls sparingly (high stamina cost: 150)

## Troubleshooting

### Animation doesn't play
- ✅ Check Animator Controller is assigned (should be `MaggieAnimationController`)
- ✅ Verify `roll` animation clip is assigned to `roll` state
- ✅ Check `AgentAnimationSync` component is attached to agent
- ✅ Verify parameters exist: `rollstart`, `rollend`, `IsRolling`

### Roll triggers but animation doesn't transition
- Check transition from `Any State` → `roll` uses `rollstart` trigger
- Check transition from `roll` → ground uses `IsRolling` = false
- Verify "Has Exit Time" is unchecked for start, checked for end
- Check transition durations (0.1 for start, 0.2 for end)

### Roll animation plays but agent doesn't move
- Check stamina is sufficient (roll costs 150, need at least 150)
- Verify `ROLL_DURATION` in `ParkourAgent.cs` matches animation length
- Check Console for errors

### Agent uses roll too often/rarely (after training)
- Adjust `rollStaminaCost` in CharacterConfig (higher = less frequent)
- Adjust `rollStyleBonus` (0.05-0.2, higher = more incentive)
- Adjust `styleEpisodeFrequency` (0.1-0.2, higher = more style episodes)

## Quick Reference

**Animator Parameters to Add:**
- `rollstart` (Trigger)
- `rollend` (Trigger)  
- `IsRolling` (Bool)

**States to Create:**
- `roll` (with your roll animation clip)

**Transitions:**
- `Any State` → `roll`: Condition = `rollstart` trigger, Has Exit Time = false
- `roll` → Ground: Condition = `IsRolling` = false, Has Exit Time = true, Exit Time = 0.9

**Code Already Handles:**
- ✅ Triggers `rollstart` when roll begins
- ✅ Sets `IsRolling` = true during roll
- ✅ Triggers `rollend` when roll completes
- ✅ Sets `IsRolling` = false when roll ends

---

**Status**: Code is complete. Just configure Animator Controller (Steps 1-4) and update Behavior Parameters (Step 8).

