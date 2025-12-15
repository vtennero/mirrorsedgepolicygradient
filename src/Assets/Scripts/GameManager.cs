using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject playerPrefab;
    public LevelGenerator levelGenerator;
    
    [Header("Spawn Settings")]
    [Tooltip("Override config values if needed, otherwise uses CharacterConfig")]
    public Vector3 playerSpawnPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    
    void Awake()
    {

        if (playerSpawnPosition.x == float.MinValue)
        {
            CharacterConfig config = CharacterConfigManager.Config;
            playerSpawnPosition = config.defaultSpawnPosition;
        }
        
        ControlModeManager existing = FindObjectOfType<ControlModeManager>();
        if (existing == null && ControlModeManager.Instance == null)
        {
            GameObject managerObj = new GameObject("ControlModeManager (Auto-Created)");
            managerObj.AddComponent<ControlModeManager>();
            Debug.Log("GameManager: ✓ Auto-created ControlModeManager");
        }
        else if (existing != null)
        {
            Debug.Log($"GameManager: ControlModeManager already exists in scene: {existing.gameObject.name}");
        }
    }
    
    void Start()
    {
        InitializeGame();
    }
    
    void InitializeGame()
    {

        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
        {
            Debug.Log($"Game initialized - Found existing player in scene: {existingPlayer.gameObject.name}");
            return;
        }
        
        if (playerPrefab != null)
        {
            GameObject spawnedPlayer = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            Debug.Log($"Game initialized - Spawned player from prefab: {spawnedPlayer.name}");
        }
        else
        {
            Debug.Log("Game initialized - No player found in scene and no playerPrefab assigned. Please add a player GameObject to the scene.");
        }
    }
    
    void Update()
    {

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            CharacterConfig config = CharacterConfigManager.Config;
            if (player.transform.position.y < config.playerResetThreshold)
            {
                ResetPlayer(player);
            }
        }
    }
    
    void ResetPlayer(PlayerController player)
    {
        player.transform.position = playerSpawnPosition;
        Debug.Log("Player reset to spawn position");
    }
}
