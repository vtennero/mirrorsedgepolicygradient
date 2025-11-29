# ML-Agents Training Log

## Experiment Tracking for Parkour Agent

---

## Run: test_v4 - FAILED
**Date:** 2024-11-27
**Duration:** ~8 minutes (500k steps)
**Agents:** 1

### Configuration:
**Actions:**
- 0: Do nothing
- 1: Jump
- 2: Move forward

**Rewards:**
- Progress: +0.1 per unit forward (Z-axis) ❌ **BUG: Wrong axis!**
- Time penalty: -0.001 per step
- Target reach: +10.0
- Fall: -1.0

**Hyperparameters:**
- Learning rate: 3e-4
- Batch size: 1024
- Hidden units: 256
- Max steps: 500k

**Environment:**
- Platform spacing: 15 units
- Platform count: 8
- Target distance: 105 units
- Episode timeout: 30s

### Results:
- **Final Reward:** -2.497 ❌
- **Behavior:** Agent learned to do nothing (safest strategy)
- **Issue:** Progress reward tracking wrong axis (Z instead of X)

---

## Run: test_v5 - PARTIAL SUCCESS
**Date:** 2024-11-27
**Duration:** ~9 minutes (500k steps)
**Agents:** 9

### Configuration:
**Actions:**
- 0: Do nothing
- 1: Jump
- 2: Move forward

**Rewards:**
- Progress: +0.1 per unit forward (X-axis) ✅ **FIXED**
- Time penalty: -0.001 per step
- Target reach: +10.0
- Fall: -1.0

**Hyperparameters:**
- Learning rate: 3e-4
- Batch size: 1024
- Hidden units: 256
- Max steps: 500k

**Environment:**
- Platform spacing: 15 units
- Platform count: 8
- Target distance: 105 units
- Episode timeout: 30s

### Results:
- **Training Reward:** +5.976 ✅
- **Inference Reward:** +0.3 ❌
- **Progress:** Agent reaches ~60 units (4 platforms) during training
- **Issue:** Poor generalization - inference performance 20x worse
- **Conclusion:** Needs more training steps

---

## Run: test_v5_demo - INFERENCE TEST
**Date:** 2024-11-27
**Duration:** ~20 minutes (1.3M inference steps)
**Agents:** 9

### Configuration:
**Using model from:** test_v5 (trained 500k steps)

**Actions:**
- 0: Do nothing
- 1: Jump
- 2: Move forward

**Rewards (for measurement only, not training):**
- Progress: +0.1 per unit forward (X-axis)
- Time penalty: -0.001 per step
- Target reach: +10.0
- Fall: -1.0

**Environment:**
- Platform spacing: 15 units
- Platform count: 8
- Target distance: 105 units
- Episode timeout: 30s

### Results:
- **Average Reward:** +0.1 to +0.5 ❌
- **Behavior:** Very poor - barely progresses beyond spawn
- **Progress:** ~1-5 units (less than 1 platform)
- **Issue:** Model trained for 500k steps doesn't generalize well
- **Conclusion:** Confirms hypothesis - need significantly more training steps (2M+)
- **Notes:** Model performs 20x worse in pure inference vs training mode

---

## Run: test_v6 - COMPLETE
**Date:** 2024-11-27
**Duration:** 16.5 minutes (2M steps)
**Agents:** 9

### Configuration:
**Actions:**
- 0: Do nothing
- 1: Jump
- 2: Move forward

**Rewards:**
- Progress: +0.1 per unit forward (X-axis)
- Grounded: +0.001 per step
- Time penalty: -0.001 per step
- Target reach: +10.0
- Fall: -1.0

**Hyperparameters:**
- Learning rate: 3e-4
- Batch size: 1024
- Hidden units: 256
- Max steps: 2M

**Environment:**
- Platform spacing: 15 units
- Platform count: 8
- Target distance: 105 units
- Episode timeout: 30s

### Results:
- **Final Reward:** +8.478
- **Peak Reward:** +9.687 (at 1.82M steps)
- **Progress:** ~85 units (5-6 platforms)
- **Std Dev:** 2.3-3.4 (stable performance)
- **vs test_v5:** +42% reward improvement (+8.478 vs +5.976)
- **Inference:** Not yet tested

---

## Run: test_v9 - FAILED (Insufficient Observations)
**Date:** 2024-11-28
**Duration:** 55.5 minutes (2M steps)
**Agents:** 1

### Configuration:
**Actions:**
- 0: Do nothing
- 1: Jump
- 2: Move forward

**Rewards:**
- Progress: +0.1 per unit forward (X-axis)
- Grounded: +0.001 per step
- Time penalty: -0.001 per step
- Target reach: +10.0
- Fall: -1.0

**Hyperparameters:**
- Learning rate: 3e-4
- Batch size: 1024
- Hidden units: 256
- Max steps: 2M

**Environment:**
- **Platform gap: RANDOMIZED 2-8 units** ⚠️
- **Platform width: RANDOMIZED 10-14 units** ⚠️
- Platform count: 8
- Episode timeout: 30s

**Observations (8 total):**
- Target relative position (3)
- Velocity (3)
- Grounded state (1)
- Forward obstacle raycast (1)

### Results:
- **Final Reward:** +3.426 ❌
- **vs test_v7 (fixed):** +8.64 → +3.43 = **60% performance DROP**
- **Issue:** Agent BLIND to platform geometry
- **Root Cause:** Observations insufficient for random environments
  - With fixed platforms: Agent memorizes "run 50 steps, jump, repeat"
  - With random platforms: Agent needs to SEE gaps/widths but can't

### Conclusion:
**Randomization requires perception.** Agent needs platform detection sensors (raycasts) to observe:
- Gap distances ahead
- Platform edge locations
- Next platform width

**Fix for test_v10:** Add 5 downward raycasts at different forward distances (2m, 4m, 6m, 8m, 10m) to detect gaps and platform edges. Increases observations from 8 → 13.

---

## Template for Future Runs:

```markdown
## Run: [run_id] - [STATUS]
**Date:** YYYY-MM-DD
**Duration:** X minutes (Y steps)
**Agents:** N

### Configuration:
**Actions:**
- 0: Action description
- 1: Action description
- ...

**Rewards:**
- Reward type: value/formula
- ...

**Hyperparameters:**
- Learning rate: X
- Batch size: X
- Hidden units: X
- Max steps: X

**Environment:**
- Key environment parameters
- ...

### Results:
- **Final Reward:** X
- **Behavior:** Description
- **Issues:** Problems found
- **Conclusions:** Next steps
```

---

## Quick Reference: Current Best

**Best Training:** test_v6 (+8.478 at 2M steps)
**Best Inference:** TBD (test_v6 not yet tested)

---

## Lessons Learned:

1. ✅ Always verify reward signals track correct axis
2. ✅ More agents = faster training (1 agent vs 9 agents = 9x speedup)
3. ✅ 500k steps insufficient - 2M steps shows 42% improvement
4. ✅ Grounded reward (+0.001/step) improves stability
5. ⏳ Need to validate inference performance, not just training
6. ✅ **Randomization requires perception** - Can't generalize without observations
   - Fixed environments: Agent memorizes sequences
   - Random environments: Agent needs sensors to perceive geometry
   - test_v9: 60% performance drop when randomized without proper observations

---

## Next Experiments to Try:

- [ ] Increase platform spacing difficulty gradually (curriculum learning)
- [ ] Tune grounded reward (0.001 vs 0.01 vs 0.005)
- [ ] Try larger networks (512 hidden units)
- [ ] Reduce time penalty to encourage exploration
- [ ] Add height variation to platforms

