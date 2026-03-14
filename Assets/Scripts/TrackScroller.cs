using UnityEngine;

// Attach this to your HalfPipe object and any parent objects in the scene
// Everything with this script scrolls toward the player automatically
public class TrackScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeed = 18f;    // Medium speed

    void FixedUpdate()
    {
        // Move everything toward the player (negative Z)
        transform.Translate(Vector3.back * scrollSpeed * Time.deltaTime, Space.World);
    }
}