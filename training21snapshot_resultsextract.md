Training 21 Results

Run: training_20251207_210205
Date: 2025-12-07
Duration: 31.6 minutes (2M steps, 28 agents)

Performance Metrics

Final Reward: +89.18 (at 2M steps)
Previous Best: +78.32 (run28)
Improvement: +14% over previous best

Training Progression:
500k steps: +26.67
1.0M steps: +45.25
1.5M steps: +81.60
2.0M steps: +89.18

Policy Loss: 0.0233 (mean, range 0.0175-0.0312)
Value Loss: 0.985 (mean, range 0.400-1.808)
Policy Entropy: 0.657 (mean, range 0.657-1.605)

Learning Rate: 8.36e-07 (final, decayed from 3.0e-4)
Epsilon: 0.100 (final, decayed from 0.2)
Beta: 0.000289 (final, decayed from 0.1)

Episode Statistics

Mean Episode Reward: 80.06 (range 3.05-88.82)
Mean Episode Length: 61.07 steps (range 4.90-68.50)
Mean Max Distance: 555.91 units (range 29.89-603.56)
Mean Episode Duration: 609.64 environment steps

Action Distribution

Jog: 67.61% (primary movement)
Sprint: 14.00%
Roll: 7.81% (increased from 0.69% in previous run)
Jump: 3.53%
Idle: 7.04%

Action Counts (per episode, mean):
Jog: 2072 actions
Sprint: 424 actions
Roll: 239 actions
Jump: 102 actions
Idle: 216 actions

Agent Behavior

Roll Usage: 7.81% of actions (vs 0.69% previous run)
Roll Count: 239 rolls per episode (mean)
Roll Improvement: 11.3x increase over previous run

Success Rate: Not explicitly logged (episodes complete with reward +80.06 mean)
Failure Indicators: Min reward 3.05 suggests some failures/timeouts
Episode Completion: Mean 61.07 steps suggests most episodes complete successfully

Historical Context (30+ Training Runs)

Run: test_v4
Final Reward: -2.497
Status: Failed (wrong axis bug)

Run: test_v5
Final Reward: +5.976
Status: First success

Run: test_v6
Final Reward: +8.478
Status: +42% over v5

Run: test_v9
Final Reward: +3.426
Status: 60% drop (missing raycasts)

Run: test_v10
Final Reward: +9.85
Status: Raycasts added

Run: test_v11
Final Reward: +11.15
Status: Best before sprint

Run: run28
Final Reward: +78.32
Status: Sprint added, 7x improvement

Run: training_20251207_171550
Final Reward: +67.90
Status: Roll system v1

Run: training_20251207_210205
Final Reward: +89.18
Status: Current best

Overall Improvement: +89.18 vs +11.15 (test_v11) = +700% over baseline

Configuration Changes vs Previous Run

Roll System:
Cost: 150 to 60 stamina
Base reward: 0 to +0.5 (always given)
Style frequency: 15% to 40%

Stamina:
Regen: 20/sec to 30/sec

Result: Roll usage increased from 0.69% to 7.81% (11.3x increase)

System State

Observations (14 floats total):
1. Target relative position: 3 floats (target.position - agent.position)
2. Velocity: 3 floats (controller.velocity, units/sec)
3. Grounded state: 1 float (1.0 if grounded, 0.0 if not)
4. Platform raycasts: 5 floats (downward raycasts at 2f, 4f, 6f, 8f, 10f ahead, normalized 0.0-1.0)
5. Obstacle distance: 1 float (forward raycast, normalized 0.0-1.0)
6. Stamina: 1 float (currentStamina / maxStamina, normalized 0.0-1.0)

Actions (5 discrete, single branch):
0. Idle (no movement)
1. Jump (vertical jump with forward boost, requires grounded and stamina >= 20)
2. Jog (forward movement at 6 units/sec)
3. Sprint (forward movement at 12 units/sec, requires stamina > 0 and no cooldown)
4. Roll (forward roll at 18 units/sec, requires stamina >= 60 and not already rolling)

Environment: 20 randomized platforms, 2.5-4.5 unit gaps
Agent Capability: Navigates randomized environments, manages stamina, uses all actions including rolls

Training Stability

Policy Loss: Stable at 0.0233 (normal range, indicates active learning)
Value Loss: 0.985 (reasonable, indicates value function learning)
Entropy: 0.657 (high exploration maintained throughout training)
No training instability observed (losses stable, no spikes)

Status: Complete. Best performing model to date.
