"""
ML-Agents Training Dashboard
A web UI for visualizing and comparing training runs
"""
import os
import json
import yaml
from datetime import datetime
from pathlib import Path
from flask import Flask, render_template, jsonify
from typing import Dict, List, Any, Optional

app = Flask(__name__)

# Path to results directory (dashboard is now in utils/dashboard, so need to go up 2 levels)
RESULTS_DIR = Path(__file__).parent.parent.parent / "src" / "results"


def parse_run_data(run_path: Path) -> Optional[Dict[str, Any]]:
    """Parse all data for a single run."""
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
                
            # Extract key config details
            behavior_name = list(config.get('behaviors', {}).keys())[0] if config.get('behaviors') else None
            if behavior_name:
                behavior = config['behaviors'][behavior_name]
                run_data['config'] = {
                    'trainer_type': behavior.get('trainer_type'),
                    'max_steps': behavior.get('max_steps'),
                    'learning_rate': behavior.get('hyperparameters', {}).get('learning_rate'),
                    'batch_size': behavior.get('hyperparameters', {}).get('batch_size'),
                    'buffer_size': behavior.get('hyperparameters', {}).get('buffer_size'),
                    'hidden_units': behavior.get('network_settings', {}).get('hidden_units'),
                    'num_layers': behavior.get('network_settings', {}).get('num_layers'),
                    'time_horizon': behavior.get('time_horizon'),
                    'gamma': behavior.get('reward_signals', {}).get('extrinsic', {}).get('gamma'),
                }
                run_data['behavior_name'] = behavior_name
                
                # Engine settings
                engine = config.get('engine_settings', {})
                run_data['engine'] = {
                    'time_scale': engine.get('time_scale'),
                    'quality_level': engine.get('quality_level'),
                    'no_graphics': engine.get('no_graphics'),
                }
                
                # Checkpoint settings
                checkpoint = config.get('checkpoint_settings', {})
                run_data['mode'] = 'inference' if checkpoint.get('inference') else 'training'
                
                run_data['full_config'] = config
        
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
                    # Get latest checkpoint
                    latest = max(checkpoints, key=lambda x: x['steps'])
                    run_data['latest_checkpoint'] = {
                        'steps': latest['steps'],
                        'reward': latest.get('reward', 0),
                        'creation_time': latest['creation_time'],
                        'date': datetime.fromtimestamp(latest['creation_time']).strftime('%Y-%m-%d %H:%M:%S')
                    }
                    run_data['checkpoints'] = checkpoints
                    run_data['num_checkpoints'] = len(checkpoints)
        
        # Parse timers.json for metrics
        timers_path = run_path / "run_logs" / "timers.json"
        if timers_path.exists():
            with open(timers_path, 'r') as f:
                timers = json.load(f)
                
            metadata = timers.get('metadata', {})
            gauges = timers.get('gauges', {})
            
            # Extract timestamps
            start_time = int(metadata.get('start_time_seconds', 0))
            end_time = int(metadata.get('end_time_seconds', 0))
            
            run_data['timestamps'] = {
                'start': datetime.fromtimestamp(start_time).strftime('%Y-%m-%d %H:%M:%S') if start_time else None,
                'end': datetime.fromtimestamp(end_time).strftime('%Y-%m-%d %H:%M:%S') if end_time else None,
                'duration_seconds': end_time - start_time if end_time and start_time else None,
                'duration_minutes': (end_time - start_time) / 60 if end_time and start_time else None,
            }
            
            run_data['metadata'] = metadata
            
            # Extract key metrics from gauges
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
            
            # Key metrics for display
            run_data['key_metrics'] = {
                'cumulative_reward_mean': metrics.get('Environment.CumulativeReward.mean', {}).get('current'),
                'cumulative_reward_max': metrics.get('Environment.CumulativeReward.mean', {}).get('max'),
                'episode_length_mean': metrics.get('Environment.EpisodeLength.mean', {}).get('current'),
                'total_steps': metrics.get('Step.sum', {}).get('current'),
                'entropy': metrics.get('Policy.Entropy.mean', {}).get('current'),
                'is_training': metrics.get('IsTraining.mean', {}).get('current', 0) > 0,
                # PPO Loss Metrics
                'policy_loss': metrics.get('Losses.PolicyLoss.mean', {}).get('current'),
                'policy_loss_min': metrics.get('Losses.PolicyLoss.mean', {}).get('min'),
                'policy_loss_max': metrics.get('Losses.PolicyLoss.mean', {}).get('max'),
                'value_loss': metrics.get('Losses.ValueLoss.mean', {}).get('current'),
                'value_loss_min': metrics.get('Losses.ValueLoss.mean', {}).get('min'),
                'value_loss_max': metrics.get('Losses.ValueLoss.mean', {}).get('max'),
                # Learning Rate Schedule
                'learning_rate_current': metrics.get('Policy.LearningRate.mean', {}).get('current'),
                'epsilon_current': metrics.get('Policy.Epsilon.mean', {}).get('current'),
                'beta_current': metrics.get('Policy.Beta.mean', {}).get('current'),
            }
            
            run_data['full_timers'] = timers
        
        return run_data
        
    except Exception as e:
        print(f"Error parsing run {run_path.name}: {e}")
        return None


