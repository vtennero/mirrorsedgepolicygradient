# Jump Feasibility Analysis

## Current Setup

### Physics Parameters
- **Jump Force**: 16 units/s (upward velocity)
- **Gravity**: -20 units/s²
- **Move Speed (Jog)**: 6 units/s
- **Move Speed (Sprint)**: 12 units/s

### Current Jump Capabilities
- **Maximum Jump Height**: h = v² / (2g) = 16² / (2 × 20) = **6.4 units**
- **Time to Peak**: t = v / g = 16 / 20 = **0.8 seconds**
- **Total Air Time**: 2 × 0.8 = **1.6 seconds**
- **Max Horizontal Distance (Jog)**: 6 × 1.6 = **9.6 units**
- **Max Horizontal Distance (Sprint)**: 12 × 1.6 = **19.2 units**

### Platform Requirements
- **Gap Range**: 2-8 units (horizontal)
- **Height Change Range**: -1.5 to +2.5 units (vertical)
- **Absolute Height Range**: -0.5 to 5.0 units

## 50% Jump Reduction Analysis

### New Parameters (50% reduction)
- **Jump Force**: 8 units/s (50% of 16)
- **Gravity**: -20 units/s² (unchanged)

### New Jump Capabilities
- **Maximum Jump Height**: h = 8² / (2 × 20) = 64 / 40 = **1.6 units**
- **Time to Peak**: t = 8 / 20 = **0.4 seconds**
- **Total Air Time**: 2 × 0.4 = **0.8 seconds**
- **Max Horizontal Distance (Jog)**: 6 × 0.8 = **4.8 units**
- **Max Horizontal Distance (Sprint)**: 12 × 0.8 = **9.6 units**

### Feasibility Check

#### ❌ PROBLEM: Cannot reach +2.5 unit platforms
- **Required jump height**: 2.5 units (max height change)
- **Available jump height**: 1.6 units
- **Deficit**: 0.9 units (56% short)
- **Result**: Agent cannot reach platforms that are 2.5 units higher

#### ❌ PROBLEM: Reduced horizontal range
- **Max gap with sprint**: 9.6 units (was 19.2)
- **Gap range**: 2-8 units
- **Result**: Some 8-unit gaps might be borderline, but should work

#### ✅ OK: Downward jumps
- **Max drop**: -1.5 units (no jump needed, just fall)
- **Result**: Still feasible

## Recommendation: 25-30% Reduction Instead

### Option 1: 25% Reduction (Jump Force = 12)
- **Jump Height**: 12² / (2 × 20) = 144 / 40 = **3.6 units**
- **Safety margin**: 3.6 / 2.5 = **1.44x** (44% safety margin)
- **Max Horizontal (Sprint)**: 12 × (2 × 12/20) = 12 × 1.2 = **14.4 units**
- **Verdict**: ✅ Feasible, safe margin

### Option 2: 30% Reduction (Jump Force = 11.2)
- **Jump Height**: 11.2² / (2 × 20) = 125.44 / 40 = **3.14 units**
- **Safety margin**: 3.14 / 2.5 = **1.26x** (26% safety margin)
- **Max Horizontal (Sprint)**: 12 × (2 × 11.2/20) = 12 × 1.12 = **13.44 units**
- **Verdict**: ✅ Feasible, tighter margin

### Option 3: Keep Jump, Fix Target Detection
- **Change target detection from 3D distance to X-axis only**
- **Current**: `Vector3.Distance()` (includes Y and Z)
- **Proposed**: `Mathf.Abs(transform.position.x - target.position.x)`
- **Verdict**: ✅ Better solution - fixes the actual problem

## Target Detection Issue

### Current Implementation
```csharp
float distanceToTarget = Vector3.Distance(transform.position, target.position);
if (distanceToTarget < config.targetReachDistance) // 2.0 units
```

### Problem
- Uses 3D distance (X, Y, Z)
- If agent passes target at Y=+3 (high jump), distance = √(0² + 3² + 0²) = 3.0 units
- Agent is past target on X-axis but 3D distance > 2.0, so doesn't trigger
- Agent continues to last platform and falls

### Solution: X-Axis Only
```csharp
float distanceToTargetX = Mathf.Abs(transform.position.x - target.position.x);
if (distanceToTargetX < config.targetReachDistance) // 2.0 units
```

### Benefits
- ✅ Agent triggers success when past target on X-axis (regardless of Y)
- ✅ Matches movement direction (agent moves along X-axis)
- ✅ More forgiving (doesn't penalize high jumps)
- ✅ No need to reduce jump strength

## Final Recommendation

**Option A (Recommended)**: Fix target detection only
- Change to X-axis distance
- Keep jump force at 16 (maintains game feasibility)
- Simple fix, addresses root cause

**Option B**: Fix target detection + reduce jump 25%
- Change to X-axis distance
- Reduce jump force to 12 (25% reduction)
- Lower jumps = less likely to pass target high
- Still maintains feasibility

**Option C (Not Recommended)**: 50% jump reduction
- ❌ Breaks game feasibility
- ❌ Cannot reach +2.5 unit platforms
- ❌ Too restrictive

