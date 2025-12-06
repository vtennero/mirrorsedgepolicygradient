#!/usr/bin/env python3
"""
Wrapper script for running ML-Agents inference with custom time scale.
Generates a temporary config file with the specified time_scale instead of modifying the source config.

Usage:
    python run_inference.py                    # Default: time-scale=1.0
    python run_inference.py --time-scale=0.1   # 10x slower
    python run_inference.py --time-scale=0.01   # 100x slower
"""

import argparse
import sys
import os
import tempfile
import shutil
import yaml
import subprocess
import uuid
from pathlib import Path
from datetime import datetime

def load_yaml(filepath):
    """Load YAML file."""
    with open(filepath, 'r') as f:
        return yaml.safe_load(f)

def save_yaml(data, filepath):
    """Save YAML file."""
    with open(filepath, 'w') as f:
        yaml.dump(data, f, default_flow_style=False, sort_keys=False, allow_unicode=True)

def get_training_runs(results_dir):
    """Get list of all folders in results directory, sorted by modification time (newest first)."""
    training_runs = []
    results_path = Path(results_dir)
    
    if not results_path.exists():
        return training_runs
    
    # Collect all folders with their modification times
    runs_with_time = []
    for folder in results_path.iterdir():
        if folder.is_dir():
            # Get modification time of the folder
            mtime = folder.stat().st_mtime
            runs_with_time.append((mtime, folder.name))
    
    # Sort by modification time (ascending = oldest first, newest at bottom)
    runs_with_time.sort(key=lambda x: x[0])
    
    # Return just the names
    return [name for _, name in runs_with_time]

def select_training_run(results_dir):
    """Interactively select a training run to initialize from."""
    training_runs = get_training_runs(results_dir)
    
    if not training_runs:
        print("Error: No training runs found in results directory.")
        print("Make sure you have completed at least one training run.")
        sys.exit(1)
    
    print("\nAvailable training runs:")
    print("-" * 50)
    for i, run in enumerate(training_runs, 1):
        print(f"  {i}. {run}")
    print("-" * 50)
    
    while True:
        try:
            choice = input(f"\nSelect training run (1-{len(training_runs)}) or 'q' to quit: ").strip()
            
            if choice.lower() == 'q':
                print("Cancelled.")
                sys.exit(0)
            
            idx = int(choice) - 1
            if 0 <= idx < len(training_runs):
                selected = training_runs[idx]
                print(f"Selected: {selected}\n")
                return selected
            else:
                print(f"Please enter a number between 1 and {len(training_runs)}")
        except ValueError:
            print("Please enter a valid number or 'q' to quit")
        except KeyboardInterrupt:
            print("\n\nCancelled.")
            sys.exit(0)

def create_temp_config(base_config_path, time_scale, debug_id):
    """Create temporary config file with specified time_scale."""
    config = load_yaml(base_config_path)
    
    # Ensure engine_settings exists
    if 'engine_settings' not in config:
        config['engine_settings'] = {}
    
    # Set time_scale
    config['engine_settings']['time_scale'] = time_scale
    
    # DEBUG: Log the config being created
    print(f"[DEBUG-{debug_id}] Creating temp config with time_scale={time_scale}")
    print(f"[DEBUG-{debug_id}] engine_settings in config: {config.get('engine_settings', {})}")
    
    # Create temporary file
    temp_dir = Path(tempfile.gettempdir())
    temp_config = temp_dir / f"parkour_config_temp_{os.getpid()}.yaml"
    
    save_yaml(config, temp_config)
    
    # DEBUG: Verify the file was written correctly
    verify_config = load_yaml(temp_config)
    actual_time_scale = verify_config.get('engine_settings', {}).get('time_scale', 'NOT_SET')
    print(f"[DEBUG-{debug_id}] Temp config file written: {temp_config}")
    print(f"[DEBUG-{debug_id}] Verified time_scale in temp config: {actual_time_scale}")
    
    return temp_config

