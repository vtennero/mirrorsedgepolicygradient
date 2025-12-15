#!/usr/bin/env python3
"""
Extract scalar data from TensorBoard event files and save to JSON.
Extracts:
- Action distribution over time (Graph 2)
- Policy/Value loss over training (Graph 7)
- Entropy over training (Graph 8)

Usage:
    python extract_tensorboard_data.py [training_dir]
    
If training_dir is not provided, uses the most recent training_* directory.
"""

import sys
import json
from pathlib import Path
from typing import Dict, List, Optional
import argparse

try:
    import yaml
except ImportError:
    yaml = None

try:
    from tensorboard.backend.event_processing.event_accumulator import EventAccumulator
except ImportError:
    print("ERROR: tensorboard package not found.")
    print("Install with: pip install tensorboard")
    sys.exit(1)


def find_tensorboard_logs(training_dir: Path) -> Optional[Path]:
    """
    Find TensorBoard event files in the training directory.
    ML-Agents stores them in a subdirectory named after the behavior.
    """
    # First check subdirectories (behavior-specific folders)
    for subdir in training_dir.iterdir():
        if subdir.is_dir():
            event_files = list(subdir.glob("events.out.tfevents.*"))
            if event_files:
                return subdir
    
    # Fallback: check training_dir directly
    event_files = list(training_dir.glob("events.out.tfevents.*"))
    if event_files:
        return training_dir
    
    return None


def extract_scalars(log_dir: Path) -> Dict[str, List[Dict]]:
    """
    Extract all scalar data from TensorBoard event files.
    Returns a dictionary mapping tag names to lists of (step, value) pairs.
    """
    # Create EventAccumulator
    ea = EventAccumulator(str(log_dir))
    ea.Reload()
    
    # Get all scalar tags
    scalar_tags = ea.Tags().get('scalars', [])
    
    if not scalar_tags:
        print(f"Warning: No scalar data found in {log_dir}")
        return {}
    
    print(f"Found {len(scalar_tags)} scalar tags")
    
    # Extract data for each tag
    extracted_data = {}
    
    for tag in scalar_tags:
        scalar_events = ea.Scalars(tag)
        
        # Convert to list of dicts with step and value
        data_points = [
            {
                "step": int(event.step),
                "value": float(event.value),
                "wall_time": float(event.wall_time)
            }
            for event in scalar_events
        ]
        
        extracted_data[tag] = data_points
        print(f"  - {tag}: {len(data_points)} data points")
    
    return extracted_data


def filter_action_distribution(scalars: Dict[str, List[Dict]]) -> List[Dict]:
    """
    Extract action distribution percentages over time.
    Tags: Actions/JumpPercentage, Actions/JogPercentage, Actions/SprintPercentage, 
          Actions/RollPercentage, Actions/IdlePercentage
    """
    action_tags = [
        "Actions/JumpPercentage",
        "Actions/JogPercentage", 
        "Actions/SprintPercentage",
        "Actions/RollPercentage",
        "Actions/IdlePercentage"
    ]
    
    # Find all steps that have at least one action percentage
    all_steps = set()
    for tag in action_tags:
        if tag in scalars:
            for point in scalars[tag]:
                all_steps.add(point["step"])
    
    # Build combined data points
    result = []
    for step in sorted(all_steps):
        point = {"step": step}
        
        for tag in action_tags:
            # Extract the action name from tag (e.g., "Actions/JumpPercentage" -> "jump")
            action_name = tag.replace("Actions/", "").replace("Percentage", "").lower()
            
            # Find value for this step
            value = None
            if tag in scalars:
                for data_point in scalars[tag]:
                    if data_point["step"] == step:
                        value = data_point["value"]
                        break
            
            point[action_name] = value
        
        result.append(point)
    
    return result


