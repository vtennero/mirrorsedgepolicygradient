using UnityEngine;

public class QuickCharacterSetup : MonoBehaviour
{
    void Start()
    {
        CreateDemo();
    }
    
    void CreateDemo()
    {
        // Create platforms
        for (int i = 0; i < 10; i++)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = new Vector3(i * 4f, Random.Range(0, 2f), 0);
            platform.transform.localScale = new Vector3(3f, 0.5f, 3f);
            platform.name = "Platform_" + i;
            platform.GetComponent<Renderer>().material.color = Color.gray;
        }
        
        // Create a better looking character (capsule body + cube head)
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);
        player.transform.rotation = Quaternion.Euler(0, 90, 0);
        
        // Body (capsule)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(player.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        body.GetComponent<Renderer>().material.color = Color.blue;
        DestroyImmediate(body.GetComponent<CapsuleCollider>());
        
        // Head (sphere)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 1.2f, 0);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        head.GetComponent<Renderer>().material.color = Color.yellow;
        DestroyImmediate(head.GetComponent<SphereCollider>());
        
        // Add components to main player object
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.zero;
        
        player.AddComponent<PlayerController>();
        
        // Setup camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.player = player.transform;
        }
        
        Debug.Log("Character demo created! Use WASD + Mouse + Space");
    }
}
