# Training Results Report: training_20251214_173233

## Configuration
- **Style Episode Frequency:** 0.4 (40%)
- **Training Start Time:** 2025-12-14 17:32:33
- **Total Steps:** 2,000,136
- **Max Steps:** 2,000,000

## Final Performance Metrics

### Checkpoint Rewards
- **500k steps:** +39.95
- **1.0M steps:** +66.25
- **1.5M steps:** +77.75
- **2.0M steps (checkpoint):** +78.52
- **Final (2,000,136 steps):** +77.26

### Training Progression
- **500k → 1M:** +66.25 (66% improvement from 500k baseline)
- **1M → 1.5M:** +77.75 (17% improvement)
- **1.5M → 2M:** +78.52 (1% improvement)
- **Overall (500k → 2M):** +78.52 from +39.95 = +96.5% improvement

## Episode Statistics (from episode_data.json)

### Episode Length (seconds)
- **Mean:** 43.91 seconds
- **Range:** 4.54 -- 66.46 seconds
- **Note:** These are time-based lengths, not step counts

### Distance Traveled (units)
- **Mean:** 360.15 units
- **Range:** 26.09 -- 582.92 units

### Episode Count
- **Total episodes logged:** 100 (sampled every 20,000 steps)
- **All episodes:** success = true

## Action Distribution (Final Step: 2,000,000)

From action_distribution_over_time.json at final step:
- **Jump:** 4.02%
- **Jog:** 66.97%
- **Sprint:** 13.59%
- **Roll:** 10.21%
- **Idle:** 5.21%

**Total:** 100.00%

## Training Losses (Final Step: 2,000,000)

From losses_over_time.json at final step:
- **Policy Loss:** 0.0240
- **Value Loss:** 1.027

## Entropy (Final Step: 2,000,000)

From entropy_over_time.json at final step:
- **Policy Entropy:** 0.600

## Hyperparameters (from configuration.yaml)
- **Learning Rate:** 0.0003 (linear decay)
- **Beta (Entropy):** 0.1 (linear decay)
- **Epsilon (Clipping):** 0.2 (linear decay)
- **Lambda (GAE):** 0.95
- **Gamma (Discount):** 0.99
- **Batch Size:** 1024
- **Buffer Size:** 10240
- **Num Epochs:** 5
- **Time Horizon:** 128

## Network Architecture
- **Actor:** 2 hidden layers × 256 units
- **Critic:** 2 hidden layers × 128 units
- **Input Normalization:** Enabled

## Comparison with Report Values

### Discrepancies Found:

1. **Final Reward:**
   - **Report:** +89.18
   - **Actual:** +77.26 (final) or +78.52 (2M checkpoint)
   - **Difference:** ~12-14 points lower

2. **Training Progression:**
   - **Report:** +26.67 at 500k → +89.18 at 2M
   - **Actual:** +39.95 at 500k → +77.26 at 2M (or +78.52 at 2M checkpoint)

3. **Episode Statistics:**
   - **Report:** Mean length 61.07 steps, mean distance 555.91 units
   - **Actual:** Mean length 43.91 seconds, mean distance 360.15 units
   - **Note:** Report uses "steps" but data shows time-based lengths

4. **Action Distribution:**
   - **Report:** Roll 7.81%, Jog 67.61%, Sprint 14.00%, Jump 3.53%, Idle 7.04%
   - **Actual (final step):** Roll 10.21%, Jog 66.97%, Sprint 13.59%, Jump 4.02%, Idle 5.21%
   - **Difference:** Roll usage is HIGHER in actual data (10.21% vs 7.81%)

5. **Policy Loss:**
   - **Report:** 0.0233
   - **Actual:** 0.0240
   - **Difference:** Very close, within rounding

6. **Value Loss:**
   - **Report:** 0.985
   - **Actual:** 1.027
   - **Difference:** Slightly higher

7. **Entropy:**
   - **Report:** 0.657
   - **Actual:** 0.600
   - **Difference:** Slightly lower

## Summary of Discrepancies

### Critical Differences:
1. **Final Reward:** Report +89.18 vs Actual +77.26 (-11.92 points, 15% lower)
2. **Training Progression:** Report starts at +26.67 (500k) vs Actual +39.95 (500k)
3. **Episode Statistics:** Completely different values (mean length, distance)
4. **Action Distribution:** Roll usage is HIGHER in actual (10.21% vs 7.81%)

### Minor Differences:
- Policy Loss: 0.0233 vs 0.0240 (very close)
- Value Loss: 0.985 vs 1.027 (slightly higher)
- Entropy: 0.657 vs 0.600 (slightly lower)

## Notes
- The report appears to use data from a different training run (possibly training_20251214_194855 or an earlier run)
- Episode length in report is stated as "steps" but actual data shows time-based lengths (seconds)
- Roll usage is actually HIGHER in this run (10.21%) than reported (7.81%) - this is a positive finding
- Final reward is significantly lower than reported (+77.26 vs +89.18)
- The report's training progression values don't match the checkpoint rewards from this run