def get_all_runs() -> List[Dict[str, Any]]:
    """Get data for all runs in the results directory."""
    if not RESULTS_DIR.exists():
        return []
    
    runs = []
    for run_dir in sorted(RESULTS_DIR.iterdir()):
        if run_dir.is_dir():
            run_data = parse_run_data(run_dir)
            if run_data:
                runs.append(run_data)
    
    # Sort by timestamp (newest first)
    runs.sort(key=lambda x: x.get('timestamps', {}).get('start', ''), reverse=True)
    
    return runs


@app.route('/')
def index():
    """Main dashboard page."""
    return render_template('dashboard.html')


@app.route('/analysis')
def analysis():
    """Analysis page with detailed graphs."""
    return render_template('analysis.html')


@app.route('/api/runs')
def api_runs():
    """API endpoint to get all runs data."""
    runs = get_all_runs()
    return jsonify(runs)


@app.route('/api/run/<run_id>')
def api_run_detail(run_id):
    """API endpoint to get detailed data for a specific run."""
    run_path = RESULTS_DIR / run_id
    if run_path.exists():
        run_data = parse_run_data(run_path)
        if run_data:
            return jsonify(run_data)
    return jsonify({"error": "Run not found"}), 404


@app.route('/api/compare')
def api_compare():
    """API endpoint to get comparison data for all runs."""
    runs = get_all_runs()
    
    # Extract key comparison metrics
    comparison = {
        'runs': [],
        'metrics': {
            'cumulative_rewards': [],
            'episode_lengths': [],
            'steps': [],
            'training_times': [],
            'policy_loss': [],
            'value_loss': []
        }
    }
    
    for run in runs:
        run_summary = {
            'run_id': run['run_id'],
            'date': run.get('timestamps', {}).get('start'),
            'mode': run.get('mode'),
        }
        
        key_metrics = run.get('key_metrics', {})
        run_summary['reward'] = key_metrics.get('cumulative_reward_mean')
        run_summary['reward_max'] = key_metrics.get('cumulative_reward_max')
        run_summary['episode_length'] = key_metrics.get('episode_length_mean')
        run_summary['steps'] = key_metrics.get('total_steps')
        run_summary['duration'] = run.get('timestamps', {}).get('duration_minutes')
        run_summary['policy_loss'] = key_metrics.get('policy_loss')
        run_summary['value_loss'] = key_metrics.get('value_loss')
        
        comparison['runs'].append(run_summary)
        
        # For charts
        comparison['metrics']['cumulative_rewards'].append({
            'x': run['run_id'],
            'y': key_metrics.get('cumulative_reward_mean', 0)
        })
        comparison['metrics']['episode_lengths'].append({
            'x': run['run_id'],
            'y': key_metrics.get('episode_length_mean', 0)
        })
        comparison['metrics']['steps'].append({
            'x': run['run_id'],
            'y': key_metrics.get('total_steps', 0)
        })
        if run_summary['duration']:
            comparison['metrics']['training_times'].append({
                'x': run['run_id'],
                'y': run_summary['duration']
            })
        # PPO Loss metrics
        if key_metrics.get('policy_loss') is not None:
            comparison['metrics']['policy_loss'].append({
                'x': run['run_id'],
                'y': key_metrics.get('policy_loss')
            })
        if key_metrics.get('value_loss') is not None:
            comparison['metrics']['value_loss'].append({
                'x': run['run_id'],
                'y': key_metrics.get('value_loss')
            })
    
    return jsonify(comparison)


