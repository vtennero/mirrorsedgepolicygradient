#!/usr/bin/env python3
"""
Generate LaTeX-ready PDF figures from training run data.

Usage:
    python utils/generate_latex_figures.py training_20251214_155306
    
Exports all 10 figures to: report/[trainingXX...]_figures/
"""

import sys
import json
import yaml
import argparse
from pathlib import Path
from typing import Dict, List, Any, Optional
from datetime import datetime
import numpy as np
import matplotlib
matplotlib.use('PDF')  # Use PDF backend
import matplotlib.pyplot as plt
from matplotlib.backends.backend_pdf import PdfPages

# Path setup
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
RESULTS_DIR = PROJECT_ROOT / "src" / "results"
REPORT_DIR = PROJECT_ROOT / "report"


def parse_run_data(run_path: Path) -> Optional[Dict[str, Any]]:
    """Parse all data for a single run (reused from dashboard)."""
    try:
        run_data = {
            "run_id": run_path.name,
            "path": str(run_path),
            "exists": True
        }
        
        # Parse configuration.yaml
        config_path = run_path / "configuration.yaml"
        if config_path.exists():
            with open(config_path, 'r') as f:
                config = yaml.safe_load(f)
                
            behavior_name = list(config.get('behaviors', {}).keys())[0] if config.get('behaviors') else None
            if behavior_name:
                behavior = config['behaviors'][behavior_name]
                run_data['behavior_name'] = behavior_name
                run_data['full_config'] = config
                
                # Checkpoint settings
                checkpoint = config.get('checkpoint_settings', {})
                run_data['mode'] = 'inference' if checkpoint.get('inference') else 'training'
        
        # Parse training_status.json
        status_path = run_path / "run_logs" / "training_status.json"
        if status_path.exists():
            with open(status_path, 'r') as f:
                status = json.load(f)
                
            behavior_name = run_data.get('behavior_name', list(status.keys())[0])
            if behavior_name in status:
                behavior_status = status[behavior_name]
                checkpoints = behavior_status.get('checkpoints', [])
                if checkpoints:
                    run_data['checkpoints'] = checkpoints
        
        # Parse timers.json
        timers_path = run_path / "run_logs" / "timers.json"
        if timers_path.exists():
            with open(timers_path, 'r') as f:
                timers = json.load(f)
                
            metadata = timers.get('metadata', {})
            gauges = timers.get('gauges', {})
            
            # Extract timestamps (matching dashboard)
            start_time = int(metadata.get('start_time_seconds', 0))
            end_time = int(metadata.get('end_time_seconds', 0))
            
            run_data['timestamps'] = {
                'start': datetime.fromtimestamp(start_time).strftime('%Y-%m-%d %H:%M:%S') if start_time else None,
                'end': datetime.fromtimestamp(end_time).strftime('%Y-%m-%d %H:%M:%S') if end_time else None,
                'duration_seconds': end_time - start_time if end_time and start_time else None,
                'duration_minutes': (end_time - start_time) / 60 if end_time and start_time else None,
            }
            
            behavior_prefix = run_data.get('behavior_name', 'ParkourRunner')
            metrics = {}
            for key, data in gauges.items():
                if key.startswith(behavior_prefix):
                    metric_name = key.replace(f"{behavior_prefix}.", "")
                    metrics[metric_name] = {
                        'current': data.get('value'),
                        'min': data.get('min'),
                        'max': data.get('max'),
                        'count': data.get('count')
                    }
            
            run_data['metrics'] = metrics
            
            # Key metrics for display (matching dashboard)
            run_data['key_metrics'] = {
                'cumulative_reward_mean': metrics.get('Environment.CumulativeReward.mean', {}).get('current'),
                'cumulative_reward_max': metrics.get('Environment.CumulativeReward.mean', {}).get('max'),
                'episode_length_mean': metrics.get('Environment.EpisodeLength.mean', {}).get('current'),
                'total_steps': metrics.get('Step.sum', {}).get('current'),
                'entropy': metrics.get('Policy.Entropy.mean', {}).get('current'),
            }
        
        return run_data
        
    except Exception as e:
        print(f"Error parsing run {run_path.name}: {e}")
        return None


