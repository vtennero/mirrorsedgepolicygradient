# MDP Variables

## State Space (Observations)
**Total: 14 floats**

- `toTarget` (3 floats): Target position relative to agent `(target.position - transform.position)`
- `velocity` (3 floats): Agent velocity `controller.velocity`
- `isGrounded` (1 float): Grounded state `1.0 if grounded, 0.0 if not`
- `platformRaycasts` (5 floats): Downward raycast distances at 2f, 4f, 6f, 8f, 10f ahead, normalized by maxRayDist (10f)
- `obstacleDistance` (1 float): Forward obstacle raycast distance, normalized by `obstacleRaycastDistance` (10f)
- `stamina` (1 float): Normalized stamina `currentStamina / maxStamina` (0.0 to 1.0)

## Action Space
**Discrete, 1 branch, 5 actions**

- `0`: Idle/nothing
- `1`: Jump (only if `controller.isGrounded` AND `stamina >= 20.0`, blocked if insufficient)
- `2`: Jog forward (normal speed, 6f)
- `3`: Sprint forward (faster speed, 12f, consumes stamina, blocked if `stamina <= 0` OR cooldown active → falls back to jog)
- `4`: Roll forward (forward traversal similar to jump, high stamina cost 150.0, blocked if `stamina < 150.0` OR already rolling)

## Rewards

- `progressReward`: `progressDelta * 0.1` (when `currentX - lastProgressZ > 0`)
- `aliveReward`: `0.001` per step (when `controller.isGrounded`)
- `timePenalty`: `-0.001` per fixed update
- `targetReachReward`: `+10.0` (if `distanceToTarget < 2.0`)
- `fallPenalty`: `-1.0` (if `y < -5.0` or `episodeTimer > 90.0`)
- `rollStyleBonus`: `+0.1` per roll action (only in 10-20% of episodes with style bonus enabled, episode-level flag)

## Stamina System

- `maxStamina`: 100.0
- `staminaConsumptionRate`: 33.33 per second (full bar depletes in 3 seconds)
- `jumpStaminaCost`: 20.0 per jump (increased from 5.0 to force stamina conservation)
- `rollStaminaCost`: 150.0 per roll (4-6x sprint cost, high-risk/high-reward action)
- `staminaRegenRate`: 20.0 per second (regenerates when not sprinting/jumping/rolling)
- `sprintCooldownDuration`: 0.5 seconds (prevents immediate re-sprint after stopping)
- Stamina consumption/regeneration happens in `FixedUpdate()` (physics step)
- Sprint cooldown: After sprinting ends, agent must wait 0.5s before sprinting again
- Roll duration: 0.6 seconds (roll is a timed action, cannot chain rolls)

## Style Bonus System

- `rollStyleBonus`: 0.1 (style bonus magnitude for roll actions)
- `styleEpisodeFrequency`: 0.15 (15% of episodes have style bonuses enabled)
- Style bonus flag is randomly assigned at episode start
- Only roll actions receive style bonus (when flag is enabled)
- Style bonus encourages occasional use of rolls when energy allows