@app.route('/api/analysis/training-curve/<run_id>')
def api_training_curve(run_id):
    """API endpoint for training curve data (Graph 1)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    run_data = parse_run_data(run_path)
    if not run_data:
        return jsonify({"error": "Failed to parse run data"}), 500
    
    checkpoints = run_data.get('checkpoints', [])
    if not checkpoints:
        return jsonify({"error": "No checkpoint data available"}), 404
    
    # Sort by steps
    checkpoints = sorted(checkpoints, key=lambda x: x['steps'])
    
    data = {
        'steps': [cp['steps'] for cp in checkpoints],
        'rewards': [cp.get('reward', 0) for cp in checkpoints],
        'run_id': run_id
    }
    
    return jsonify(data)


@app.route('/api/analysis/action-distribution/<run_id>')
def api_action_distribution(run_id):
    """API endpoint for action distribution over time (Graph 2)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    # Try to read from extracted JSON file
    action_file = run_path / "run_logs" / "action_distribution_over_time.json"
    if action_file.exists():
        try:
            with open(action_file, 'r') as f:
                data = json.load(f)
                return jsonify(data)
        except Exception as e:
            return jsonify({"error": f"Failed to read action distribution file: {e}"}), 500
    
    # Fallback to final values from timers.json
    run_data = parse_run_data(run_path)
    if not run_data:
        return jsonify({"error": "Failed to parse run data"}), 500
    
    metrics = run_data.get('metrics', {})
    action_data = {
        'idle': metrics.get('Actions.IdlePercentage.mean', {}).get('current'),
        'jump': metrics.get('Actions.JumpPercentage.mean', {}).get('current'),
        'jog': metrics.get('Actions.JogPercentage.mean', {}).get('current'),
        'sprint': metrics.get('Actions.SprintPercentage.mean', {}).get('current'),
        'roll': metrics.get('Actions.RollPercentage.mean', {}).get('current'),
    }
    
    return jsonify({
        "error": "Time series data not available",
        "message": "Only final action distribution percentages are available. Run extract_tensorboard_data.py to generate time series data.",
        "final_values": action_data
    })


@app.route('/api/analysis/comparative/<run_id>')
def api_comparative(run_id):
    """API endpoint for comparative analysis (Graph 3)."""
    # Get all training runs
    all_runs = get_all_runs()
    training_runs = [r for r in all_runs if r.get('mode') == 'training']
    
    # Try to identify baseline (0% style), v1 (15% style), and current (40% style)
    # This is a heuristic - we'll need to check config files for style frequency
    baseline_runs = []
    v1_runs = []
    current_runs = []
    
    for run in training_runs:
        # Check if we can determine style frequency from config or run name
        # For now, we'll use the selected run and try to find similar ones
        run_data = parse_run_data(RESULTS_DIR / run['run_id'])
        if run_data:
            # We'll need to check CharacterConfig.cs or config files for style frequency
            # For now, return all training runs for comparison
            baseline_runs.append(run)
    
    # Get checkpoint data for comparison
    comparison_data = {
        'runs': []
    }
    
    for run in training_runs[:10]:  # Limit to 10 runs for performance
        checkpoints = run.get('checkpoints', [])
        if checkpoints:
            sorted_cps = sorted(checkpoints, key=lambda x: x['steps'])
            comparison_data['runs'].append({
                'run_id': run['run_id'],
                'steps': [cp['steps'] for cp in sorted_cps],
                'rewards': [cp.get('reward', 0) for cp in sorted_cps]
            })
    
    return jsonify(comparison_data)


