using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject playerPrefab;
    public LevelGenerator levelGenerator;
    
    [Header("Spawn Settings")]
    public Vector3 playerSpawnPosition = new Vector3(0, 1, 0);
    
    void Start()
    {
        InitializeGame();
    }
    
    void InitializeGame()
    {
        // The level generator will generate platforms automatically in its Start method
        // Player should be spawned manually or via prefab in the scene
        
        // If no player is found in scene and we have a prefab, spawn one
        if (FindObjectOfType<PlayerController>() == null && playerPrefab != null)
        {
            Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        }
        
        Debug.Log("Game initialized - Player should be able to move with WASD and jump with Space");
    }
    
    void Update()
    {
        // Future game logic can go here
        // For now, just handle basic game state
        
        // Example: Reset player if they fall too far
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && player.transform.position.y < -10f)
        {
            ResetPlayer(player);
        }
    }
    
    void ResetPlayer(PlayerController player)
    {
        player.transform.position = playerSpawnPosition;
        Debug.Log("Player reset to spawn position");
    }
}
