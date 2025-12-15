using UnityEngine;

[CreateAssetMenu(fileName = "CharacterConfig", menuName = "Parkour/Character Config")]
public class CharacterConfig : ScriptableObject
{
    [Header("=== MOVEMENT SPEED ===")]
    [Tooltip("Normal walking/running speed (units per second)")]
    public float moveSpeed = 6f;
    
    [Tooltip("Maximum sprint speed when fully accelerated (units per second)")]
    public float sprintSpeed = 12f;
    
    [Tooltip("Time in seconds to reach maximum sprint speed from normal speed")]
    public float sprintAccelTime = 3f;
    
    [Tooltip("How fast sprint speed decays when not sprinting (multiplier)")]
    public float sprintDecelerationRate = 2f;
    
    [Tooltip("Minimum movement input magnitude to be considered 'moving' (0-1)")]
    public float movementThreshold = 0.1f;
    
    [Tooltip("Sprint progress threshold before sprint animation activates (0-1)")]
    public float sprintAnimationThreshold = 0.5f;
    
    [Header("=== JUMPING ===")]
    [Tooltip("Vertical jump force applied when jumping (higher = jump higher). Reduced for more horizontal, human-like jumps.")]
    public float jumpForce = 8f;
    
    [Tooltip("Extra forward/horizontal speed boost when jumping (for longer, more horizontal jumps)")]
    public float jumpForwardBoost = 10f;
    
    [Tooltip("How fast jump momentum decays in air (air resistance multiplier)")]
    public float jumpMomentumDecayRate = 2f;
    
    [Header("=== PHYSICS ===")]
    [Tooltip("Gravity force applied per second (negative = downward)")]
    public float gravity = -20f;
    
    [Tooltip("Small negative velocity to keep character grounded when on ground")]
    public float groundedVelocityReset = -2f;
    
    [Header("=== GROUND DETECTION ===")]
    [Tooltip("Distance below character to check for ground (in units)")]
    public float groundCheckDistance = 0.1f;
    
    [Header("=== CAMERA ===")]
    [Tooltip("Camera rotation speed multiplier")]
    public float cameraRotationSpeed = 2f;
    
    [Tooltip("Vertical camera rotation limit in degrees (up/down look limit)")]
    public float cameraVerticalRotationLimit = 80f;
    
    [Tooltip("Camera offset from player (behind and above)")]
    public Vector3 cameraOffset = new Vector3(0, 2, -5);
    
    [Tooltip("Height offset for camera look-at target (above player center)")]
    public float cameraLookHeightOffset = 1.5f;
    
    [Tooltip("Mouse sensitivity multiplier for camera/player rotation")]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Mouse input multiplier (0.1 = slower, 1.0 = faster)")]
    public float mouseInputMultiplier = 0.1f;
    
    [Tooltip("Player vertical look limit in degrees (for first-person mode)")]
    public float playerVerticalLookLimit = 80f;
    
    [Header("=== ANIMATION ===")]
    [Tooltip("Base animation playback speed (1.0 = normal speed)")]
    public float baseAnimationSpeed = 1f;
    
    [Tooltip("Maximum animation speed multiplier when sprinting (1.5 = 50% faster)")]
    public float maxSprintAnimationSpeed = 1.5f;
    
    [Header("=== STAMINA SYSTEM ===")]
    [Tooltip("Maximum stamina capacity")]
    public float maxStamina = 100f;
    
    [Tooltip("Stamina consumed per second while sprinting. Reduced from 33.33 to allow stamina to build up for rolls/jumps.")]
    public float staminaConsumptionRate = 20f;
    
    [Tooltip("Stamina consumed per jump. Increased from 5 to 20 to force agent to conserve stamina for jumps (20% of max stamina).")]
    public float jumpStaminaCost = 20f;
    
    [Tooltip("Stamina consumed per roll forward. Should be achievable mid-episode even after some sprinting. Reduced to allow rolls throughout episode.")]
    public float rollStaminaCost = 60f;
    
    [Tooltip("Stamina regenerated per second when not sprinting/jumping/rolling. Higher than consumption to allow stamina to build up for rolls/jumps.")]
    public float staminaRegenRate = 30f;
    
    [Tooltip("Penalty per step when stamina is critically low (< 20%). Discourages keeping stamina at 0.")]
    public float lowStaminaPenalty = -0.002f;
    
    [Header("=== RL AGENT SETTINGS ===")]
    [Tooltip("Forward progress reward multiplier (reward per unit of forward progress)")]
    public float progressRewardMultiplier = 0.1f;
    
    [Tooltip("Time penalty per fixed update (encourages speed, negative = penalty)")]
    public float timePenalty = -0.001f;
    
    [Tooltip("Distance from target to consider 'reached' (in units)")]
    public float targetReachDistance = 2f;
    
    [Tooltip("Reward for reaching the target")]
    public float targetReachReward = 10f;
    
    [Tooltip("Y position threshold for falling off (below this = fall penalty)")]
    public float fallThreshold = -5f;
    
    [Tooltip("Penalty for falling off or timing out")]
    public float fallPenalty = -1f;
    
    [Tooltip("Maximum episode time in seconds before timeout. Should be sufficient for agent to reach target (calculated dynamically based on platform layout)")]
    public float episodeTimeout = 100f;
    
    [Tooltip("Raycast distance for obstacle detection (in units)")]
    public float obstacleRaycastDistance = 10f;
    
    [Header("=== STYLE ACTION REWARDS ===")]
    [Tooltip("Base reward for roll actions (always given, encourages roll usage). Should be significant compared to progress reward (0.1/unit).")]
    public float rollBaseReward = 0.5f;
    
    [Tooltip("Style bonus magnitude for roll actions (applied in style episodes, in addition to base reward). Should be substantial to incentivize rolls.")]
    public float rollStyleBonus = 1.5f;
    
    [Tooltip("Probability that an episode will have style bonuses enabled (0.1 = 10%, 0.2 = 20%)")]
    [Range(0f, 1f)]
    public float styleEpisodeFrequency = 0.4f;
    
    [Header("=== GAME SETTINGS ===")]
    [Tooltip("Y position threshold for player reset (below this = reset to spawn)")]
    public float playerResetThreshold = -10f;
    
    [Tooltip("Default player spawn position")]
    public Vector3 defaultSpawnPosition = new Vector3(0, 1, 0);
}