@app.route('/api/analysis/policy-value-loss/<run_id>')
def api_policy_value_loss(run_id):
    """API endpoint for policy and value loss over training (Graph 7)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    # Try to read from extracted JSON file
    loss_file = run_path / "run_logs" / "losses_over_time.json"
    if loss_file.exists():
        try:
            with open(loss_file, 'r') as f:
                data = json.load(f)
                return jsonify(data)
        except Exception as e:
            return jsonify({"error": f"Failed to read losses file: {e}"}), 500
    
    # Fallback to final values from timers.json
    run_data = parse_run_data(run_path)
    if not run_data:
        return jsonify({"error": "Failed to parse run data"}), 500
    
    metrics = run_data.get('metrics', {})
    
    return jsonify({
        "error": "Time series data not available",
        "message": "Only final policy and value loss values are available. Run extract_tensorboard_data.py to generate time series data.",
        "final_values": {
            "policy_loss": metrics.get('Losses.PolicyLoss.mean', {}).get('current'),
            "value_loss": metrics.get('Losses.ValueLoss.mean', {}).get('current')
        }
    })


@app.route('/api/analysis/entropy/<run_id>')
def api_entropy(run_id):
    """API endpoint for entropy over training (Graph 8)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    # Try to read from extracted JSON file
    entropy_file = run_path / "run_logs" / "entropy_over_time.json"
    if entropy_file.exists():
        try:
            with open(entropy_file, 'r') as f:
                data = json.load(f)
                return jsonify(data)
        except Exception as e:
            return jsonify({"error": f"Failed to read entropy file: {e}"}), 500
    
    # Fallback to final value from timers.json
    run_data = parse_run_data(run_path)
    if not run_data:
        return jsonify({"error": "Failed to parse run data"}), 500
    
    metrics = run_data.get('metrics', {})
    
    return jsonify({
        "error": "Time series data not available",
        "message": "Only final entropy value is available. Run extract_tensorboard_data.py to generate time series data.",
        "final_value": metrics.get('Policy.Entropy.mean', {}).get('current')
    })


@app.route('/api/analysis/episode-length-dist/<run_id>')
def api_episode_length_dist(run_id):
    """API endpoint for episode length distribution (Graph 5)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    episode_file = run_path / "run_logs" / "episode_data.json"
    if episode_file.exists():
        try:
            with open(episode_file, 'r') as f:
                data = json.load(f)
                return jsonify(data)
        except Exception as e:
            return jsonify({"error": f"Failed to read episode data file: {e}"}), 500
    
    return jsonify({"error": "Episode data not available", "message": "Run training with TrainingLogger.cs enabled to generate episode data."})


@app.route('/api/analysis/stamina/<run_id>')
def api_stamina(run_id):
    """API endpoint for stamina management (Graph 6)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    stamina_file = run_path / "run_logs" / "stamina_trajectories.json"
    if stamina_file.exists():
        try:
            with open(stamina_file, 'r') as f:
                data = json.load(f)
                
                # Ensure data structure is correct
                if 'trajectories' not in data:
                    return jsonify({
                        "error": "Invalid data format",
                        "message": "stamina_trajectories.json missing 'trajectories' key"
                    }), 500
                
                trajectories = data.get('trajectories', [])
                if not trajectories:
                    return jsonify({
                        "error": "No trajectory data",
                        "message": "stamina_trajectories.json contains no trajectories"
                    }), 500
                
                # Validate and clean data
                valid_trajectories = []
                for traj in trajectories:
                    if 'episodeNumber' in traj and 'dataPoints' in traj:
                        data_points = traj.get('dataPoints', [])
                        if data_points:  # Only include trajectories with data
                            valid_trajectories.append(traj)
                
                if not valid_trajectories:
                    return jsonify({
                        "error": "No valid trajectory data",
                        "message": "All trajectories have empty dataPoints arrays"
                    }), 500
                
                return jsonify({
                    "trajectories": valid_trajectories
                })
        except json.JSONDecodeError as e:
            return jsonify({"error": f"Invalid JSON in stamina file: {e}"}), 500
        except Exception as e:
            return jsonify({"error": f"Failed to read stamina file: {e}"}), 500
    
    return jsonify({"error": "Stamina data not available", "message": "Run training with TrainingLogger.cs enabled to generate stamina trajectories."})


