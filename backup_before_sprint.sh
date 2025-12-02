#!/bin/bash
# Backup incompatible files before adding sprint action
# This change breaks existing models (action space 3→4, observation space 13→14)

BACKUP_DIR="backup_before_adding_sprinting"

echo "Creating backup directory: $BACKUP_DIR"
mkdir -p "$BACKUP_DIR"

echo "Backing up incompatible files..."

# Backup prefab with BehaviorParameters
if [ -f "src/Assets/Prefabs/TrainingArea.prefab" ]; then
    cp "src/Assets/Prefabs/TrainingArea.prefab" "$BACKUP_DIR/"
    echo "✓ Backed up TrainingArea.prefab"
else
    echo "✗ TrainingArea.prefab not found"
fi

# Backup scripts
if [ -f "src/Assets/Scripts/ParkourAgent.cs" ]; then
    cp "src/Assets/Scripts/ParkourAgent.cs" "$BACKUP_DIR/"
    echo "✓ Backed up ParkourAgent.cs"
else
    echo "✗ ParkourAgent.cs not found"
fi

if [ -f "src/Assets/Scripts/CharacterConfig.cs" ]; then
    cp "src/Assets/Scripts/CharacterConfig.cs" "$BACKUP_DIR/"
    echo "✓ Backed up CharacterConfig.cs"
else
    echo "✗ CharacterConfig.cs not found"
fi

if [ -f "src/Assets/Scripts/AgentAnimationSync.cs" ]; then
    cp "src/Assets/Scripts/AgentAnimationSync.cs" "$BACKUP_DIR/"
    echo "✓ Backed up AgentAnimationSync.cs"
else
    echo "✗ AgentAnimationSync.cs not found"
fi

# Backup MDP documentation
if [ -f "MDP.md" ]; then
    cp "MDP.md" "$BACKUP_DIR/"
    echo "✓ Backed up MDP.md"
else
    echo "✗ MDP.md not found"
fi

echo ""
echo "Backup complete! Files saved to: $BACKUP_DIR"
echo ""
echo "⚠️  WARNING: After implementing sprint, all existing trained models will be incompatible."
echo "   Action space: 3 → 4"
echo "   Observation space: 13 → 14"
echo "   Must retrain from scratch."

