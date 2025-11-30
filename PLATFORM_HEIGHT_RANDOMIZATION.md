# Platform Height Randomization

## Overview
Added platform height randomization to `TrainingArea.cs` to create more varied and challenging training environments for the parkour agent. This complements the existing gap and width randomization.

## Changes Made

### New Configuration Parameters

Added to `TrainingArea.cs`:

```csharp
[Tooltip("Randomize platform heights (vertical position)")]
[SerializeField] private bool randomizeHeights = true;

[Tooltip("Max height difference between consecutive platforms (units). Agent can jump ~6.4 units high.")]
[SerializeField] private Vector2 heightChangeRange = new Vector2(-1.5f, 2.5f);

[Tooltip("Absolute min/max heights to keep platforms within reasonable bounds")]
[SerializeField] private Vector2 absoluteHeightRange = new Vector2(-0.5f, 5f);
```

### Physics-Based Feasibility

The height ranges are calculated based on the agent's movement physics:

#### Agent Capabilities
- **Jump Force**: 16 units/s (upward velocity)
- **Gravity**: -20 units/s²
- **Move Speed**: 6 units/s (base movement)

#### Jump Physics Calculations
- **Maximum Jump Height**: h = v² / (2g) = 16² / (2 × 20) = **6.4 units**
- **Time to Peak**: t = v / g = 16 / 20 = 0.8 seconds
- **Total Air Time**: 2 × 0.8 = 1.6 seconds
- **Max Horizontal Distance** (at base speed): 6 × 1.6 = 9.6 units
- **Max with Sprint/Momentum**: ~19 units

#### Safe Height Ranges
- **Height Change Range**: -1.5 to +2.5 units
  - **Jumping UP**: Max +2.5 units (39% of max jump height - very safe)
  - **Dropping DOWN**: Max -1.5 units (controlled descent)
- **Absolute Height Range**: -0.5 to 5 units
  - Keeps platforms within reasonable vertical bounds
  - Prevents extreme configurations

### Implementation Details

#### Height Generation
1. First platform starts at base height (0, clamped to absolute range)
2. Each subsequent platform:
   - Randomly varies by `heightChangeRange` from previous platform
   - Gets clamped to `absoluteHeightRange` for safety
   - Only applied when both `randomizePlatforms` and `randomizeHeights` are enabled

#### Feasibility Validation
Added `IsJumpFeasible()` method that:
- Uses projectile motion physics to validate jumps
- Checks if height difference is within jump capability
- Calculates time in air using kinematic equations
- Verifies horizontal distance is reachable
- Includes safety margins (90% for vertical, 180% for horizontal)

#### Debug Features
- Editor-only feasibility warnings for potentially difficult jumps
- Logs gap distance and height difference when jumps may be challenging
- Helps with configuration tuning during development

## Benefits

### Training Improvement
1. **Better Generalization**: Agent learns to handle varied vertical terrain
2. **More Realistic**: Real parkour environments have height variations
3. **Curriculum-Ready**: Can adjust ranges for progressive difficulty
4. **Maintains Feasibility**: All jumps are physically possible

### Configuration Flexibility
- Can enable/disable height randomization independently
- Adjustable ranges for different difficulty levels
- Deterministic mode with fixed heights still available
- Random seed support for reproducible training

## Usage

### Default Configuration
Platforms will now vary in height between episodes when `randomizePlatforms = true` and `randomizeHeights = true`.

### Adjusting Difficulty
- **Easier**: Reduce `heightChangeRange` to (-1.0, 1.5)
- **Harder**: Increase to (-2.0, 3.0) - but monitor feasibility warnings!
- **Flat**: Set `randomizeHeights = false`

### Custom Heights
If you provide a `platformHeights` array in the Inspector, it will use those fixed heights instead of generating random ones.

## Testing Recommendations

1. **Monitor Training Metrics**: Watch for changes in success rate
2. **Check Feasibility Warnings**: Review editor console during play testing
3. **Adjust Ranges Gradually**: Start conservative, increase difficulty over time
4. **Validate with Heuristic**: Test manually with keyboard controls first

## Technical Notes

### Projectile Motion Equations Used
```
Vertical position: y = y₀ + v₀t + ½gt²
Height at time t: h(t) = jumpForce × t + 0.5 × gravity × t²
Time to reach height: t = (-v₀ ± √(v₀² + 2gh)) / g
Horizontal distance: x = moveSpeed × t
```

### Safety Margins
- 90% of max jump height for upward jumps
- 180% horizontal reach (accounts for sprint and momentum)
- Absolute bounds prevent extreme outliers

## Future Enhancements

Possible improvements:
1. **Dynamic Difficulty**: Adjust ranges based on agent performance
2. **Terrain Types**: Different height patterns (stairs, waves, etc.)
3. **Gap-Height Correlation**: Reduce height variation for longer gaps
4. **Advanced Physics**: Account for sprint mechanics and momentum

## Files Modified

- `src/Assets/Scripts/TrainingArea.cs`: Main implementation
- `PLATFORM_HEIGHT_RANDOMIZATION.md`: This documentation

## Related Systems

- Existing gap randomization: 2-8 units
- Platform width randomization: 10-14 units
- Agent physics in `CharacterConfig.cs`
- Episode regeneration in `ParkourAgent.cs`


