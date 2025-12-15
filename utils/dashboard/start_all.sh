#!/bin/bash
# Start both Dashboard and TensorBoard for ML-Agents training monitoring

echo "============================================"
echo "ML-Agents Training Monitoring"
echo "============================================"
echo ""

# Activate conda environment
if command -v conda &> /dev/null; then
    echo "Activating conda environment: mlagents"
    # Initialize conda for bash shell
    eval "$(conda shell.bash hook)"
    conda activate mlagents
    if [ $? -ne 0 ]; then
        echo "[WARNING] Failed to activate mlagents environment"
        echo "Continuing with current environment..."
    fi
    echo ""
else
    echo "[WARNING] conda command not found"
    echo "Continuing without conda activation..."
    echo ""
fi

# Check if Python is available
if ! command -v python3 &> /dev/null && ! command -v python &> /dev/null; then
    echo "[ERROR] Python is not installed or not in PATH"
    echo "Please install Python 3.10+ and try again"
    exit 1
fi

# Use python3 if available, otherwise python
PYTHON_CMD="python3"
if ! command -v python3 &> /dev/null; then
    PYTHON_CMD="python"
fi

# Check if Flask is installed
if ! $PYTHON_CMD -c "import flask" &> /dev/null; then
    echo "[WARNING] Flask is not installed"
    echo "Installing dashboard dependencies..."
    pip3 install -r requirements.txt || pip install -r requirements.txt
    if [ $? -ne 0 ]; then
        echo "[ERROR] Failed to install dependencies"
        exit 1
    fi
fi

# Check if TensorBoard is installed
if ! $PYTHON_CMD -c "import tensorboard" &> /dev/null; then
    echo "[WARNING] TensorBoard is not installed"
    echo "TensorBoard should be included in your mlagents environment"
    echo "Install with: pip install tensorboard"
    exit 1
fi

# Get the project root (2 levels up from utils/dashboard)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

echo "Starting services..."
echo ""

# Start Dashboard in background
echo "[1/2] Starting Dashboard on port 5000..."
cd "$SCRIPT_DIR"
$PYTHON_CMD app.py > dashboard.log 2>&1 &
DASHBOARD_PID=$!

# Wait a moment for dashboard to start
sleep 2

# Start TensorBoard in background
echo "[2/2] Starting TensorBoard on port 6006..."
cd "$PROJECT_ROOT/src"
tensorboard --logdir results/ --port 6006 > "$SCRIPT_DIR/tensorboard.log" 2>&1 &
TENSORBOARD_PID=$!

# Wait a moment for tensorboard to start
sleep 2

echo ""
echo "============================================"
echo "Services Started Successfully!"
echo "============================================"
echo ""
echo "📊 Dashboard:    http://localhost:5000"
echo "   - View training runs"
echo "   - Compare metrics"
echo "   - See configurations"
echo ""
echo "📈 TensorBoard:  http://localhost:6006"
echo "   - Real-time training graphs"
echo "   - Reward curves"
echo "   - Loss metrics"
echo ""

# Function to cleanup on exit
cleanup() {
    echo ""
    echo "Shutting down services..."
    kill $DASHBOARD_PID 2>/dev/null
    kill $TENSORBOARD_PID 2>/dev/null
    echo "Services stopped."
    exit 0
}

# Trap Ctrl+C and call cleanup
trap cleanup INT TERM

# Keep script running and show status
while true; do
    # Check if both processes are still running
    if ! kill -0 $DASHBOARD_PID 2>/dev/null; then
        echo "[WARNING] Dashboard process ($DASHBOARD_PID) has stopped!"
        echo "Check logs: $SCRIPT_DIR/dashboard.log"
    fi
    
    if ! kill -0 $TENSORBOARD_PID 2>/dev/null; then
        echo "[WARNING] TensorBoard process ($TENSORBOARD_PID) has stopped!"
        echo "Check logs: $SCRIPT_DIR/tensorboard.log"
    fi
    
    sleep 10
done

