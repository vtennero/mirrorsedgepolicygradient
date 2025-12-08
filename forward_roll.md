**Style Action Integration Specs**

**Action Space:**
Add "Roll Forward" as discrete action with same forward traversal capability as jump.

**Energy Costs:**
- Sprint: baseline stamina cost
- Jump: 2-3x sprint cost (indicative, tune)
- Roll Forward: 4-6x sprint cost (indicative, tune)

**Reward Structure:**
Implement episode-level style bonus flag:
- 10-20% of training episodes (indicative): roll actions receive +0.1 style bonus (tune magnitude)
- Remaining episodes: no style bonus, only energy cost applies
- Flag is randomly assigned at episode start, affects all rolls in that episode

**Failure Conditions:**
Standard fall penalty applies regardless of which action caused the fall.

**Inference:**
Sample from policy distribution (do not use argmax action selection). Policy stochasticity will produce occasional rolls based on learned value.

**Tuning Parameters to Test:**
1. Roll stamina cost multiplier (4-6x baseline)
2. Style bonus magnitude (0.05-0.2 range)
3. Style episode frequency (10-20%)

Agent should learn rolls are high-risk/high-reward and use them sparingly when energy allows.