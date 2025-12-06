#!/usr/bin/env python3
"""Script to identify failed runs (0 cumulative reward) in results directory."""

import json
import os
from pathlib import Path

def get_cumulative_reward(run_path: Path) -> float:
    """Extract cumulative reward from a run's timers.json file."""
    timers_path = run_path / "run_logs" / "timers.json"
    
    if not timers_path.exists():
        return None  # No timers file = failed run
    
    try:
        with open(timers_path, 'r') as f:
            timers = json.load(f)
        
        gauges = timers.get('gauges', {})
        
        # Check for cumulative reward in gauges
        # Try different possible metric names
        behavior_prefixes = ['ParkourRunner', 'Environment']
        
        for prefix in behavior_prefixes:
            # Try Environment.CumulativeReward.mean
            key = f"{prefix}.Environment.CumulativeReward.mean"
            if key in gauges:
                return gauges[key].get('value', 0)
            
            # Try Episode.TotalReward.mean (used in test_v21)
            key = f"{prefix}.Episode.TotalReward.mean"
            if key in gauges:
                return gauges[key].get('value', 0)
        
        # If gauges exist but no reward metric found, check if gauges is empty
        if not gauges:
            return None  # No gauges = failed run
        
        # If gauges exist but no reward metric, return 0
        return 0
        
    except Exception as e:
        print(f"Error reading {timers_path}: {e}")
        return None

def main():
    results_dir = Path("src/results")
    
    if not results_dir.exists():
        print(f"Results directory not found: {results_dir}")
        return
    
    all_runs = []
    failed_runs = []
    
    # Get all folders in results directory
    for item in results_dir.iterdir():
        if item.is_dir():
            reward = get_cumulative_reward(item)
            all_runs.append({
                'name': item.name,
                'reward': reward,
                'path': str(item)
            })
            
            # Failed if reward is None, 0, negative, or missing
            if reward is None or reward <= 0:
                failed_runs.append(item.name)
    
    # Sort by name
    all_runs.sort(key=lambda x: x['name'])
    failed_runs.sort()
    
    print("=" * 80)
    print("ALL RUNS IN RESULTS DIRECTORY")
    print("=" * 80)
    for run in all_runs:
        reward_str = f"{run['reward']:.2f}" if run['reward'] is not None else "MISSING/FAILED"
        status = "[OK]" if run['reward'] and run['reward'] > 0 else "[FAIL]"
        print(f"{status} {run['name']:50s} Reward: {reward_str}")
    
    print("\n" + "=" * 80)
    print(f"FAILED RUNS (0 or missing reward): {len(failed_runs)}")
    print("=" * 80)
    for run_name in failed_runs:
        print(f"  - {run_name}")
    
    print("\n" + "=" * 80)
    print("PROPOSED DELETION COMMANDS")
    print("=" * 80)
    print("# Windows (PowerShell):")
    print("$failed = @(")
    for run_name in failed_runs:
        print(f'    "{run_name}",')
    print(")")
    print("foreach ($run in $failed) { Remove-Item -Recurse -Force \"src\\results\\$run\" }")
    
    print("\n# Linux/Mac (Bash):")
    for run_name in failed_runs:
        print(f'rm -rf "src/results/{run_name}"')
    
    print("\n# Or delete all at once:")
    print(f'rm -rf src/results/{" src/results/".join(failed_runs)}')

if __name__ == "__main__":
    main()

