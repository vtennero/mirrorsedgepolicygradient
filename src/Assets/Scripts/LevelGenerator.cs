using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Platform Configuration")]
    public GameObject platformPrefab;
    public int platformCount = 20;
    public float spacing = 15f;
    public float minHeight = 0f;
    public float maxHeight = 4f;
    
    [Header("Platform Properties")]
    public Vector3 platformSize = new Vector3(12f, 0.5f, 6f);
    public Material platformMaterial;

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {

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

            float x = i * spacing;
            float y = Random.Range(minHeight, maxHeight);
            float z = 0f;
            
            Vector3 position = new Vector3(x, y, z);
            
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = position;
            platform.transform.localScale = platformSize;
            platform.name = $"Platform_{i}";
            
            if (platformMaterial != null)
            {
                platform.GetComponent<Renderer>().material = platformMaterial;
            }
            
            platform.transform.SetParent(this.transform);
        }
    }

    void GeneratePrefabPlatforms()
    {
        for (int i = 0; i < platformCount; i++)
        {

            float x = i * spacing;
            float y = Random.Range(minHeight, maxHeight);
            float z = 0f;
            
            Vector3 position = new Vector3(x, y, z);
            
            GameObject platform = Instantiate(platformPrefab, position, Quaternion.identity);
            platform.name = $"Platform_{i}";
            platform.transform.SetParent(this.transform);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < platformCount; i++)
        {
            float x = i * spacing;
            float y = (minHeight + maxHeight) * 0.5f;
            Vector3 position = new Vector3(x, y, 0);
            Gizmos.DrawWireCube(position, platformSize);
        }
    }
}