def filter_losses(scalars: Dict[str, List[Dict]]) -> List[Dict]:
    """
    Extract policy and value loss over time.
    Tries multiple possible tag name variations used by ML-Agents.
    """
    # Try multiple possible tag name variations
    policy_loss_tags = [
        "Policy/Loss",
        "Losses/PolicyLoss",
        "Losses/Policy Loss",
        "Policy Loss",
        "PolicyLoss"
    ]
    value_loss_tags = [
        "Value/Loss",
        "Losses/ValueLoss",
        "Losses/Value Loss",
        "Value Loss",
        "ValueLoss"
    ]
    
    # Find which tags actually exist
    policy_tag = None
    value_tag = None
    
    for tag in policy_loss_tags:
        if tag in scalars:
            policy_tag = tag
            break
    
    for tag in value_loss_tags:
        if tag in scalars:
            value_tag = tag
            break
    
    # If not found, try to find any tag containing "policy" and "loss" (case insensitive)
    if not policy_tag:
        for tag in scalars.keys():
            if "policy" in tag.lower() and "loss" in tag.lower():
                policy_tag = tag
                print(f"  Found policy loss tag: {tag}")
                break
    
    if not value_tag:
        for tag in scalars.keys():
            if "value" in tag.lower() and "loss" in tag.lower():
                value_tag = tag
                print(f"  Found value loss tag: {tag}")
                break
    
    if not policy_tag and not value_tag:
        print("  Warning: No loss tags found. Available tags:")
        for tag in sorted(scalars.keys()):
            if "loss" in tag.lower():
                print(f"    - {tag}")
        return []
    
    # Find all steps that have at least one loss
    all_steps = set()
    if policy_tag:
        for point in scalars[policy_tag]:
            all_steps.add(point["step"])
    if value_tag:
        for point in scalars[value_tag]:
            all_steps.add(point["step"])
    
    # Build combined data points
    result = []
    for step in sorted(all_steps):
        point = {"step": step}
        
        # Get policy loss
        policy_loss = None
        if policy_tag:
            for data_point in scalars[policy_tag]:
                if data_point["step"] == step:
                    policy_loss = data_point["value"]
                    break
        point["policy_loss"] = policy_loss
        
        # Get value loss
        value_loss = None
        if value_tag:
            for data_point in scalars[value_tag]:
                if data_point["step"] == step:
                    value_loss = data_point["value"]
                    break
        point["value_loss"] = value_loss
        
        result.append(point)
    
    return result


def filter_entropy(scalars: Dict[str, List[Dict]]) -> List[Dict]:
    """
    Extract entropy over time.
    Tag: Policy/Entropy
    """
    entropy_tag = "Policy/Entropy"
    
    if entropy_tag not in scalars:
        return []
    
    return [
        {
            "step": point["step"],
            "entropy": point["value"]
        }
        for point in scalars[entropy_tag]
    ]


def filter_episode_data(scalars: Dict[str, List[Dict]]) -> List[Dict]:
    """
    Extract episode data (length, max distance, success) from TensorBoard.
    Tags: Episode/Length, Episode/MaxDistance, Episode/TotalReward
    Creates episode_data.json format compatible with TrainingLogger output.
    """
    length_tag = "Episode/Length"
    max_distance_tag = "Episode/MaxDistance"
    reward_tag = "Episode/TotalReward"
    
    # Check which tags exist
    has_length = length_tag in scalars
    has_max_distance = max_distance_tag in scalars
    has_reward = reward_tag in scalars
    
    if not (has_length or has_max_distance):
        return []
    
    # Collect all steps that have at least one metric
    all_steps = set()
    if has_length:
        for point in scalars[length_tag]:
            all_steps.add(point["step"])
    if has_max_distance:
        for point in scalars[max_distance_tag]:
            all_steps.add(point["step"])
    
    # Build episode data (each step represents an episode checkpoint)
    episodes = []
    episode_number = 1
    
    for step in sorted(all_steps):
        length = None
        max_distance = None
        total_reward = None
        
        if has_length:
            for point in scalars[length_tag]:
                if point["step"] == step:
                    length = point["value"]
                    break
        
        if has_max_distance:
            for point in scalars[max_distance_tag]:
                if point["step"] == step:
                    max_distance = point["value"]
                    break
        
        if has_reward:
            for point in scalars[reward_tag]:
                if point["step"] == step:
                    total_reward = point["value"]
                    break
        
        # Only add if we have meaningful data
        if length is not None or max_distance is not None:
            # Estimate success: positive reward suggests success
            success = total_reward > 0 if total_reward is not None else None
            
            episodes.append({
                "episodeNumber": episode_number,
                "stepCount": step,
                "length": length if length is not None else 0.0,
                "maxDistance": max_distance if max_distance is not None else 0.0,
                "success": success if success is not None else True  # Default to True if unknown
            })
            episode_number += 1
    
    return episodes


