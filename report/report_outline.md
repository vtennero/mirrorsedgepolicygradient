1. Introduction (10%)
   Problem: Multi-objective parkour with aesthetic quality
   Challenge: Human feedback incompatible with accelerated training
   Contribution: Stochastic reward shaping as preference approximation

2. Background & Related Work (15%)
   - RLHF (Christiano et al.) - the ideal but infeasible approach
   - Reward shaping in RL (brief, 1-2 citations)
   - Your position: bridging the gap with practical constraints

3. Methodology (30%)
   3.1 Training Infrastructure
       - 28 parallel agents
       - 2M steps / 30min wall-clock time
       - Time acceleration necessitates offline preference modeling

   3.2 Reward Design
       - Base rewards: speed, energy efficiency
       - Style reward approximation: 40% episode flag for roll bonus
       - Rationale: stochastic injection mimics preference diversity
   
   3.3 State/Action Space
       (your discretization choices)

4. Implementation (15%)
   Unity ML-Agents setup
   Training hyperparameters
   Why you chose 40% (if there's reasoning, otherwise admit arbitrary)

5. Results & Analysis (25%)
   5.1 Behavioral Emergence
       - Does style bonus change learned behavior?
       - Compare 40% episodes vs 60% episodes qualitatively
       - Energy/speed trade-offs
   
   5.2 Training Dynamics
       - Convergence analysis
       - Style bonus impact on learning curves
   
   5.3 Limitation Analysis
       - What behaviors emerge that humans wouldn't actually prefer?
       - Where does the approximation break down?

6. Discussion & Future Work (5%)
   - Offline RLHF: record trajectories, get batch human rankings, retrain
   - Reward model pre-training from human-labeled clips
   - Active learning: query human for uncertain states only
