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

# Path to results directory
RESULTS_DIR = Path(__file__).parent.parent / "src" / "results"


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


if __name__ == '__main__':
    print("Starting ML-Agents Training Dashboard...")
    print(f"Results directory: {RESULTS_DIR}")
    app.run(debug=True, port=5000, host='0.0.0.0')

