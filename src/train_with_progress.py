#!/usr/bin/env python3
"""
Wrapper for mlagents-learn that adds completion percentage to training output.
Automatically generates a run-id based on date/time (format: training_YYYYMMDD_HHMMSS).
Usage: python train_with_progress.py <config_file> [other_args...]
Example: python train_with_progress.py parkour_config.yaml --force
"""

import sys
import subprocess
import re
import yaml
from pathlib import Path
from datetime import datetime

def get_max_steps(config_file):
    """Extract max_steps from the YAML config file."""
    try:
        with open(config_file, 'r') as f:
            config = yaml.safe_load(f)
            # Get the first behavior's max_steps
            for behavior_name, behavior_config in config['behaviors'].items():
                max_steps = behavior_config.get('max_steps', None)
                if max_steps:
                    return max_steps, behavior_name
        return None, None
    except Exception as e:
        print(f"Warning: Could not read max_steps from config: {e}")
        return None, None

def generate_run_id():
    """Generate a run-id based on current date and time with 'training' prefix."""
    now = datetime.now()
    # Format: training_YYYYMMDD_HHMMSS
    run_id = f"training_{now.strftime('%Y%m%d_%H%M%S')}"
    return run_id

def main():
    if len(sys.argv) < 2:
        print("Usage: python train_with_progress.py <config_file> [additional args...]")
        print("Example: python train_with_progress.py parkour_config.yaml --force")
        print("Note: run-id is automatically generated (format: training_YYYYMMDD_HHMMSS)")
        sys.exit(1)
    
    config_file = sys.argv[1]
    additional_args = sys.argv[2:]
    
    # Remove any existing --run-id arguments (user-provided ones will be ignored)
    additional_args = [arg for arg in additional_args if not arg.startswith('--run-id')]
    
    # Always auto-generate run-id
    run_id = generate_run_id()
    additional_args.append(f"--run-id={run_id}")
    print(f"Auto-generated run-id: {run_id}")
    
    # Read max_steps from config
    max_steps, behavior_name = get_max_steps(config_file)
    
    if max_steps is None:
        print("Warning: Could not determine max_steps. Percentage will not be shown.")
    else:
        print(f"Training '{behavior_name}' with max_steps: {max_steps:,}")
        print("=" * 80)
    
    # Build the command
    cmd = ["mlagents-learn", config_file] + additional_args
    
    # Run mlagents-learn and intercept output
    process = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        bufsize=1,
        universal_newlines=True
    )
    
    # Pattern to match ML-Agents step output
    # Example: [INFO] ParkourRunner. Step: 680000. Time Elapsed: 735.333 s. Mean Reward: 9.899. Std of Reward: 3.424. Training.
    step_pattern = re.compile(r'\[INFO\]\s+(\w+)\.\s+Step:\s+(\d+)\.')
    
    try:
        for line in process.stdout:
            # Check if this is a step info line
            match = step_pattern.search(line)
            if match and max_steps:
                current_step = int(match.group(2))
                percentage = (current_step / max_steps) * 100
                
                # Insert percentage after "Step: XXXXX."
                modified_line = re.sub(
                    r'(Step:\s+\d+\.)',
                    rf'\1 [{percentage:.1f}%]',
                    line.rstrip()
                )
                print(modified_line, flush=True)
            else:
                # Print line as-is
                print(line, end='', flush=True)
    
    except KeyboardInterrupt:
        print("\n\nTraining interrupted by user.")
        process.terminate()
        sys.exit(1)
    
    # Wait for process to complete
    return_code = process.wait()
    sys.exit(return_code)

if __name__ == "__main__":
    main()

