#!/usr/bin/env python3
"""
Comprehensive script to identify and clean up failed/incomplete ML-Agents runs.

Detects various failure modes:
- Missing critical files (configuration.yaml, timers.json, etc.)
- Zero or negative rewards
- No checkpoints saved
- Runs that crashed immediately (< 10 seconds)
- Incomplete runs (missing metadata)

Usage:
    python utils/check_failed_runs.py              # Scan only
    python utils/check_failed_runs.py --clean      # Scan and delete failed runs
    python utils/check_failed_runs.py --dry-run    # Show what would be deleted
"""

import json
import yaml
import shutil
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Tuple, Optional
import argparse


class RunAnalyzer:
    """Analyzes a single run directory to determine if it failed."""
    
    def __init__(self, run_path: Path):
        self.path = run_path
        self.name = run_path.name
        self.failures = []
        self.warnings = []
        self.size_bytes = 0
        
        # File paths
        self.config_path = run_path / "configuration.yaml"
        self.timers_path = run_path / "run_logs" / "timers.json"
        self.status_path = run_path / "run_logs" / "training_status.json"
        
        # Extracted data
        self.config = None
        self.timers = None
        self.status = None
        self.reward = None
        self.duration = None
        self.checkpoints = []
    
    def analyze(self) -> bool:
        """
        Analyze the run and return True if it's failed.
        Populates self.failures with reasons for failure.
        """
        # Calculate directory size
        self.size_bytes = sum(f.stat().st_size for f in self.path.rglob('*') if f.is_file())
        
        # Check 1: Critical files existence
        if not self.config_path.exists():
            self.failures.append("Missing configuration.yaml")
        
        if not self.timers_path.exists():
            self.failures.append("Missing timers.json")
        
        if not self.status_path.exists():
            self.failures.append("Missing training_status.json")
        
        # If missing critical files, mark as failed
        if self.failures:
            return True
        
        # Check 2: Parse files and validate content
        try:
            with open(self.config_path, 'r') as f:
                self.config = yaml.safe_load(f)
        except Exception as e:
            self.failures.append(f"Corrupted configuration.yaml: {e}")
        
        try:
            with open(self.timers_path, 'r') as f:
                self.timers = json.load(f)
        except Exception as e:
            self.failures.append(f"Corrupted timers.json: {e}")
        
        try:
            with open(self.status_path, 'r') as f:
                self.status = json.load(f)
        except Exception as e:
            self.failures.append(f"Corrupted training_status.json: {e}")
        
        # If files are corrupted, mark as failed
        if self.failures:
            return True
        
        # Check 3: Extract metrics
        self._extract_reward()
        self._extract_duration()
        self._extract_checkpoints()
        
        # Check 4: Validate metrics
        if self.reward is None:
            self.failures.append("No reward metric found")
        elif self.reward <= 0:
            self.failures.append(f"Zero or negative reward: {self.reward:.2f}")
        
        if self.duration is not None and self.duration < 10:
            self.failures.append(f"Run crashed immediately (duration: {self.duration:.1f}s)")
        
        if len(self.checkpoints) == 0:
            # Check if it's inference mode (inference doesn't save checkpoints)
            is_inference = self._is_inference_mode()
            if not is_inference:
                self.failures.append("No checkpoints saved (training run)")
            else:
                # For inference, just warn but don't fail
                self.warnings.append("No checkpoints (inference mode)")
        
        # Check 5: Very small directory size (< 100KB likely means nothing happened)
        if self.size_bytes < 100 * 1024:
            self.warnings.append(f"Very small size: {self._format_size(self.size_bytes)}")
        
        return len(self.failures) > 0
    
    def _is_inference_mode(self) -> bool:
        """Check if this is an inference run."""
        if self.config:
            checkpoint_settings = self.config.get('checkpoint_settings', {})
            return checkpoint_settings.get('inference', False)
        return 'inference' in self.name.lower()
    
    def _extract_reward(self):
        """Extract cumulative reward from timers.json."""
        if not self.timers:
            return
        
        gauges = self.timers.get('gauges', {})
        
        # Try multiple possible metric names
        possible_keys = [
            'ParkourRunner.Environment.CumulativeReward.mean',
            'Environment.CumulativeReward.mean',
            'ParkourRunner.Episode.TotalReward.mean',
            'Episode.TotalReward.mean',
        ]
        
        for key in possible_keys:
            if key in gauges:
                self.reward = gauges[key].get('value', 0)
                return
    
    def _extract_duration(self):
        """Extract run duration from timers.json."""
        if not self.timers:
            return
        
        metadata = self.timers.get('metadata', {})
        start_time = metadata.get('start_time_seconds', 0)
        end_time = metadata.get('end_time_seconds', 0)
        
        # Convert to float if they're strings
        try:
            if start_time and end_time:
                start_time = float(start_time)
                end_time = float(end_time)
                self.duration = end_time - start_time
        except (ValueError, TypeError):
            pass  # Invalid timestamps, skip duration
    
    def _extract_checkpoints(self):
        """Extract checkpoints from training_status.json."""
        if not self.status:
            return
        
        # Status file is a dict with behavior names as keys
        for behavior_name, behavior_data in self.status.items():
            if isinstance(behavior_data, dict):
                checkpoints = behavior_data.get('checkpoints', [])
                self.checkpoints.extend(checkpoints)
    
    def _format_size(self, bytes_size: int) -> str:
        """Format bytes to human-readable size."""
        for unit in ['B', 'KB', 'MB', 'GB']:
            if bytes_size < 1024.0:
                return f"{bytes_size:.1f}{unit}"
            bytes_size /= 1024.0
        return f"{bytes_size:.1f}TB"
    
    def get_summary(self) -> str:
        """Get a one-line summary of the run."""
        if not self.failures:
            reward_str = f"✓ {self.reward:.2f}" if self.reward else "✓ OK"
            return f"[OK] {reward_str}"
        else:
            return f"[FAIL] {', '.join(self.failures[:2])}"  # Show first 2 failures


