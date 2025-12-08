# ML-Agents Training Dashboard - Quick Guide

## 🚀 Dashboard is Live!

Access it at:
http://localhost:5000
tensorboard --logdir=results
http://localhost:6006/

The dashboard is currently running in the background and will auto-refresh every 30 seconds.

## 📊 What You'll See

### Main Overview (Top Stats)
- **Total Runs**: Number of training/inference runs
- **Best Reward**: Highest cumulative reward achieved
- **Latest Run**: Most recent run with timestamp
- **Total Training Steps**: Cumulative steps across all runs

### Run Cards (Expandable)

Each run displays:

#### **Quick View (Always Visible)**
- Run ID and mode (Training/Inference badge)
- Date and duration
- **3 Key Metrics** for fast comparison:
  - Average Reward
  - Episode Length
  - Total Steps

#### **Expanded View (Click to Open)**
Click any run card to reveal:

1. **Detailed Metrics**
   - Cumulative reward (current, min, max)
   - Episode length statistics
   - Policy entropy
   - Value estimates

2. **Configuration**
   - Learning rate, batch size, buffer size
   - Network architecture (hidden units, layers)
   - Time horizon, gamma, max steps

3. **Checkpoints**
   - All saved checkpoints with steps, rewards, and creation times

4. **Full Data** (Toggle buttons)
   - Complete configuration YAML
   - All metrics from timers.json

### Comparison Tab

Switch to the "Comparison" tab to see:

- **4 Bar Charts** comparing all runs:
  - Cumulative Reward
  - Episode Length
  - Total Steps
  - Training Duration

- **Comparison Table**: Side-by-side metrics for all runs

## 🎯 Optimized for Your Workflow

Since you're running training every ~20 minutes for 3 days:

### **Quick Glance** (5 seconds)
1. Check top-right metrics on each run card
2. Identify best performing runs by reward
3. See timestamps to track recent runs

### **Deep Dive** (When Needed)
1. Click to expand any run
2. Compare configurations between good/bad runs
3. Check checkpoint progression
4. Toggle full data for detailed analysis

## 🔍 Filtering & Sorting

**Filter Buttons:**
- All / Training / Inference

**Sort Dropdown:**
- Date (Newest/Oldest)
- Reward (Highest/Lowest)
- Steps (Most first)

## 📁 Data Sources

The dashboard automatically parses from `src/results/`:
- `configuration.yaml` - Training hyperparameters
- `run_logs/timers.json` - Performance metrics, timestamps
- `run_logs/training_status.json` - Checkpoints

## 💡 Pro Tips

1. **Leave it open** - Auto-refreshes every 30 seconds
2. **Use filters** - Quickly separate training vs inference runs
3. **Sort by reward** - Find best runs instantly
4. **Expand multiple** - Compare configurations side-by-side
5. **Check dates** - Track improvement over time

## 🛠️ Commands

**Start the dashboard:**
```bash
cd dashboard
python app.py
```

Or use the quick start scripts:
- Windows: `dashboard/start.bat`
- Linux/Mac: `dashboard/start.sh`

**Stop the dashboard:**
Press `Ctrl+C` in the terminal

## 📊 Key Metrics Explained

- **Cumulative Reward (Mean)**: Average total reward per episode - **your main success metric**
- **Episode Length**: How long episodes last (shorter = faster learning or failure)
- **Total Steps**: Training progress indicator
- **Entropy**: Policy randomness (higher = more exploration)

## 🎨 Visual Design

- **Training runs**: Blue badge
- **Inference runs**: Green badge
- **Best metrics**: Highlighted in charts
- **Expandable sections**: Clean, organized details on demand

---

## Current Status

✅ Dashboard running at http://localhost:5000
✅ Auto-parsing all runs in `src/results/`
✅ Auto-refresh every 30 seconds
✅ All your current runs loaded (test_v1, test_v3, test_v4, test_v5, test_v5_demo, test_v6, etc.)