def get_all_runs() -> List[Dict[str, Any]]:
    """Get data for all runs in the results directory (reused from dashboard)."""
    if not RESULTS_DIR.exists():
        return []
    
    runs = []
    for run_dir in sorted(RESULTS_DIR.iterdir()):
        if run_dir.is_dir():
            run_data = parse_run_data(run_dir)
            if run_data:
                runs.append(run_data)
    
    # Sort by timestamp (newest first) - would need timestamps for this, but for now just return
    return runs


def downsample_data(x_data: List[float], y_data: List[float], max_points: int = 500) -> tuple:
    """Downsample data to max_points while preserving curve shape."""
    if len(x_data) <= max_points:
        return x_data, y_data
    
    # Use numpy for efficient downsampling
    indices = np.linspace(0, len(x_data) - 1, max_points, dtype=int)
    return [x_data[i] for i in indices], [y_data[i] for i in indices]


def format_steps(step: int) -> str:
    """Format training steps as 'X.XM' for display."""
    return f"{step / 1000000:.1f}M"


def plot_training_curve(run_data: Dict, run_id: str, output_path: Path):
    """Graph 1: Training Curve - Cumulative Reward vs Training Steps."""
    checkpoints = run_data.get('checkpoints', [])
    if not checkpoints:
        print(f"  ⚠ Skipping training curve: No checkpoint data")
        return
    
    sorted_cps = sorted(checkpoints, key=lambda x: x['steps'])
    steps = [cp['steps'] for cp in sorted_cps]
    rewards = [cp.get('reward', 0) for cp in sorted_cps]
    
    # Downsample if needed
    steps, rewards = downsample_data(steps, rewards)
    
    fig, ax = plt.subplots(figsize=(8, 5))
    ax.plot(steps, rewards, 'b-', linewidth=2, label='Mean Episode Reward')
    ax.fill_between(steps, rewards, alpha=0.3, color='blue')
    ax.set_xlabel('Training Steps', fontsize=11)
    ax.set_ylabel('Mean Episode Reward', fontsize=11)
    ax.set_title(f'Training Curve: Cumulative Reward vs Training Steps\n{run_id}', fontsize=12, fontweight='bold')
    ax.grid(True, alpha=0.3)
    ax.legend()
    
    # Format x-axis
    ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    
    plt.tight_layout()
    plt.savefig(output_path / 'training_curve.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: training_curve.pdf")


def plot_action_distribution(run_path: Path, run_id: str, output_path: Path):
    """Graph 2: Action Distribution Over Time."""
    action_file = run_path / "run_logs" / "action_distribution_over_time.json"
    if not action_file.exists():
        print(f"  ⚠ Skipping action distribution: File not found")
        return
    
    try:
        with open(action_file, 'r') as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        print(f"  ⚠ Skipping action distribution: Invalid JSON - {e}")
        return
    except Exception as e:
        print(f"  ⚠ Skipping action distribution: Failed to read file - {e}")
        return
    
    data_points = data.get('data', [])
    if not data_points:
        print(f"  ⚠ Skipping action distribution: No data points")
        return
    
    # Extract data
    steps = [d['step'] for d in data_points]
    actions = {
        'Idle': [d.get('idle', 0) for d in data_points],
        'Jump': [d.get('jump', 0) for d in data_points],
        'Jog': [d.get('jog', 0) for d in data_points],
        'Sprint': [d.get('sprint', 0) for d in data_points],
        'Roll': [d.get('roll', 0) for d in data_points],
    }
    
    # Downsample
    steps, _ = downsample_data(steps, steps)
    for key in actions:
        _, actions[key] = downsample_data(steps, actions[key])
    
    fig, ax = plt.subplots(figsize=(8, 5))
    colors = {
        'Idle': 'gray',
        'Jump': 'red',
        'Jog': 'blue',
        'Sprint': 'orange',
        'Roll': 'teal'
    }
    
    for action, values in actions.items():
        ax.plot(steps, values, linewidth=2, label=action, color=colors[action])
    
    ax.set_xlabel('Training Steps', fontsize=11)
    ax.set_ylabel('Percentage (%)', fontsize=11)
    ax.set_title(f'Action Distribution Over Time\n{run_id}', fontsize=12, fontweight='bold')
    ax.set_ylim(0, 100)
    ax.grid(True, alpha=0.3)
    ax.legend()
    
    ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    
    plt.tight_layout()
    plt.savefig(output_path / 'action_distribution.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: action_distribution.pdf")


def plot_comparative(run_id: str, output_path: Path):
    """Graph 3: Comparative Analysis - Baseline vs Current Configuration."""
    # Get all training runs (matching dashboard method)
    all_runs = get_all_runs()
    training_runs = [r for r in all_runs if r.get('mode') == 'training']
    
    if len(training_runs) < 2:
        print(f"  ⚠ Skipping comparative: Need at least 2 training runs")
        return
    
    # Limit to 10 runs for readability (matching dashboard)
    training_runs = training_runs[:10]
    
    fig, ax = plt.subplots(figsize=(8, 5))
    colors = plt.cm.tab10(np.linspace(0, 1, len(training_runs)))
    
    for idx, run in enumerate(training_runs):
        checkpoints = run.get('checkpoints', [])
        if not checkpoints:
            continue
        
        sorted_cps = sorted(checkpoints, key=lambda x: x['steps'])
        steps = [cp['steps'] for cp in sorted_cps]
        rewards = [cp.get('reward', 0) for cp in sorted_cps]
        
        # Downsample
        steps, rewards = downsample_data(steps, rewards)
        
        ax.plot(steps, rewards, linewidth=1.5, label=run['run_id'], color=colors[idx], alpha=0.8)
    
    ax.set_xlabel('Training Steps', fontsize=11)
    ax.set_ylabel('Mean Episode Reward', fontsize=11)
    ax.set_title(f'Comparative Analysis: Baseline vs Current Configuration\n{run_id}', fontsize=12, fontweight='bold')
    ax.grid(True, alpha=0.3)
    ax.legend(fontsize=8, ncol=2)
    
    ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    
    plt.tight_layout()
    plt.savefig(output_path / 'comparative_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: comparative_analysis.pdf")


def plot_roll_usage(run_path: Path, run_id: str, output_path: Path):
    """Graph 4: Roll Usage vs Style Episode Frequency."""
    # Get data from all training runs for comparison (matching dashboard method)
    all_runs = get_all_runs()
    training_runs = [r for r in all_runs if r.get('mode') == 'training']
    
    data_points = []
    for run in training_runs:
        run_dir = RESULTS_DIR / run['run_id']
        
        # Try to get style frequency from metadata.json (matching dashboard fallback logic)
        metadata_file = run_dir / "metadata.json"
        style_frequency = None
        if metadata_file.exists():
            try:
                with open(metadata_file, 'r') as f:
                    metadata = json.load(f)
                    style_frequency = metadata.get('styleEpisodeFrequency') or metadata.get('style_episode_frequency')
            except Exception:
                pass
        
        # Fallback: try to get from config file
        if style_frequency is None:
            config_file = run_dir / "configuration.yaml"
            if config_file.exists():
                try:
                    with open(config_file, 'r') as f:
                        config = yaml.safe_load(f)
                        # Default fallback to 0.4 if not found
                        style_frequency = 0.4
                except Exception:
                    pass
        
        # If still None, use default
        if style_frequency is None:
            style_frequency = 0.4
        
        # Get roll usage from metrics
        metrics = run.get('metrics', {})
        roll_usage = metrics.get('Actions.RollPercentage.mean', {}).get('current')
        
        if roll_usage is not None:
            data_points.append((style_frequency * 100, roll_usage))
    
    if not data_points:
        print(f"  ⚠ Skipping roll usage: No comparison data available")
        return
    
    x_vals, y_vals = zip(*data_points)
    
    fig, ax = plt.subplots(figsize=(8, 5))
    ax.scatter(x_vals, y_vals, s=100, alpha=0.6, color='teal', edgecolors='black', linewidth=1)
    ax.set_xlabel('Style Episode Frequency (%)', fontsize=11)
    ax.set_ylabel('Final Roll Usage (%)', fontsize=11)
    ax.set_title(f'Roll Usage vs Style Episode Frequency\n{run_id}', fontsize=12, fontweight='bold')
    ax.set_xlim(0, 50)
    ax.grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig(output_path / 'roll_usage.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: roll_usage.pdf")


def plot_episode_length_dist(run_path: Path, run_id: str, output_path: Path):
    """Graph 5: Episode Length Distribution."""
    episode_file = run_path / "run_logs" / "episode_data.json"
    if not episode_file.exists():
        print(f"  ⚠ Skipping episode length dist: File not found")
        return
    
    with open(episode_file, 'r') as f:
        data = json.load(f)
    
    episodes = data.get('episodes', [])
    if not episodes:
        print(f"  ⚠ Skipping episode length dist: No episode data")
        return
    
    # Check if this is TensorBoard checkpoint data vs individual episode data (matching dashboard logic)
    is_checkpoint_data = len(episodes) < 200 and any(e.get('stepCount', 0) % 20000 == 0 for e in episodes)
    
    fig, ax = plt.subplots(figsize=(8, 5))
    
    if is_checkpoint_data:
        # TensorBoard checkpoint data: show as time series (mean episode length over training)
        step_counts = [e.get('stepCount', 0) for e in episodes]
        lengths = [e.get('length', 0) for e in episodes]
        
        # Downsample
        step_counts, lengths = downsample_data(step_counts, lengths)
        
        ax.plot(step_counts, lengths, 'b-', linewidth=2, label='Mean Episode Length')
        ax.fill_between(step_counts, lengths, alpha=0.3, color='blue')
        ax.set_xlabel('Training Steps', fontsize=11)
        ax.set_ylabel('Mean Episode Length (steps)', fontsize=11)
        ax.set_title(f'Episode Length Over Training\n{run_id}', fontsize=12, fontweight='bold')
        ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    else:
        # Individual episode data: show as histogram
        success_lengths = [e['length'] for e in episodes if e.get('success', False)]
        failure_lengths = [e['length'] for e in episodes if not e.get('success', False)]
        
        if not success_lengths and not failure_lengths:
            print(f"  ⚠ Skipping episode length dist: No length data")
            return
        
        max_length = max(max(success_lengths) if success_lengths else 0, max(failure_lengths) if failure_lengths else 0)
        bins = np.linspace(0, max_length, 21)
        
        if success_lengths:
            ax.hist(success_lengths, bins=bins, alpha=0.6, label='Success Episodes', color='teal', edgecolor='black')
        if failure_lengths:
            ax.hist(failure_lengths, bins=bins, alpha=0.6, label='Failure Episodes', color='red', edgecolor='black')
        
        ax.set_xlabel('Episode Length (steps)', fontsize=11)
        ax.set_ylabel('Frequency', fontsize=11)
        ax.set_title(f'Episode Length Distribution\n{run_id}', fontsize=12, fontweight='bold')
    
    ax.legend()
    ax.grid(True, alpha=0.3, axis='y')
    
    plt.tight_layout()
    plt.savefig(output_path / 'episode_length_dist.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: episode_length_dist.pdf")


def plot_stamina(run_path: Path, run_id: str, output_path: Path):
    """Graph 6: Stamina Management Over Episode Timeline."""
    stamina_file = run_path / "run_logs" / "stamina_trajectories.json"
    if not stamina_file.exists():
        print(f"  ⚠ Skipping stamina: File not found")
        return
    
    try:
        with open(stamina_file, 'r') as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        print(f"  ⚠ Skipping stamina: Invalid JSON - {e}")
        return
    except Exception as e:
        print(f"  ⚠ Skipping stamina: Failed to read file - {e}")
        return
    
    # Ensure data structure is correct (matching dashboard validation)
    if 'trajectories' not in data:
        print(f"  ⚠ Skipping stamina: Missing 'trajectories' key")
        return
    
    trajectories = data.get('trajectories', [])
    if not trajectories:
        print(f"  ⚠ Skipping stamina: No trajectory data")
        return
    
    # Validate and clean data (matching dashboard method)
    valid_trajectories = []
    for traj in trajectories:
        if 'episodeNumber' in traj and 'dataPoints' in traj:
            data_points = traj.get('dataPoints', [])
            if data_points:  # Only include trajectories with data
                valid_trajectories.append(traj)
    
    if not valid_trajectories:
        print(f"  ⚠ Skipping stamina: No valid trajectory data")
        return
    
    # Show up to 10 episodes
    valid_trajectories = valid_trajectories[:10]
    
    fig, ax = plt.subplots(figsize=(8, 5))
    colors = plt.cm.tab10(np.linspace(0, 1, len(valid_trajectories)))
    
    for idx, traj in enumerate(valid_trajectories):
        data_points = traj.get('dataPoints', [])
        if not data_points:
            continue
        
        timesteps = [dp['timestep'] for dp in data_points]
        stamina = [dp['stamina'] for dp in data_points]
        
        ax.plot(timesteps, stamina, linewidth=1.5, label=f"Episode {traj.get('episodeNumber', idx)}", 
                color=colors[idx], alpha=0.7)
    
    ax.set_xlabel('Episode Timestep', fontsize=11)
    ax.set_ylabel('Stamina Level (0-100)', fontsize=11)
    ax.set_title(f'Stamina Management Over Episode Timeline\n{run_id}', fontsize=12, fontweight='bold')
    ax.set_ylim(0, 100)
    ax.grid(True, alpha=0.3)
    ax.legend(fontsize=8, ncol=2)
    
    plt.tight_layout()
    plt.savefig(output_path / 'stamina.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: stamina.pdf")


def plot_loss(run_path: Path, run_id: str, output_path: Path):
    """Graph 7: Policy Loss and Value Loss Over Training."""
    loss_file = run_path / "run_logs" / "losses_over_time.json"
    if not loss_file.exists():
        print(f"  ⚠ Skipping loss: File not found")
        return
    
    try:
        with open(loss_file, 'r') as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        print(f"  ⚠ Skipping loss: Invalid JSON - {e}")
        return
    except Exception as e:
        print(f"  ⚠ Skipping loss: Failed to read file - {e}")
        return
    
    data_points = data.get('data', [])
    if not data_points:
        print(f"  ⚠ Skipping loss: No data points")
        return
    
    steps = [d['step'] for d in data_points]
    policy_loss = [d.get('policy_loss') for d in data_points if d.get('policy_loss') is not None]
    value_loss = [d.get('value_loss') for d in data_points if d.get('value_loss') is not None]
    
    policy_steps = [d['step'] for d in data_points if d.get('policy_loss') is not None]
    value_steps = [d['step'] for d in data_points if d.get('value_loss') is not None]
    
    if not policy_steps and not value_steps:
        print(f"  ⚠ Skipping loss: No loss data")
        return
    
    fig, ax1 = plt.subplots(figsize=(8, 5))
    
    if policy_steps:
        policy_steps, policy_loss = downsample_data(policy_steps, policy_loss)
        ax1.plot(policy_steps, policy_loss, 'r-', linewidth=2, label='Policy Loss')
        ax1.set_ylabel('Policy Loss', fontsize=11, color='r')
        ax1.tick_params(axis='y', labelcolor='r')
    
    if value_steps:
        value_steps, value_loss = downsample_data(value_steps, value_loss)
        ax2 = ax1.twinx()
        ax2.plot(value_steps, value_loss, 'orange', linewidth=2, label='Value Loss')
        ax2.set_ylabel('Value Loss', fontsize=11, color='orange')
        ax2.tick_params(axis='y', labelcolor='orange')
    
    ax1.set_xlabel('Training Steps', fontsize=11)
    ax1.set_title(f'Policy Loss and Value Loss Over Training\n{run_id}', fontsize=12, fontweight='bold')
    ax1.grid(True, alpha=0.3)
    
    # Combine legends
    lines1, labels1 = ax1.get_legend_handles_labels()
    if value_steps:
        lines2, labels2 = ax2.get_legend_handles_labels()
        ax1.legend(lines1 + lines2, labels1 + labels2, loc='upper right')
    else:
        ax1.legend(loc='upper right')
    
    ax1.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    
    plt.tight_layout()
    plt.savefig(output_path / 'loss.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: loss.pdf")


def plot_entropy(run_path: Path, run_id: str, output_path: Path):
    """Graph 8: Entropy Over Training."""
    entropy_file = run_path / "run_logs" / "entropy_over_time.json"
    if not entropy_file.exists():
        print(f"  ⚠ Skipping entropy: File not found")
        return
    
    try:
        with open(entropy_file, 'r') as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        print(f"  ⚠ Skipping entropy: Invalid JSON - {e}")
        return
    except Exception as e:
        print(f"  ⚠ Skipping entropy: Failed to read file - {e}")
        return
    
    data_points = data.get('data', [])
    if not data_points:
        print(f"  ⚠ Skipping entropy: No data points")
        return
    
    steps = [d['step'] for d in data_points]
    entropy = [d['entropy'] for d in data_points]
    
    # Downsample
    steps, entropy = downsample_data(steps, entropy)
    
    fig, ax = plt.subplots(figsize=(8, 5))
    ax.plot(steps, entropy, 'purple', linewidth=2, label='Policy Entropy')
    ax.fill_between(steps, entropy, alpha=0.3, color='purple')
    ax.set_xlabel('Training Steps', fontsize=11)
    ax.set_ylabel('Policy Entropy', fontsize=11)
    ax.set_title(f'Entropy Over Training\n{run_id}', fontsize=12, fontweight='bold')
    ax.grid(True, alpha=0.3)
    ax.legend()
    
    ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    
    plt.tight_layout()
    plt.savefig(output_path / 'entropy.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: entropy.pdf")


def plot_distance(run_path: Path, run_id: str, output_path: Path):
    """Graph 9: Distance Traveled Distribution."""
    episode_file = run_path / "run_logs" / "episode_data.json"
    if not episode_file.exists():
        print(f"  ⚠ Skipping distance: File not found")
        return
    
    with open(episode_file, 'r') as f:
        data = json.load(f)
    
    episodes = data.get('episodes', [])
    if not episodes:
        print(f"  ⚠ Skipping distance: No episode data")
        return
    
    success_distances = [e['maxDistance'] for e in episodes if e.get('success', False)]
    failure_distances = [e['maxDistance'] for e in episodes if not e.get('success', False)]
    
    if not success_distances and not failure_distances:
        print(f"  ⚠ Skipping distance: No distance data")
        return
    
    fig, ax = plt.subplots(figsize=(8, 5))
    
    max_dist = max(max(success_distances) if success_distances else 0, 
                   max(failure_distances) if failure_distances else 0)
    bins = np.linspace(0, max_dist, 21)
    
    if success_distances:
        ax.hist(success_distances, bins=bins, alpha=0.6, label='Success Episodes', 
                color='teal', edgecolor='black')
    if failure_distances:
        ax.hist(failure_distances, bins=bins, alpha=0.6, label='Failure Episodes', 
                color='red', edgecolor='black')
    
    ax.set_xlabel('Distance Traveled (units)', fontsize=11)
    ax.set_ylabel('Frequency', fontsize=11)
    ax.set_title(f'Distance Traveled Distribution\n{run_id}', fontsize=12, fontweight='bold')
    ax.legend()
    ax.grid(True, alpha=0.3, axis='y')
    
    plt.tight_layout()
    plt.savefig(output_path / 'distance.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: distance.pdf")


def plot_reward_breakdown(run_path: Path, run_id: str, output_path: Path):
    """Graph 10: Reward Component Breakdown Over Time."""
    reward_file = run_path / "run_logs" / "reward_components.json"
    if not reward_file.exists():
        print(f"  ⚠ Skipping reward breakdown: File not found")
        return
    
    with open(reward_file, 'r') as f:
        data = json.load(f)
    
    rewards = data.get('rewards', [])
    if not rewards:
        print(f"  ⚠ Skipping reward breakdown: No reward data")
        return
    
    # Sort by stepCount
    rewards = sorted(rewards, key=lambda x: x.get('stepCount', 0))
    
    step_counts = [r.get('stepCount', 0) for r in rewards]
    
    components = {
        'Progress Reward': [r.get('progressReward', 0) for r in rewards],
        'Roll Base Reward': [r.get('rollBaseReward', 0) for r in rewards],
        'Roll Style Bonus': [r.get('rollStyleBonus', 0) for r in rewards],
        'Target Reach Reward': [r.get('targetReachReward', 0) for r in rewards],
        'Grounded Reward': [r.get('groundedReward', 0) for r in rewards],
        'Low Stamina Penalty': [r.get('lowStaminaPenalty', 0) for r in rewards],
        'Time Penalty': [r.get('timePenalty', 0) for r in rewards],
        'Fall Penalty': [r.get('fallPenalty', 0) for r in rewards],
    }
    
    # Downsample all data using the same indices to ensure consistent lengths
    if len(step_counts) > 500:
        indices = np.linspace(0, len(step_counts) - 1, 500, dtype=int)
        step_counts = [step_counts[i] for i in indices]
        for key in components:
            components[key] = [components[key][i] for i in indices]
    
    fig, ax = plt.subplots(figsize=(8, 5))
    
    colors = {
        'Progress Reward': 'blue',
        'Roll Base Reward': 'red',
        'Roll Style Bonus': 'orange',
        'Target Reach Reward': 'teal',
        'Grounded Reward': 'purple',
        'Low Stamina Penalty': 'gray',
        'Time Penalty': 'yellow',
        'Fall Penalty': 'darkred'
    }
    
    # Stacked area chart
    ax.stackplot(step_counts, *components.values(), labels=components.keys(), 
                 colors=[colors[k] for k in components.keys()], alpha=0.7)
    
    ax.set_xlabel('Training Steps', fontsize=11)
    ax.set_ylabel('Reward Contribution', fontsize=11)
    ax.set_title(f'Reward Component Breakdown Over Time\n{run_id}', fontsize=12, fontweight='bold')
    ax.legend(fontsize=8, ncol=2, loc='upper left')
    ax.grid(True, alpha=0.3)
    
    ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: format_steps(int(x))))
    
    plt.tight_layout()
    plt.savefig(output_path / 'reward_breakdown.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"  ✓ Generated: reward_breakdown.pdf")


def get_training_runs_sorted() -> List[Dict[str, Any]]:
    """Get all training runs sorted by timestamp (newest last)."""
    all_runs = get_all_runs()
    training_runs = [r for r in all_runs if r.get('mode') == 'training']
    
    # Sort by timestamp if available, otherwise by directory modification time
    def sort_key(run):
        # Try to get timestamp from run data
        timestamps = run.get('timestamps', {})
        start = timestamps.get('start')
        if start:
            # Parse timestamp string
            try:
                return datetime.strptime(start, '%Y-%m-%d %H:%M:%S')
            except:
                pass
        
        # Fallback to directory modification time
        run_path = RESULTS_DIR / run['run_id']
        if run_path.exists():
            return datetime.fromtimestamp(run_path.stat().st_mtime)
        return datetime.min
    
    training_runs.sort(key=sort_key)
    return training_runs


def main():
    parser = argparse.ArgumentParser(
        description="Generate LaTeX-ready PDF figures from training run data"
    )
    parser.add_argument(
        "run_id",
        type=str,
        nargs='?',
        default=None,
        help="Training run ID (e.g., training_20251214_155306). If not provided, will show interactive selection."
    )
    
    args = parser.parse_args()
    
    # If run_id not provided, show interactive selection
    if args.run_id is None:
        training_runs = get_training_runs_sorted()
        
        if not training_runs:
            print(f"ERROR: No training runs found in {RESULTS_DIR}")
            sys.exit(1)
        
        print("\nAvailable training runs (newest at bottom):")
        print("=" * 70)
        for idx, run in enumerate(training_runs, 1):
            run_id = run['run_id']
            timestamp = run.get('timestamps', {}).get('start', 'N/A')
            reward = run.get('key_metrics', {}).get('cumulative_reward_mean', 'N/A')
            if isinstance(reward, (int, float)):
                reward = f"{reward:.2f}"
            print(f"  [{idx:2d}] {run_id}")
            print(f"       Date: {timestamp} | Reward: {reward}")
        
        print("=" * 70)
        print(f"\nDefault (newest): [{len(training_runs)}] {training_runs[-1]['run_id']}")
        
        while True:
            try:
                selection = input(f"\nSelect run [1-{len(training_runs)}, default={len(training_runs)}]: ").strip()
                
                if not selection:
                    # Default to newest (last in list)
                    selected_idx = len(training_runs) - 1
                    break
                
                selected_idx = int(selection) - 1
                if 0 <= selected_idx < len(training_runs):
                    break
                else:
                    print(f"Please enter a number between 1 and {len(training_runs)}")
            except ValueError:
                print("Please enter a valid number")
            except KeyboardInterrupt:
                print("\nCancelled.")
                sys.exit(0)
        
        selected_run = training_runs[selected_idx]
        args.run_id = selected_run['run_id']
        print(f"\nSelected: {args.run_id}\n")
    
    # Find run directory
    run_path = RESULTS_DIR / args.run_id
    if not run_path.exists():
        print(f"ERROR: Training run not found: {args.run_id}")
        print(f"Available runs in {RESULTS_DIR}:")
        training_runs = get_training_runs_sorted()
        for run in training_runs:
            print(f"  - {run['run_id']}")
        sys.exit(1)
    
    # Parse run data
    print(f"Loading data for: {args.run_id}")
    run_data = parse_run_data(run_path)
    if not run_data:
        print(f"ERROR: Failed to parse run data")
        sys.exit(1)
    
    # Create output directory
    output_dir = REPORT_DIR / f"{args.run_id}_figures"
    output_dir.mkdir(parents=True, exist_ok=True)
    print(f"Output directory: {output_dir}")
    print()
    
    # Generate all figures
    print("Generating figures...")
    plot_training_curve(run_data, args.run_id, output_dir)
    plot_action_distribution(run_path, args.run_id, output_dir)
    plot_comparative(args.run_id, output_dir)
    plot_roll_usage(run_path, args.run_id, output_dir)
    plot_episode_length_dist(run_path, args.run_id, output_dir)
    plot_stamina(run_path, args.run_id, output_dir)
    plot_loss(run_path, args.run_id, output_dir)
    plot_entropy(run_path, args.run_id, output_dir)
    plot_distance(run_path, args.run_id, output_dir)
    plot_reward_breakdown(run_path, args.run_id, output_dir)
    
    print()
    print(f"✓ All figures generated in: {output_dir}")
    print(f"  Upload these PDF files to Overleaf and use \\includegraphics{{filename.pdf}}")


if __name__ == "__main__":
    main()