def scan_results(results_dir: Path) -> Tuple[List[RunAnalyzer], List[RunAnalyzer]]:
    """
    Scan all runs in results directory.
    Returns: (successful_runs, failed_runs)
    """
    if not results_dir.exists():
        print(f"[ERROR] Results directory not found: {results_dir}")
        return [], []
    
    successful_runs = []
    failed_runs = []
    
    for run_dir in sorted(results_dir.iterdir()):
        if run_dir.is_dir():
            analyzer = RunAnalyzer(run_dir)
            is_failed = analyzer.analyze()
            
            if is_failed:
                failed_runs.append(analyzer)
            else:
                successful_runs.append(analyzer)
    
    return successful_runs, failed_runs


def print_report(successful_runs: List[RunAnalyzer], failed_runs: List[RunAnalyzer]):
    """Print a detailed report of all runs."""
    total_wasted_bytes = sum(run.size_bytes for run in failed_runs)
    # Create a temporary analyzer just for formatting
    temp_analyzer = RunAnalyzer(Path('.'))
    wasted_size = temp_analyzer._format_size(total_wasted_bytes)
    
    print("=" * 100)
    print("ML-AGENTS RUN ANALYSIS REPORT")
    print("=" * 100)
    print(f"Total runs: {len(successful_runs) + len(failed_runs)}")
    print(f"[OK] Successful: {len(successful_runs)}")
    print(f"[FAIL] Failed: {len(failed_runs)}")
    print(f"Disk space wasted: {wasted_size}")
    print()
    
    # Print successful runs (brief)
    if successful_runs:
        print("=" * 100)
        print("SUCCESSFUL RUNS")
        print("=" * 100)
        for run in successful_runs[:10]:  # Show first 10
            reward_str = f"{run.reward:.2f}" if run.reward else "N/A"
            checkpoints_str = f"{len(run.checkpoints)} checkpoints" if run.checkpoints else "inference"
            print(f"[OK] {run.name:50s} Reward: {reward_str:>10s}  {checkpoints_str}")
        
        if len(successful_runs) > 10:
            print(f"... and {len(successful_runs) - 10} more successful runs")
        print()
    
    # Print failed runs (detailed)
    if failed_runs:
        print("=" * 100)
        print("FAILED RUNS (Detailed)")
        print("=" * 100)
        
        # Group by failure type
        by_type = {}
        for run in failed_runs:
            failure_key = run.failures[0] if run.failures else "Unknown"
            if failure_key not in by_type:
                by_type[failure_key] = []
            by_type[failure_key].append(run)
        
        for failure_type, runs in sorted(by_type.items()):
            print(f"\n** {failure_type} ({len(runs)} runs):")
            for run in runs:
                size_str = run._format_size(run.size_bytes)
                print(f"   [FAIL] {run.name:50s} Size: {size_str:>10s}")
                for failure in run.failures[1:]:  # Show additional failures
                    print(f"      - {failure}")
                for warning in run.warnings:
                    print(f"      [WARN] {warning}")
        print()


