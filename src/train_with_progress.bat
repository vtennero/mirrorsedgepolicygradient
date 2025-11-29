@echo off
REM Wrapper batch file for train_with_progress.py
REM Usage: train_with_progress.bat <config_file> [additional args...]
REM Example: train_with_progress.bat parkour_config.yaml --run-id=test_v10 --force

python train_with_progress.py %*

