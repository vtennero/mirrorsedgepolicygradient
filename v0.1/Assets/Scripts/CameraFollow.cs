using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 2, -3); // Closer to character
    
    void LateUpdate()
    {
        if (player != null)
        {
            // Rotate the offset based on player's rotation
            Vector3 rotatedOffset = player.rotation * offset;
            transform.position = player.position + rotatedOffset;
            transform.LookAt(player.position + Vector3.up * 1.5f);
        }
    }
}
