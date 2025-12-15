Critical Review: What to Cut, Clarify, or Relocate
SECTIONS TO NUKE
Section 3.1 (Training Infrastructure) - COMPLETE NUKE
The 28 parallel agents and 2M steps/30min are implementation details, not methodology. They don't contribute to understanding your core idea. If you must mention this, it's one sentence in Section 4.
Section 4 (Implementation) as standalone - NUKE THE SECTION
Unity ML-Agents and hyperparameters are appendix material at best. The grader doesn't care what framework you used unless it's novel. Your "why 40%": admit it's arbitrary/exploratory IN that section and move on.
"Your position: bridging the gap" in Section 2 - NUKE THIS PHRASE
Don't editorialize your own work in the background section. Let your methodology speak for itself.
SECTIONS NEEDING CRITICAL CLARIFICATION (or nuke if you can't)
Section 1: "Stochastic reward shaping as preference approximation"
This is your thesis, but it's unclear what this means. Clarify: Are you claiming that randomly giving 40% of episodes a style bonus approximates human preferences? Why would randomness approximate preferences? This needs a concrete hypothesis or you should nuke the framing entirely and just say "we explore stochastic reward injection."
Section 3.2: Reward Design - CLARIFY AS FOLLOWS:
What to write:
"At episode initialization, a Bernoulli trial (p=0.4) determines whether style bonuses are active for that entire episode. When active, roll actions receive +1.5 bonus atop the base +0.5 reward (total +2.0). When inactive, rolls receive only base reward (+0.5).
The 40% frequency is exploratory, selected after observing 15% frequency produced insufficient roll adoption (0.69% of actions). Higher frequencies risk overwhelming base objectives (speed, energy efficiency)."
What NOT to write:

Don't claim this "mimics preference diversity" unless you can defend why random episode-level switching approximates human aesthetic preferences
Don't present 40% as principled without justification

NUKE the current rationale: "stochastic injection mimics preference diversity"
REPLACE with honest framing:
"This episode-level stochasticity allows the agent to learn roll execution without requiring rolls in every situation, avoiding degenerate policies that sacrifice task performance for style points."
Goal for this subsection:
Make it crystal clear that:

The mechanism is episode-level switching (not per-timestep randomness)
The 40% value is empirically tuned, not theoretically justified
You're exploring whether conditional rewards can shape style without destroying base behavior

Section 5.1: "Compare 40% vs 60%"
You need to clarify NOW whether you actually ran this comparison. If you didn't, nuke this bullet. If you did, this is your core result. [answer: we didnt -> adjust accordingly]
MOVE TO APPENDIX (targeting 50% of content)
TO APPENDIX:

Entire Section 4 (Implementation details)
Section 3.3 (State/Action Space) - just reference "see Appendix A for discretization details"
Section 3.1 (if you insist on keeping it) - "Training was conducted with 28 parallel agents (Appendix B)"
Detailed hyperparameter tables
Extra plots/learning curves beyond the 2-3 most important ones
Any mathematical derivations for reward functions (keep intuition in main text)
Extended related work citations (keep only 3-4 most relevant in Section 2)

KEEP IN MAIN TEXT:

The core logic of why stochastic rewards might work (if you can articulate it)
One clear figure showing behavioral difference (with/without style bonus)
One learning curve showing convergence
Your main limitation: the gap between stochastic bonuses and actual human preferences

UPDATED STRUCTURE & GOALS
Section 1 (5%): Problem + why RLHF fails here + your specific approach in 2 sentences
Section 2 (10%): RLHF concept (Christiano), reward shaping (1 citation), why you can't use RLHF directly
Section 3 (40%):

3.1: Reward design (base + stochastic style bonus) with CLEAR operational definition
3.2: Why this might approximate preferences (hypothesis) OR honest admission it's exploratory
Reference appendix for implementation

Section 4 (35%): Results

4.1: Does style bonus change behavior? (quantitative + qualitative)
4.2: Learning dynamics (1-2 key plots)
4.3: Where it breaks / doesn't match human intuition

Section 5 (10%): What you learned + what would actually work (offline RLHF sketch)