@app.route('/api/analysis/reward-breakdown/<run_id>')
def api_reward_breakdown(run_id):
    """API endpoint for reward component breakdown (Graph 10)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    reward_file = run_path / "run_logs" / "reward_components.json"
    if reward_file.exists():
        try:
            with open(reward_file, 'r') as f:
                data = json.load(f)
                return jsonify(data)
        except Exception as e:
            return jsonify({"error": f"Failed to read reward components file: {e}"}), 500
    
    return jsonify({"error": "Reward breakdown data not available", "message": "Run training with TrainingLogger.cs enabled to generate reward component data."})


@app.route('/api/analysis/roll-usage/<run_id>')
def api_roll_usage(run_id):
    """API endpoint for roll usage vs style frequency (Graph 4)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    # Try to get style frequency from metadata.json
    metadata_file = run_path / "metadata.json"
    style_frequency = None
    if metadata_file.exists():
        try:
            with open(metadata_file, 'r') as f:
                metadata = json.load(f)
                # Try both camelCase and snake_case
                style_frequency = metadata.get('styleEpisodeFrequency') or metadata.get('style_episode_frequency')
        except Exception:
            pass
    
    # Fallback: try to get from config file
    if style_frequency is None:
        config_file = run_path / "configuration.yaml"
        if config_file.exists():
            try:
                import yaml
                with open(config_file, 'r') as f:
                    config = yaml.safe_load(f)
                    # Try to find style frequency in config (might be in different places)
                    # For now, use a reasonable default if not found
                    style_frequency = 0.4  # Default fallback
            except Exception:
                pass
    
    # Get roll usage from timers.json
    run_data = parse_run_data(run_path)
    roll_usage = None
    if run_data:
        metrics = run_data.get('metrics', {})
        roll_usage = metrics.get('Actions.RollPercentage.mean', {}).get('current')
    
    # If style_frequency is None, try to infer from run name or use default
    if style_frequency is None:
        # Try to infer from run name patterns (if any)
        # Otherwise default to 0.4 (40%) which is the current config
        style_frequency = 0.4
    
    if roll_usage is not None:
        return jsonify({
            "style_frequency": style_frequency,
            "roll_usage": roll_usage,
            "run_id": run_id
        })
    
    return jsonify({
        "error": "Data not available",
        "message": "Need roll usage from timers.json",
        "style_frequency": style_frequency,
        "roll_usage": roll_usage
    })


@app.route('/api/analysis/distance-dist/<run_id>')
def api_distance_dist(run_id):
    """API endpoint for distance traveled distribution (Graph 9)."""
    run_path = RESULTS_DIR / run_id
    if not run_path.exists():
        return jsonify({"error": "Run not found"}), 404
    
    # Same file as episode length distribution
    episode_file = run_path / "run_logs" / "episode_data.json"
    if episode_file.exists():
        try:
            with open(episode_file, 'r') as f:
                data = json.load(f)
                return jsonify(data)
        except Exception as e:
            return jsonify({"error": f"Failed to read episode data file: {e}"}), 500
    
    return jsonify({"error": "Distance data not available", "message": "Run training with TrainingLogger.cs enabled to generate episode data."})


if __name__ == '__main__':
    print("Starting ML-Agents Training Dashboard...")
    print(f"Results directory: {RESULTS_DIR}")
    app.run(debug=True, port=5000, host='0.0.0.0')

