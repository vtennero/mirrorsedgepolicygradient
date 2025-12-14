# ML-Agents Training Dashboard

A web-based dashboard for visualizing and comparing ML-Agents training runs.

## Features

- **Run Overview**: View all training/inference runs with key metrics at a glance
- **Expandable Details**: Click any run to see full configuration, metrics, and checkpoints
- **Comparison Charts**: Visual comparison of rewards, episode lengths, steps, and training time
- **Filtering & Sorting**: Filter by training/inference mode and sort by various metrics
- **Auto-refresh**: Dashboard updates every 30 seconds automatically
- **Timestamps**: Full date/time information for each run

## Quick Start

### 1. Install Dependencies

```bash
cd utils/dashboard
pip install -r requirements.txt
```

### 2. Run the Server

**Option A: Dashboard + TensorBoard (Recommended)**
```bash
# Linux/Mac
chmod +x start_all.sh
./start_all.sh

# Windows
start_all.bat
```

This starts both:
- **Dashboard**: http://localhost:5000
- **TensorBoard**: http://localhost:6006

**Option B: Dashboard Only**
```bash
python app.py
```

The dashboard will be available at: **http://localhost:5000**

## What the Dashboard Shows

### Key Metrics (Main View)
- **Run ID**: Name of the training run
- **Date & Time**: When the run started/ended and duration
- **Mode**: Training or Inference
- **Average Reward**: Mean cumulative reward across episodes
- **Episode Length**: Average episode duration
- **Total Steps**: Total training/inference steps

### Expandable Details (Click to Expand)
Each run card can be expanded to show:

1. **Detailed Metrics**
   - Cumulative reward (current, min, max)
   - Episode length statistics
   - Policy entropy
   - Value estimates

2. **Configuration**
   - Learning rate, batch size, buffer size
   - Network architecture (hidden units, layers)
   - Time horizon, gamma
   - Max steps

3. **Checkpoints**
   - All saved checkpoints with steps, rewards, and timestamps

4. **Full Data**
   - Complete configuration YAML
   - All metrics from timers.json

### Comparison Tab
- **Bar Charts**: Visual comparison across all runs
  - Cumulative rewards
  - Episode lengths
  - Total steps
  - Training duration
- **Comparison Table**: Side-by-side metrics for all runs

## Data Sources

The dashboard automatically parses:
- `configuration.yaml`: Training hyperparameters and settings
- `run_logs/timers.json`: Performance metrics and timing data
- `run_logs/training_status.json`: Checkpoints and training status

## Tips for Regular Use

- Leave the dashboard open - it auto-refreshes every 30 seconds
- Use filters to focus on training vs inference runs
- Sort by reward to quickly identify best-performing runs
- Expand runs to compare configurations side-by-side
- Check the Comparison tab to see trends across runs

## API Endpoints

- `GET /`: Main dashboard interface
- `GET /api/runs`: JSON data for all runs
- `GET /api/run/<run_id>`: Detailed data for specific run
- `GET /api/compare`: Comparison metrics across runs

