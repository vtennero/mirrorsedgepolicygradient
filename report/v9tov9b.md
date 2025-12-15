MINOR ISSUES
Section 2.1 - Small redundancy
You write:
Our stochastic reward modulation extends traditional reward shaping [3], which modifies rewards to guide learning while preserving optimal policies, by introducing episodic stochastic reward modulation where reward structure varies probabilistically across episodes.
"stochastic reward modulation" appears twice in one sentence (beginning and end).
BETTER:
Our approach extends traditional reward shaping [3], which modifies rewards while preserving optimal policies, by introducing episodic stochasticity where reward structure varies probabilistically across episodes.

Section 4.4 - Missing context for Figure 4
You deleted the explanation of what the baseline runs tested. Now you have Figure 4 with 11 mysterious training curves and no explanation of what they represent.
Current text:
The stochastic reward shaping (40% style frequency) increased roll usage from 0.69% to 7.81% (11.3× increase) and final reward from +67.90 to +89.18 (31% improvement) compared to baseline configurations.
ADD after "configurations.":
Figure 4 shows 11 baseline runs exploring learning rates (3e-4 to 5e-4), beta values (0.05 to 0.2), style frequencies (0%, 10%, 15%), and roll costs (60-150 stamina); all underperformed the final 40% dual-reward configuration.
Without this, Figure 4 looks like random noise.

Appendix A.3 - Loss of detailed breakdown
You compressed this heavily, which is fine, but now it's unclear what "∼11%" and "79%" refer to without the calculation context.
Consider adding one sentence:
For a typical successful episode (~700 units progress, ~850 steps): progress provides ~70.0 reward (79%), target reach +10.0 (11%), with remaining from grounded/stamina/roll bonuses.
This gives readers the calculation anchor.