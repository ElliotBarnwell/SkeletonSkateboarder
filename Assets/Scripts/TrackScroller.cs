using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

// Scrolls the track toward the player and rotates it so the cross-section at the
// player's position stays normal to the camera. Rotation is smoothed so curves
// ease in gradually rather than snapping, giving the feel of moving through the pipe.
public class TrackScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeed    = 18f;
    public float rotationSpeed  = 3f;

    [Header("References")]
    public SplineContainer splineContainer;

    private float splineT      = 0f;
    private float splineLength = 1f;

    void Awake()
    {
        if (splineContainer != null)
            splineLength = splineContainer.Spline.GetLength();

        // Snap to correct initial transform with no smoothing
        AlignToGate(snap: true);
    }

    void Update()
    {
        splineT += (scrollSpeed / splineLength) * Time.deltaTime;
        AlignToGate(snap: false);
        Physics.SyncTransforms();
    }

    void AlignToGate(bool snap)
    {
        if (splineContainer == null) return;

        Spline spline = splineContainer.Spline;

        float3 lp = spline.EvaluatePosition(splineT);
        float3 lt = math.normalizesafe(spline.EvaluateTangent(splineT));

        Vector3 localPoint   = new Vector3(lp.x, lp.y, lp.z);
        Vector3 localTangent = new Vector3(lt.x, lt.y, lt.z);

        Quaternion targetRot = Quaternion.FromToRotation(localTangent, Vector3.forward);

        // Smooth the rotation so curves ease in; position is derived from the
        // smoothed rotation each frame so the two stay in sync
        transform.rotation = snap
            ? targetRot
            : Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        transform.position = -(transform.rotation * localPoint);
    }
}
