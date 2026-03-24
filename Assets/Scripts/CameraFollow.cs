using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Offset")]
    public float behindDistance = 10f;
    public float heightOffset   = 5f;

    void LateUpdate()
    {
        if (player == null) return;

        transform.position = player.position
                           - Vector3.forward * behindDistance
                           + Vector3.up      * heightOffset;

        transform.LookAt(player);
    }
}
