@echo off
REM Backup incompatible files before adding sprint action
REM This change breaks existing models (action space 3→4, observation space 13→14)

set BACKUP_DIR=backup_before_adding_sprinting

echo Creating backup directory: %BACKUP_DIR%
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo Backing up incompatible files...
echo.

REM Backup prefab with BehaviorParameters
if exist "src\Assets\Prefabs\TrainingArea.prefab" (
    copy "src\Assets\Prefabs\TrainingArea.prefab" "%BACKUP_DIR%\" >nul
    echo [OK] Backed up TrainingArea.prefab
) else (
    echo [FAIL] TrainingArea.prefab not found
)

REM Backup scripts
if exist "src\Assets\Scripts\ParkourAgent.cs" (
    copy "src\Assets\Scripts\ParkourAgent.cs" "%BACKUP_DIR%\" >nul
    echo [OK] Backed up ParkourAgent.cs
) else (
    echo [FAIL] ParkourAgent.cs not found
)

if exist "src\Assets\Scripts\CharacterConfig.cs" (
    copy "src\Assets\Scripts\CharacterConfig.cs" "%BACKUP_DIR%\" >nul
    echo [OK] Backed up CharacterConfig.cs
) else (
    echo [FAIL] CharacterConfig.cs not found
)

if exist "src\Assets\Scripts\AgentAnimationSync.cs" (
    copy "src\Assets\Scripts\AgentAnimationSync.cs" "%BACKUP_DIR%\" >nul
    echo [OK] Backed up AgentAnimationSync.cs
) else (
    echo [FAIL] AgentAnimationSync.cs not found
)

REM Backup MDP documentation
if exist "MDP.md" (
    copy "MDP.md" "%BACKUP_DIR%\" >nul
    echo [OK] Backed up MDP.md
) else (
    echo [FAIL] MDP.md not found
)

echo.
echo Backup complete! Files saved to: %BACKUP_DIR%
echo.
echo WARNING: After implementing sprint, all existing trained models will be incompatible.
echo    Action space: 3 -^> 4
echo    Observation space: 13 -^> 14
echo    Must retrain from scratch.
pause

