using UnityEngine;

public class SimpleMaggieSetup : MonoBehaviour
{
    void Start()
    {

        Debug.Log("=== MAGGIE SETUP INSTRUCTIONS ===");
        Debug.Log("1. Drag idle.glb from Project into Hierarchy");
        Debug.Log("2. Add CharacterController component to it");
        Debug.Log("3. Add PlayerController component to it");
        Debug.Log("4. Set position to (0, 1, 0)");
        Debug.Log("5. Set rotation to (0, 90, 0)");
        Debug.Log("6. Make sure Main Camera has CameraFollow script with Maggie as target");
        
        CreatePlatformsOnly();
    }
    
    void CreatePlatformsOnly()
    {

        for (int i = 0; i < 10; i++)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = new Vector3(i * 4f, Random.Range(0, 2f), 0);
            platform.transform.localScale = new Vector3(3f, 0.5f, 3f);
            platform.name = "Platform_" + i;
        }
        
        Debug.Log("Platforms created! Now manually add Maggie to the scene.");
    }
}
