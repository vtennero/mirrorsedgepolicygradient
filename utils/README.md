# Utilities

Helper scripts and tools for ML-Agents training workflow.

## 📁 Contents

### 🔍 `check_failed_runs.py`
**Purpose**: Identify and clean up failed/incomplete ML-Agents training and inference runs.

Many training/inference runs fail or get interrupted, leaving behind empty directories that waste disk space. This script helps you find and remove them.

**Features**:
- Detects multiple failure types:
  - Missing critical files (configuration.yaml, timers.json, etc.)
  - Zero or negative rewards
  - No checkpoints saved (for training runs)
  - Immediate crashes (< 10 seconds duration)
  - Corrupted JSON/YAML files
- Calculates wasted disk space
- Groups failures by type for easy review
- Generates cleanup commands for multiple platforms
- Safe deletion with confirmation prompt

**Usage**:
```bash
# Scan only (no deletion)
python utils/check_failed_runs.py

# Preview what would be deleted
python utils/check_failed_runs.py --dry-run

# Delete failed runs (with confirmation)
python utils/check_failed_runs.py --clean

# Custom results directory
python utils/check_failed_runs.py --results-dir path/to/results
```

**Example Output**:
```
Scanning results directory...

====================================================================================================
ML-AGENTS RUN ANALYSIS REPORT
====================================================================================================
Total runs: 89
[OK] Successful: 29
[FAIL] Failed: 60
Disk space wasted: 4.3MB

====================================================================================================
FAILED RUNS (Detailed)
====================================================================================================

** Missing configuration.yaml (4 runs):
   [FAIL] training_20251207_171425                           Size:      1.1KB
      - Missing timers.json
      - Missing training_status.json

** No reward metric found (56 runs):
   [FAIL] inference_v21_2025-12-06_13-27-17                  Size:     10.8KB
      [WARN] No checkpoints (inference mode)
      [WARN] Very small size: 10.8KB
   ...
```

**What Gets Flagged as Failed**:
- Missing essential files (config, timers, status)
- Zero rewards (runs that never started properly)
- Missing checkpoints (training runs that crashed before saving)
- Very short duration (< 10 seconds = immediate crash)
- Note: Inference runs without checkpoints are NOT marked as failed (expected behavior)

---

### 📊 `dashboard/`
**Purpose**: Web-based dashboard for visualizing and comparing training runs.

Interactive web UI that shows all your training runs with metrics, configurations, and comparison charts.

**Features**:
- Run overview with key metrics (reward, episode length, steps, duration)
- Expandable details for each run (full config, checkpoints, all metrics)
- Comparison charts across all runs
- Filtering by training/inference mode
- Auto-refresh every 30 seconds
- Mobile-responsive design

**Quick Start**:
```bash
cd utils/dashboard

# Start both Dashboard + TensorBoard
./start_all.sh       # Linux/Mac
start_all.bat        # Windows

# Or just Dashboard
python app.py
```

Then visit:
- **Dashboard**: http://localhost:5000
- **TensorBoard**: http://localhost:6006

See [dashboard/README.md](dashboard/README.md) for detailed documentation.

---

### 📊 `generate_latex_figures.py`
**Purpose**: Generate LaTeX-ready PDF figures from training run data for inclusion in reports.

Converts training data (JSON files) into publication-quality PDF figures suitable for LaTeX documents. Generates all 10 analysis graphs matching the dashboard visualizations.

**Features**:
- Generates 10 PDF figures from training run data
- Automatic data downsampling for large datasets
- Includes training run ID in figure titles
- Academic-friendly styling and formatting
- Exports to `report/[trainingXX...]_figures/` directory

**Usage**:
```bash
# Generate figures for a specific training run
python utils/generate_latex_figures.py training_20251214_155306
```

**Output**:
- Creates directory: `report/training_20251214_155306_figures/`
- Generates 10 PDF files:
  - `training_curve.pdf` - Cumulative reward over training steps
  - `action_distribution.pdf` - Action percentages over time
  - `comparative_analysis.pdf` - Comparison across multiple runs
  - `roll_usage.pdf` - Roll usage vs style frequency
  - `episode_length_dist.pdf` - Episode length histogram
  - `stamina.pdf` - Stamina management over episodes
  - `loss.pdf` - Policy and value loss over training
  - `entropy.pdf` - Policy entropy over training
  - `distance.pdf` - Distance traveled distribution
  - `reward_breakdown.pdf` - Reward component breakdown

**For Overleaf**:
1. Run the script to generate PDFs
2. Upload PDF files to your Overleaf project
3. Include in LaTeX with: `\includegraphics[width=0.8\textwidth]{filename.pdf}`

**Requirements**:
- `matplotlib` (for PDF generation)
- `numpy` (for data processing)
- Training run must have completed and generated JSON data files

**Note**: Some figures require specific data files:
- Action distribution, loss, entropy: Requires running `extract_tensorboard_data.py` first
- Episode length, distance, stamina, reward breakdown: Requires `TrainingLogger.cs` enabled during training

---

## 🔧 Common Workflows

### Clean Up Failed Runs

After training sessions, you'll often have failed runs taking up space:

```bash
# 1. Check what failed
python utils/check_failed_runs.py

# 2. Preview deletion (see what would be deleted without actually deleting)
python utils/check_failed_runs.py --dry-run

# 3. Delete with confirmation
python utils/check_failed_runs.py --clean
```

**When to Use**:
- After a training session with crashes
- Before backing up results
- When disk space is low
- To clean up test/debug runs

### Monitor Training Progress

Start the dashboard before or during training:

```bash
# Start the dashboard
cd utils/dashboard
python app.py

# In browser: http://localhost:5000
```

**Features**:
- Compare different training runs
- Track reward progression
- Identify best-performing configurations
- Export run data for analysis

---

## 📝 Usage Notes

### Running from Project Root
All utilities expect to be run from the **project root** directory:

```bash
# Correct (from project root)
python utils/check_failed_runs.py

# Incorrect (from utils folder)
cd utils
python check_failed_runs.py  # This won't work!
```

### Failure Detection Criteria

Runs are marked as failed when:
1. **Missing files**: No configuration.yaml, timers.json, or training_status.json
2. **Zero rewards**: Cumulative reward is 0 or negative (never started properly)
3. **No checkpoints**: Training runs without any saved checkpoints (crashed early)
4. **Immediate crash**: Duration < 10 seconds
5. **Corrupted files**: JSON/YAML parsing errors

**Special case**: Inference runs without checkpoints are NOT marked as failed (this is expected behavior).

### Disk Space Management

Failed runs typically take 10KB - 2MB each. With many failed attempts during debugging, this can add up:
- 50 failed runs ≈ 5-10 MB
- 100 failed runs ≈ 10-20 MB
- 200 failed runs ≈ 20-50 MB

Regular cleanup keeps your results directory manageable.

---

## 🛠️ Troubleshooting

### "Results directory not found"
Make sure you're running from the project root and `src/results/` exists.

### Dashboard shows no runs
- Check that `src/results/` has run directories
- Restart the dashboard after new training runs
- Check browser console for errors

### Failed runs not detected
- Verify the run has a `run_logs/` folder
- Check that timers.json and training_status.json exist
- Some runs may legitimately have zero reward (just starting training)

---

## 📚 Related Documentation

- [Dashboard README](dashboard/README.md) - Detailed dashboard documentation
- [Main README](../README.md) - Project overview and setup
- [Training Guide](../TRAINING_LOG.md) - Training experiments and results


