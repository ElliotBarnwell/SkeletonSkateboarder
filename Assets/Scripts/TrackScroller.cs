using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

// Scrolls the track toward the player and rotates it so the cross-section at the
// player's position stays normal to the camera. Rotation is smoothed so curves
// ease in gradually rather than snapping, giving the feel of moving through the pipe.
public class TrackScroller : MonoBehaviour
{
    public static TrackScroller Instance { get; private set; }

    [Header("Scroll Settings")]
    public float scrollSpeed    = 18f;
    public float rotationSpeed  = 3f;

    [Header("Speed Boost")]
    [Tooltip("How quickly the boosted speed bleeds back to scrollSpeed (units/sec²).")]
    public float boostDecayRate = 12f;

    [Header("References")]
    public SplineContainer splineContainer;

    private float splineT       = 0f;
    private float splineLength  = 1f;
    private float _currentSpeed;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _currentSpeed = scrollSpeed;

        if (splineContainer != null)
            splineLength = splineContainer.Spline.GetLength();

        // Snap to correct initial transform with no smoothing
        AlignToGate(snap: true);
    }

    void Update()
    {
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, scrollSpeed, boostDecayRate * Time.deltaTime);
        splineT += (_currentSpeed / splineLength) * Time.deltaTime;
        AlignToGate(snap: false);
        Physics.SyncTransforms();
    }

    /// Instantly raises scroll speed by <paramref name="amount"/> above the baseline.
    /// The excess decays back to scrollSpeed at boostDecayRate units/sec.
    public void ApplyBoost(float amount)
    {
        _currentSpeed = Mathf.Max(_currentSpeed, scrollSpeed + amount);
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