def main():
    parser = argparse.ArgumentParser(
        description='Run ML-Agents inference with custom time scale',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python run_inference.py                    # Default: time-scale=1.0 (normal speed)
  python run_inference.py --time-scale=0.1   # 10x slower
  python run_inference.py --time-scale=0.01  # 100x slower
        """
    )
    
    parser.add_argument('--time-scale', type=float, default=1.0, help='Time scale (default: 1.0 = normal, 0.1 = 10x slower, etc.)')
    parser.add_argument('--base-config', default='parkour_config.yaml', help='Base config file to use')
    parser.add_argument('--results-dir', default='results', help='Results directory containing training runs')
    
    # Parse known args to separate our args from mlagents-learn args
    args, mlagents_args = parser.parse_known_args()
    
    # Filter out --time-scale from mlagents_args (we handle it via config file)
    # Also handle --time-scale=VALUE format
    filtered_mlagents_args = []
    skip_next = False
    for i, arg in enumerate(mlagents_args):
        if skip_next:
            skip_next = False
            continue
        if arg == '--time-scale':
            # Skip this and the next argument (the value)
            skip_next = True
            continue
        elif arg.startswith('--time-scale='):
            # Skip this argument (contains both flag and value)
            continue
        filtered_mlagents_args.append(arg)
    
    # Get script directory and paths
    script_dir = Path(__file__).parent
    base_config_path = script_dir / args.base_config
    results_dir = script_dir / args.results_dir
    
    if not base_config_path.exists():
        print(f"Error: Base config file not found: {base_config_path}")
        sys.exit(1)
    
    # Generate unique debug ID for filtering logs
    debug_id = str(uuid.uuid4())[:8].upper()
    print(f"\n[DEBUG-{debug_id}] ========== TIMESCALE DEBUG SESSION ==========")
    print(f"[DEBUG-{debug_id}] Debug ID: {debug_id} (use this to filter logs)")
    print(f"[DEBUG-{debug_id}] Command line time_scale argument: {args.time_scale}")
    print(f"[DEBUG-{debug_id}] Base config path: {base_config_path}")
    
    # Check Unity TimeManager.asset
    time_manager_path = script_dir / 'ProjectSettings' / 'TimeManager.asset'
    if time_manager_path.exists():
        with open(time_manager_path, 'r') as f:
            content = f.read()
            if 'm_TimeScale:' in content:
                for line in content.split('\n'):
                    if 'm_TimeScale:' in line:
                        print(f"[DEBUG-{debug_id}] Unity TimeManager.asset m_TimeScale: {line.strip()}")
                        break
    
    # Auto-generate run ID with timestamp
    timestamp = datetime.now().strftime('%Y-%m-%d_%H-%M-%S')
    run_id = f"inference_v21_{timestamp}"
    print(f"[DEBUG-{debug_id}] Generated run_id: {run_id}")
    
    # Interactively select training run
    initialize_from = select_training_run(results_dir)
    print(f"[DEBUG-{debug_id}] Selected training run: {initialize_from}")
    
    # Create temporary config with time_scale
    print(f"[DEBUG-{debug_id}] Creating temporary config with time_scale={args.time_scale}...")
    temp_config = create_temp_config(base_config_path, args.time_scale, debug_id)
    print(f"[DEBUG-{debug_id}] Temporary config path: {temp_config}")
    
    # DEBUG: Print full engine_settings section
    final_config = load_yaml(temp_config)
    engine_settings = final_config.get('engine_settings', {})
    print(f"[DEBUG-{debug_id}] Final engine_settings in temp config: {engine_settings}")
    if 'time_scale' in engine_settings:
        print(f"[DEBUG-{debug_id}] ✓ time_scale is set to: {engine_settings['time_scale']}")
    else:
        print(f"[DEBUG-{debug_id}] ✗ ERROR: time_scale NOT FOUND in engine_settings!")
    
    # DEMO MODE ONLY: Write time_scale to a file Unity can read immediately
    # ⚠️ CRITICAL: This file is ONLY read when MLAGENTS_DEMO_MODE=true
    # ⚠️ Training is COMPLETELY UNAFFECTED - Unity skips reading this during training
    # Unity reads from Assets/../ which is the project root
    timescale_file = script_dir / "TIMESCALE.txt"
    with open(timescale_file, 'w') as f:
        f.write(str(args.time_scale))
    print(f"[DEBUG-{debug_id}] ✓ Wrote time_scale to {timescale_file}: {args.time_scale} (DEMO MODE ONLY - training unaffected)")
    
    try:
        # CRITICAL: Set environment variable so Unity can read it
        # ML-Agents doesn't apply engine_settings.time_scale in Editor mode
        os.environ['MLAGENTS_TIME_SCALE'] = str(args.time_scale)
        print(f"[DEBUG-{debug_id}] Set environment variable MLAGENTS_TIME_SCALE={args.time_scale}")
        
        # Build mlagents-learn command
        cmd = [
            'mlagents-learn',
            str(temp_config),
            '--run-id', run_id,
            '--initialize-from', initialize_from,
            '--inference'
        ]
        
        # Add any additional mlagents-learn arguments (excluding --time-scale which we handle via config)
        cmd.extend(filtered_mlagents_args)
        
        print(f"[DEBUG-{debug_id}] Full command: {' '.join(cmd)}")
        print(f"[DEBUG-{debug_id}] Config file being used: {temp_config}")
        print(f"[DEBUG-{debug_id}] Environment variable MLAGENTS_TIME_SCALE={os.environ.get('MLAGENTS_TIME_SCALE')}")
        print(f"[DEBUG-{debug_id}] ============================================\n")
        
        # Run mlagents-learn (environment variable will be inherited)
        result = subprocess.run(cmd, cwd=script_dir, env=os.environ.copy())
        
        return result.returncode
        
    finally:
        # Clean up temporary config
        if temp_config.exists():
            print(f"\nCleaning up temporary config: {temp_config}")
            temp_config.unlink()
        
        # Clean up TIMESCALE.txt file
        timescale_file = script_dir / "TIMESCALE.txt"
        if timescale_file.exists():
            print(f"Cleaning up TIMESCALE.txt")
            timescale_file.unlink()

if __name__ == '__main__':
    sys.exit(main())

