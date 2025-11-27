# How to Add ParkourAgent Component - Step by Step

## Where to Add It

The `ParkourAgent` component needs to be added to the **same GameObject** that has:
- `PlayerController` component
- `CharacterController` component

This is usually the GameObject named "Player" in your Hierarchy.

## Step-by-Step Instructions

### Step 1: Find Your Player GameObject
1. Look at the **Hierarchy panel** (top-left of Unity)
2. Find the GameObject that has `PlayerController` on it
   - It might be named "Player", "Character", "Maggie", or something similar
   - If you're not sure, click different GameObjects and check the Inspector to see if they have `PlayerController`

### Step 2: Select the Player GameObject
1. **Click on the Player GameObject** in the Hierarchy
2. The **Inspector panel** (right side) should now show the Player's components

### Step 3: Add the Component
1. In the **Inspector panel**, look at the bottom of the component list
2. You should see a button that says **"Add Component"** 
   - If you don't see it, scroll down in the Inspector
   - It's usually at the very bottom, below all existing components
3. **Click "Add Component"**
4. In the search box that appears, type: **"ParkourAgent"**
5. **Click on "ParkourAgent"** in the list to add it

### Step 4: Configure ParkourAgent
After adding it, you'll see the ParkourAgent component in the Inspector with these fields:

**Required:**
- **Target** - This needs to be assigned!
  - Create an empty GameObject: Right-click in Hierarchy → Create Empty
  - Name it "Target" 
  - Position it where you want the agent to reach (e.g., at the end of your parkour course)
  - Drag this GameObject into the "Target" field in ParkourAgent

- **Controller** - Should auto-find your CharacterController, but you can drag it manually if needed

**Settings (optional):**
- Move Speed: 6 (default, should match PlayerController)
- Jump Force: 8 (default, should match PlayerController)  
- Gravity: -20 (default, should match PlayerController)

## Visual Guide

```
Hierarchy Panel          Inspector Panel
-----------------        -------------------
Main Camera              [Player GameObject Selected]
Directional Light        
Player  ← SELECT THIS    Components on Player:
  ├─ Transform              ├─ Transform
  ├─ CharacterController    ├─ CharacterController
  ├─ PlayerController       ├─ PlayerController
  └─ (need to add)         └─ [Add Component] ← CLICK HERE
                              Then search "ParkourAgent"
```

## If You Still Don't See "Add Component"

1. Make sure you selected the GameObject in **Hierarchy** (not in Project)
2. Make sure the **Inspector panel** is visible (Window → General → Inspector)
3. Scroll down in the Inspector - the button might be below the visible area
4. Try right-clicking on the component list area in Inspector - sometimes there's a context menu

## After Adding

1. Check the Console - the warning about "ParkourAgent not found" should disappear
2. Assign the Target field (create a Target GameObject and drag it in)
3. The ControlModeManager should now find it automatically

## Quick Test

After adding ParkourAgent:
1. Press Play
2. Check Console - should see: `ParkourAgent enabled: True (mode: RLAgent)`
3. Press **F2** to manually switch to RL Agent mode if needed