def generate_cleanup_commands(failed_runs: List[RunAnalyzer], results_dir: Path):
    """Generate shell commands to delete failed runs."""
    if not failed_runs:
        return
    
    print("=" * 100)
    print("CLEANUP COMMANDS")
    print("=" * 100)
    print()
    
    # Windows PowerShell
    print("# Windows (PowerShell):")
    print('$failedRuns = @(')
    for run in failed_runs:
        print(f'    "{run.name}",')
    print(')')
    print(f'foreach ($run in $failedRuns) {{ Remove-Item -Recurse -Force "{results_dir}\\$run" }}')
    print()
    
    # Linux/Mac Bash
    print("# Linux/Mac (Bash):")
    for run in failed_runs:
        print(f'rm -rf "{results_dir}/{run.name}"')
    print()
    
    # One-liner for bash
    print("# Or delete all at once (Bash):")
    run_names = '" "'.join(run.name for run in failed_runs)
    print(f'cd {results_dir} && rm -rf "{run_names}"')
    print()


def delete_failed_runs(failed_runs: List[RunAnalyzer], dry_run: bool = False):
    """Delete failed run directories."""
    if not failed_runs:
        print("[OK] No failed runs to delete!")
        return
    
    print()
    print("=" * 100)
    if dry_run:
        print("DRY RUN - Would delete the following runs:")
    else:
        print("DELETING FAILED RUNS")
    print("=" * 100)
    
    deleted_count = 0
    total_freed = 0
    
    for run in failed_runs:
        size_str = run._format_size(run.size_bytes)
        
        if dry_run:
            print(f"   Would delete: {run.name:50s} ({size_str})")
        else:
            try:
                shutil.rmtree(run.path)
                print(f"   [OK] Deleted: {run.name:50s} ({size_str})")
                deleted_count += 1
                total_freed += run.size_bytes
            except Exception as e:
                print(f"   [FAIL] Failed to delete {run.name}: {e}")
    
    if not dry_run and deleted_count > 0:
        print()
        print(f"[OK] Successfully deleted {deleted_count} failed runs")
        print(f"Freed {RunAnalyzer(Path('.'))._format_size(total_freed)} of disk space")


def main():
    parser = argparse.ArgumentParser(
        description="Check for failed ML-Agents training/inference runs and optionally clean them up.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python utils/check_failed_runs.py              # Scan and report only
  python utils/check_failed_runs.py --clean      # Scan and delete failed runs
  python utils/check_failed_runs.py --dry-run    # Show what would be deleted
        """
    )
    parser.add_argument('--clean', action='store_true',
                        help='Delete failed runs after scanning')
    parser.add_argument('--dry-run', action='store_true',
                        help='Show what would be deleted without actually deleting')
    parser.add_argument('--yes', '-y', action='store_true',
                        help='Skip confirmation prompt (auto-confirm deletion)')
    parser.add_argument('--results-dir', type=str, default='src/results',
                        help='Path to results directory (default: src/results)')
    
    args = parser.parse_args()
    
    results_dir = Path(args.results_dir)
    
    print("Scanning results directory...")
    print()
    
    successful_runs, failed_runs = scan_results(results_dir)
    
    print_report(successful_runs, failed_runs)
    
    if failed_runs:
        generate_cleanup_commands(failed_runs, results_dir)
        
        if args.clean or args.dry_run:
            if args.clean and not args.dry_run and not args.yes:
                response = input(f"\n[WARNING] Delete {len(failed_runs)} failed runs? [y/N]: ")
                if response.lower() != 'y':
                    print("[CANCEL] Deletion cancelled")
                    return
            
            delete_failed_runs(failed_runs, dry_run=args.dry_run)
        else:
            print("Tip: Run with --clean to delete failed runs, or --dry-run to preview")
    else:
        print("[OK] No failed runs found! All runs are healthy.")


if __name__ == "__main__":
    main()