def main():
    parser = argparse.ArgumentParser(
        description="Extract TensorBoard scalar data to JSON files"
    )
    parser.add_argument(
        "training_dir",
        nargs="?",
        type=str,
        help="Path to training directory (default: most recent training_* directory)"
    )
    parser.add_argument(
        "--results-dir",
        type=str,
        default="results",
        help="Results directory (default: results)"
    )
    
    args = parser.parse_args()
    
    # Determine training directory
    script_dir = Path(__file__).parent
    results_dir = script_dir / args.results_dir
    
    if args.training_dir:
        training_dir = Path(args.training_dir)
        if not training_dir.is_absolute():
            training_dir = results_dir / args.training_dir
    else:
        # Find most recent training_* directory
        training_dirs = sorted(
            results_dir.glob("training_*"),
            key=lambda p: p.stat().st_mtime,
            reverse=True
        )
        
        if not training_dirs:
            print(f"ERROR: No training_* directories found in {results_dir}")
            sys.exit(1)
        
        training_dir = training_dirs[0]
        print(f"Using most recent training directory: {training_dir.name}")
    
    if not training_dir.exists():
        print(f"ERROR: Training directory not found: {training_dir}")
        sys.exit(1)
    
    # Find TensorBoard logs
    log_dir = find_tensorboard_logs(training_dir)
    if not log_dir:
        print(f"ERROR: No TensorBoard event files found in {training_dir}")
        print("Make sure training has started and TensorBoard logs have been created.")
        sys.exit(1)
    
    print(f"Found TensorBoard logs in: {log_dir}")
    
    # Extract all scalars
    print("\nExtracting scalar data...")
    scalars = extract_scalars(log_dir)
    
    if not scalars:
        print("ERROR: No scalar data found")
        sys.exit(1)
    
    # Filter and save specific data
    run_logs_dir = training_dir / "run_logs"
    run_logs_dir.mkdir(exist_ok=True)
    
    print("\nSaving extracted data...")
    
    # Graph 2: Action distribution over time
    action_dist = filter_action_distribution(scalars)
    if action_dist:
        action_file = run_logs_dir / "action_distribution_over_time.json"
        with open(action_file, 'w') as f:
            json.dump({"data": action_dist}, f, indent=2)
        print(f"  ✓ Saved action distribution: {len(action_dist)} data points -> {action_file}")
    else:
        print("  ⚠ No action distribution data found")
    
    # Graph 7: Policy/Value loss over training
    losses = filter_losses(scalars)
    if losses:
        loss_file = run_logs_dir / "losses_over_time.json"
        with open(loss_file, 'w') as f:
            json.dump({"data": losses}, f, indent=2)
        print(f"  ✓ Saved losses: {len(losses)} data points -> {loss_file}")
    else:
        print("  ⚠ No loss data found")
    
    # Graph 8: Entropy over training
    entropy = filter_entropy(scalars)
    if entropy:
        entropy_file = run_logs_dir / "entropy_over_time.json"
        with open(entropy_file, 'w') as f:
            json.dump({"data": entropy}, f, indent=2)
        print(f"  ✓ Saved entropy: {len(entropy)} data points -> {entropy_file}")
    else:
        print("  ⚠ No entropy data found")
    
    # Graph 5 & 9: Episode data (length, max distance) - fallback from TensorBoard
    episode_data = filter_episode_data(scalars)
    if episode_data:
        episode_file = run_logs_dir / "episode_data.json"
        # Check if TrainingLogger already created this file
        if episode_file.exists():
            try:
                # Merge with existing data
                with open(episode_file, 'r') as f:
                    existing = json.load(f)
                    existing_episodes = existing.get("episodes", [])
                    # Only add if we have more data or different data
                    if len(episode_data) > len(existing_episodes):
                        print(f"  ⚠ episode_data.json already exists with {len(existing_episodes)} episodes")
                        print(f"     TensorBoard has {len(episode_data)} episodes - keeping existing file")
                    else:
                        # Use TensorBoard data as fallback
                        with open(episode_file, 'w') as f:
                            json.dump({"episodes": episode_data}, f, indent=2)
                        print(f"  ✓ Saved episode data (from TensorBoard): {len(episode_data)} episodes -> {episode_file}")
            except Exception as e:
                print(f"  ⚠ Could not read existing episode_data.json: {e}")
                # Write TensorBoard data
                with open(episode_file, 'w') as f:
                    json.dump({"episodes": episode_data}, f, indent=2)
                print(f"  ✓ Saved episode data (from TensorBoard): {len(episode_data)} episodes -> {episode_file}")
        else:
            # No existing file, write TensorBoard data
            with open(episode_file, 'w') as f:
                json.dump({"episodes": episode_data}, f, indent=2)
            print(f"  ✓ Saved episode data (from TensorBoard): {len(episode_data)} episodes -> {episode_file}")
    else:
        print("  ⚠ No episode data found in TensorBoard")
    
    # Create metadata.json if it doesn't exist (for style frequency)
    metadata_file = training_dir / "metadata.json"
    if not metadata_file.exists():
        try:
            # Try to extract style frequency from config
            config_file = training_dir / "configuration.yaml"
            style_frequency = None
            if config_file.exists() and yaml:
                try:
                    with open(config_file, 'r') as f:
                        config = yaml.safe_load(f)
                        # Style frequency might be in CharacterConfig or as a parameter
                        # For now, we'll set a default or try to find it
                        # This is a fallback - TrainingLogger should create this
                        style_frequency = 0.4  # Default, should be overridden by TrainingLogger
                except Exception:
                    style_frequency = 0.4  # Default fallback
            
            if style_frequency is not None:
                metadata = {
                    "styleEpisodeFrequency": style_frequency,
                    "trainingStartTime": training_dir.stat().st_mtime,
                    "source": "extracted_from_tensorboard"
                }
                with open(metadata_file, 'w') as f:
                    json.dump(metadata, f, indent=2)
                print(f"  ✓ Created metadata.json (fallback, style frequency: {style_frequency})")
        except Exception as e:
            print(f"  ⚠ Could not create metadata.json: {e}")
    
    print("\n✓ Extraction complete!")


if __name__ == "__main__":
    main()

