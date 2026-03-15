using UnityEngine;

// Moves the track toward the player each frame.
// Physics.SyncTransforms() keeps the MeshCollider position current so raycasts
// in PlayerController always hit the correct surface location.
public class TrackScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeed = 18f;

    void Update()
    {
        transform.Translate(Vector3.back * scrollSpeed * Time.deltaTime, Space.World);
        Physics.SyncTransforms();
    }
}
