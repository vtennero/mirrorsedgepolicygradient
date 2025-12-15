Essential Graphs (Must Have)
1. Training Curve: Cumulative Reward vs Training Steps
Axes:

X: Training steps (0 to 2M)
Y: Mean episode reward

Justification: This is the foundational graph for any RL paper. You claim monotonic improvement and specific checkpoint values (+26.67 at 500k → +89.18 at 2M), but I see no visual evidence. The RLHF paper you reference uses this exact format because it's the primary way to demonstrate learning progress and convergence. Without this, your claims about 234% improvement are unsubstantiated.
Confidence in your claims without this graph: 40/100
2. Action Distribution Over Time
Axes:

X: Training steps (0 to 2M)
Y: Percentage of each action (0-100%)
Lines: 5 separate lines for Idle, Jump, Jog, Sprint, Roll

Justification: You claim roll usage increased from 0.69% to 7.81% (11.3× improvement), but I have no idea when this happened during training. Did the agent discover rolls at 200k steps? 1.5M steps? This graph is critical for understanding behavioral emergence and validating that your stochastic reward shaping actually caused the behavior change rather than random exploration.
Confidence in causality claims without this: 25/100
3. Comparative Analysis: Baseline vs Current Configuration
Axes:

X: Training steps (0 to 2M)
Y: Mean episode reward
Lines: 3 lines (Baseline 0% style, v1 15% style, Current 40% style)

Justification: You present Table comparing three configurations but claim they all trained for 2M steps. I need to see if they converged at similar rates or if the 40% configuration learned faster/slower. This demonstrates whether your approach affects sample efficiency, not just final performance.
Confidence in comparative claims: 35/100
Important Supporting Graphs
4. Roll Usage vs Style Episode Frequency
Axes:

X: Style episode frequency (0%, 15%, 40%)
Y: Final roll usage percentage (with error bars if multiple runs)

Justification: You claim 40% was chosen empirically but only tested 15% and 40%. This ablation study graph would validate your design choice and show whether the relationship is linear or has diminishing returns. As a teacher, I'm skeptical that you properly explored this hyperparameter space.
Confidence in 40% selection: 30/100 (arbitrary choice inadequately justified)
5. Episode Length Distribution
Axes:

X: Episode length (steps)
Y: Frequency (histogram)
Overlay: Success vs failure episodes (different colors)

Justification: You report mean episode length 61.07 steps (range 4.90-68.50), but this tells me nothing about the distribution. Are most episodes successful? Are failures clustered at early timesteps? This reveals whether your agent reliably solves the task or just occasionally gets lucky.
Confidence in "reliable navigation" claim: 50/100
6. Stamina Management Over Episode Timeline
Axes:

X: Episode timestep (0 to ~850)
Y: Stamina level (0-100)
Lines: Multiple episode trajectories (showing variation)

Justification: You claim the agent learned strategic stamina management, but I have zero evidence. This graph would show whether stamina hovers around 20% (conservative), oscillates (strategic), or depletes completely (the "sprint bashing" problem you claim to have solved).
Confidence in stamina management claims: 20/100
7. Policy Loss and Value Loss Over Training
Axes:

X: Training steps (0 to 2M)
Y: Loss values (dual Y-axis for policy and value loss)
Lines: Policy loss, Value loss

Justification: You report final values (policy 0.0233, value 0.985) but I need to see convergence behavior. Did policy converge early? Did value loss stabilize or oscillate? This validates your claim of "stable, converged" learning.
Confidence in convergence claims: 45/100
Nice to Have (Strengthens Analysis)
8. Entropy Over Training
Axes:

X: Training steps
Y: Policy entropy

Justification: You report final entropy 0.657 and claim "high exploration maintained," but your beta decayed from 0.1 to 0.000289. I expect entropy to decay proportionally. If it didn't, that's interesting and needs explanation.
9. Distance Traveled Distribution
Axes:

X: Distance traveled (units)
Y: Frequency (histogram)

Justification: Mean 555.91 units (range 29.89-603.56) is a massive variance. This distribution reveals how often the agent reaches near-target distances vs. failing catastrophically.
10. Reward Component Breakdown Over Time
Axes:

X: Training steps
Y: Reward contribution (stacked area chart)
Areas: Progress reward, roll rewards, target reach, penalties

Justification: Your reward breakdown in Section 3.2.4 is theoretical. I want empirical evidence that progress reward dominates (79%) as you claim.