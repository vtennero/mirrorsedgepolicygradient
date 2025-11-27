using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Platform Configuration")]
    public GameObject platformPrefab;
    public int platformCount = 8;
    public float spacing = 15f;  // MUCH longer distances for running
    public float minHeight = 0f;
    public float maxHeight = 4f; // More dramatic height differences
    
    [Header("Platform Properties")]
    public Vector3 platformSize = new Vector3(12f, 0.5f, 6f); // LONG platforms for running: 12x6 units
    public Material platformMaterial;

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        // If no prefab is assigned, create platforms using primitives
        if (platformPrefab == null)
        {
            GeneratePrimitivePlatforms();
        }
        else
        {
            GeneratePrefabPlatforms();
        }
    }

    void GeneratePrimitivePlatforms()
    {
        for (int i = 0; i < platformCount; i++)
        {
            // Calculate platform position
            float x = i * spacing;
            float y = Random.Range(minHeight, maxHeight);
            float z = 0f;
            
            Vector3 position = new Vector3(x, y, z);
            
            // Create cube primitive
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = position;
            platform.transform.localScale = platformSize;
            platform.name = $"Platform_{i}";
            
            // Ensure it has a collider (primitives come with colliders)
            // Apply material if provided
            if (platformMaterial != null)
            {
                platform.GetComponent<Renderer>().material = platformMaterial;
            }
            
            // Parent to this object for organization
            platform.transform.SetParent(this.transform);
        }
    }

    void GeneratePrefabPlatforms()
    {
        for (int i = 0; i < platformCount; i++)
        {
            // Calculate platform position
            float x = i * spacing;
            float y = Random.Range(minHeight, maxHeight);
            float z = 0f;
            
            Vector3 position = new Vector3(x, y, z);
            
            // Instantiate prefab
            GameObject platform = Instantiate(platformPrefab, position, Quaternion.identity);
            platform.name = $"Platform_{i}";
            platform.transform.SetParent(this.transform);
        }
    }

    // For testing in editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < platformCount; i++)
        {
            float x = i * spacing;
            float y = (minHeight + maxHeight) * 0.5f; // Average height for preview
            Vector3 position = new Vector3(x, y, 0);
            Gizmos.DrawWireCube(position, platformSize);
        }
    }
}
