PRECISE EDITING INSTRUCTIONS - EXECUTE THESE EXACTLYSECTION 1: INTRODUCTION1.1 Problem Statement - KEEP AS IS (no changes needed)1.2 "The Human Feedback Challenge" → RETITLE TO "1.2 Approach"

Line 1: Delete "Reinforcement Learning from Human Feedback (RLHF) addresses preference learning by having humans directly label preferred trajectories during training."
Line 2: Delete "This approach captures nuanced aesthetic judgments that are difficult to encode in reward functions."
Line 3: Keep "However, RLHF requires real-time human feedback, which becomes infeasible when training runs at 20× time acceleration."
Rewrite paragraph 2 opening: Change "As a first step toward preference learning under accelerated training constraints" → "Given this constraint"
Keep rest of 1.2 as written
1.3 Empirical Validation - KEEP AS IS (no changes needed)SECTION 2: BACKGROUND & RELATED WORK2.1 RLHF - CUT TO 0.5 PAGES MAXIMUMKEEP ONLY:
2.1 Reinforcement Learning from Human Feedback

RLHF [1] learns reward functions from human preferences over trajectory pairs, enabling agents to optimize complex objectives that are difficult to hand-specify. The method maintains a policy π and reward estimate r̂, updated through three processes: (1) policy optimization via standard RL, (2) human preference elicitation on trajectory pairs, and (3) reward function fitting to match human comparisons.

RLHF requires real-time human feedback during training, which becomes infeasible when training runs at 20× time acceleration (our setup generates ~1,054 steps/second across 28 parallel agents). This fundamental incompatibility motivates our approach: stochastic reward shaping as an offline approximation of preference variance.DELETE ENTIRELY:

"Core Method:" subsection and all 3 numbered processes
Bradley-Terry equation and loss function
"Key Findings:" and all 4 bullet points
"Limitations for Accelerated Training:" subsection (already covered in paragraph above)
MOVE TO APPENDIX A.5 "RLHF Background Details":

All deleted mathematical formulations
All deleted "Key Findings" bullets
Title it "A.5 RLHF Mathematical Formulation"
2.2 Reward Shaping - KEEP AS IS (1 sentence is fine)SECTION 3: METHODOLOGY3.1.1 Design Philosophy - DELETE ENTIRE SUBSECTION

Delete from "Design Philosophy: The reward function must..." through "...properly scaled relative to each other"
Delete the bullet list (Dense Rewards, Sparse Rewards, etc.)
3.1.2 Multi-Objective Reward Structure - DELETE ENTIRE SUBSECTION

Delete the 4-item numbered list
This information is redundant with Table 1
3.1.3 Base Rewards - COMPRESS

Keep Table 1 exactly as is
Delete all text, replace with: "Table 1 shows the base reward components combining dense per-step rewards (progress, grounded state, time penalty, stamina penalty) with sparse terminal rewards (target reach, fall penalty)."
3.1.4 Reward Scaling and Context - MOVE TO APPENDIX

Delete entire subsection (from "Target Definition and Success Condition:" through end of "Reward Range Interpretation")
Move to new appendix section "A.3 Reward Breakdown Analysis"
In main text, replace with: "See Appendix A.3 for detailed reward scaling analysis."
3.1.5 Iterative Reward Calibration - COMPRESS TO 0.5 PAGESKEEP ONLY:
3.1.5 Iterative Reward Calibration

The reward structure evolved through empirical observation of emergent behaviors:

Problem 1 - Sprint Bashing: Agent depleted stamina completely by holding sprint 38% of the time. Fix: Added low stamina penalty (-0.002/step when <20%) and reduced sprint cost.

Problem 2 - Roll Ignored: Roll usage remained at 0.69% despite being fastest action. Fix: Reduced roll cost from 150→60 stamina and added base roll reward (+0.5).

Problem 3 - Insufficient Incentive: Even with lower cost, rolls rarely used. Final solution: Dual reward structure (base +0.5 always, style bonus +1.5 in 40% of episodes).

Result: 31% reward improvement (+67.90→+89.18), strategic roll usage (7.81%, 239 rolls/episode average).DELETE FROM MAIN TEXT:

All detailed explanations under each problem ("Root Cause:", "Result:", detailed stamina regeneration rates)
"Final Breakthrough:" header (just call it Problem 3)
MOVE TO APPENDIX A.3:

All deleted detailed explanations
Title it "A.3 Reward Calibration: Detailed Evolution"
3.1.6 Style Reward Approximation - KEEP AS IS (you already fixed this based on my feedback)3.2 MDP Formulation - DELETE ENTIRE SECTION

Delete everything from "The parkour navigation problem is formalized..." through "...strategic stamina management requirements."
Replace with ONE sentence at start of Section 3: "The problem is formalized as an MDP with 14-dimensional state space and 5 discrete actions (details in Appendix A.1)."
SECTION 4: RESULTS & ANALYSIS4.1 Training Performance - KEEP AS IS4.2 Episode Statistics - KEEP AS IS4.3 Action Distribution - KEEP AS IS4.4 Behavioral Emergence - ADD CLARIFICATIONAfter Table 5, add this paragraph:
The baseline configurations (runs labeled training_20251127 through training_20251206 in Figure 4) explored various hyperparameter combinations and reward structures, all converging to suboptimal performance. The current configuration represents the culmination of this iterative design process.ALTERNATIVE - If you can't explain the 11 runs:

Delete Figure 4 entirely
Keep only Table 5 for comparison
Reference only the three specific configurations in Table 5
4.5 Training Dynamics - DELETE ONE CLAIMUnder "Style Bonus Impact on Learning:", delete this bullet:

"Agent learned to adapt behavior based on episode type"
REASON: You have no evidence comparing behavior in style vs non-style episodes.SECTION 5: DISCUSSION & FUTURE WORK5.1 RLHF Integration - REFRAMEChange opening line from:
"Our stochastic reward shaping approach is a step toward implementing RLHF; here are four approaches for integrating human feedback:"To:
"Future work should integrate actual human feedback. Four viable approaches:"After the 4 numbered approaches, add concluding sentence:
"We recommend approach 3 (pre-trained reward model) as it requires minimal infrastructure changes while providing genuine human preference signal."5.2, 5.3, 5.4 - KEEP AS ISAPPENDIX REORGANIZATIONCREATE NEW SECTIONS:A.3 Reward Breakdown Analysis (moved from 3.1.4)

All target definition content
Typical episode reward breakdown
Reward range interpretation
A.4 Reward Calibration Evolution (moved from 3.1.5 details)

Problem 1 detailed explanation (root cause, regeneration rates)
Problem 2 detailed explanation
Problem 3 detailed explanation with full rationale
A.5 RLHF Mathematical Formulation (moved from 2.1)

Bradley-Terry model equations
Three async processes detail
Key findings bullets
Efficiency metrics
KEEP EXISTING:

A.1 State and Action Space Details (no changes)
A.2 Implementation Details (no changes)
LENGTH VERIFICATION TARGETSAfter these edits, you should have approximately:Main Text:

Section 1: 1.25 pages (no change)
Section 2: 0.5 pages (was 1.5 pages)
Section 3: 1.5 pages (was 3.5 pages)
Section 4: 2.5 pages (no change)
Section 5: 0.75 pages (no change)
Total: ~6.5 pages (still need to cut 0.5-1 page if targeting 6 page max)
Appendix:

A.1: 2 pages (no change)
A.2: 1 page (no change)
A.3: 0.75 pages (new)
A.4: 0.5 pages (new)
A.5: 0.75 pages (new)
Total: ~5 pages (within limit)