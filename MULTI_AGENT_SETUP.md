# Multi-Agent Training Setup Guide

## Why Multiple Training Areas?

With 18 training areas running in parallel:
- **18x more experience per second**
- **Much faster convergence**
- **Same policy learning from all agents**
- **Standard practice in ML-Agents**

## Quick Setup (Manual)

### Step 1: Duplicate TrainingArea
1. Open `TrainingScene` in Unity
2. Select `TrainingArea` in Hierarchy
3. Press **Ctrl+D** 17 times (for 18 total)

### Step 2: Space Them Out
Arrange in a grid to avoid overlap. For example:

```
Grid Layout (6x3):
Area_0   Area_1   Area_2   Area_3   Area_4   Area_5
Area_6   Area_7   Area_8   Area_9   Area_10  Area_11
Area_12  Area_13  Area_14  Area_15  Area_16  Area_17
```

**Suggested spacing:**
- Each TrainingArea is ~120 units wide (8 platforms × 15 spacing)
- Add 20 unit buffer between areas
- Total spacing: 140 units apart in X-axis
- Use 30 units apart in Z-axis for rows

### Step 3: Use This Spacing Script (Optional)

Create a new C# script in Unity called `ArrangeTrainingAreas.cs`:

```csharp
using UnityEngine;

[ExecuteInEditMode]
public class ArrangeTrainingAreas : MonoBehaviour
{
    [Header("Auto-arrange Settings")]
    [SerializeField] private int columns = 6;
    [SerializeField] private float spacingX = 140f;
    [SerializeField] private float spacingZ = 30f;
    
    [ContextMenu("Arrange Training Areas")]
    public void ArrangeAreas()
    {
        TrainingArea[] areas = FindObjectsOfType<TrainingArea>();
        
        for (int i = 0; i < areas.Length; i++)
        {
            int row = i / columns;
            int col = i % columns;
            
            Vector3 position = new Vector3(
                col * spacingX,
                0f,
                row * spacingZ
            );
            
            areas[i].transform.position = position;
            areas[i].gameObject.name = $"TrainingArea_{i}";
        }
        
        Debug.Log($"Arranged {areas.Length} training areas in a {columns}-column grid");
    }
}
```

**To use:**
1. Attach to any GameObject in scene
2. Right-click the script in Inspector → "Arrange Training Areas"
3. All areas will be positioned automatically!

### Step 4: Verify Setup
After duplicating and spacing:
1. Make sure no areas overlap (check Scene view)
2. Each area should have its own Agent, SpawnPoint, and Target assigned
3. All agents should have the same Behavior Name: **"ParkourRunner"**

## Performance Considerations

### Unity Performance
- **18 agents with physics** = more GPU/CPU load
- If Unity lags, reduce to 12 or 9 areas
- Consider `--time-scale=20` instead of 100 initially

### Training Speed Comparison
| Setup | Experience/Min | Time to 500k steps |
|-------|----------------|-------------------|
| 1 agent | ~1,000 steps | ~8 hours |
| 9 agents | ~9,000 steps | ~1 hour |
| 18 agents | ~18,000 steps | ~30 minutes |

## Common Issues

### Issue: "Agents not learning from each other"
✅ **This is correct!** All agents share ONE policy/brain. They're all contributing to the same learning process.

### Issue: "Some agents perform differently"
✅ **This is normal!** Random initialization means some will succeed/fail differently. They all help the shared policy learn.

### Issue: "Unity is lagging"
**Solutions:**
- Reduce number of areas
- Lower time-scale from 100 to 20
- Disable shadows: Edit → Project Settings → Quality → Shadows = "Disable"
- Use --no-graphics (but you still need Unity open)

## Updated Training Command

With 18 agents, training will be MUCH faster:

```bash
cd src
conda activate mlagents

# Faster training with multiple agents
mlagents-learn parkour_config.yaml --run-id=test_v7_18agents --force

# Then press Play in Unity
```

## Expected Results

With 18 agents and balanced config:
- **Previous (1 agent):** 1 hour for minimal progress
- **New (18 agents):** 15-30 minutes for significant learning
- **Target:** Should see positive rewards within 100k-200k steps

## Monitoring Multiple Agents

In TensorBoard, the metrics are **averaged across all agents**:
- `Episode/TotalReward` = mean reward across all 18 agents
- `Episode/MaxDistance` = mean max distance
- Individual agent variations are smoothed out

This is what you want! The aggregate statistics show overall policy performance.

