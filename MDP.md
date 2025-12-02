# MDP Variables

## State Space (Observations)
**Total: 13 floats**

- `toTarget` (3 floats): Target position relative to agent `(target.position - transform.position)`
- `velocity` (3 floats): Agent velocity `controller.velocity`
- `isGrounded` (1 float): Grounded state `1.0 if grounded, 0.0 if not`
- `platformRaycasts` (5 floats): Downward raycast distances at 2f, 4f, 6f, 8f, 10f ahead, normalized by maxRayDist (10f)
- `obstacleDistance` (1 float): Forward obstacle raycast distance, normalized by `obstacleRaycastDistance` (10f)

## Action Space
**Discrete, 1 branch, 3 actions**

- `0`: Idle/nothing
- `1`: Jump (only if `controller.isGrounded`)
- `2`: Run forward

## Rewards

- `progressReward`: `progressDelta * 0.1` (when `currentX - lastProgressZ > 0`)
- `aliveReward`: `0.001` per step (when `controller.isGrounded`)
- `timePenalty`: `-0.001` per fixed update
- `targetReachReward`: `+10.0` (if `distanceToTarget < 2.0`)
- `fallPenalty`: `-1.0` (if `y < -5.0` or `episodeTimer > 90.0`)

