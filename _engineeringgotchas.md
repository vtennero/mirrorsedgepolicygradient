# Engineering Gotchas

## Demo Mode: Environment Variables vs Files

**Issue:** Unity Editor doesn't inherit environment variables from separate Python processes.

**Problem:**
- Python scripts (`run_inference.py`, `train_with_progress.py`) set `MLAGENTS_DEMO_MODE` in their process
- If Unity Editor is already running, it doesn't see the env var (env vars are process-local)
- Result: Demo mode doesn't activate when Unity Editor is launched separately

**Solution:**
- Check environment variable first (works when Unity is launched by mlagents-learn)
- Fallback to reading `demo_mode.env` file from disk (works when Unity Editor is already open)
- Files are accessible to any process, env vars are only inherited by child processes

**When it works:**
- Unity launched by mlagents-learn → inherits env var → demo mode ON/OFF
- Unity Editor already open → reads `demo_mode.env` file → demo mode ON/OFF

**Files affected:** All `CheckDemoMode()` / `IsDemoMode()` methods in Unity scripts

