using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform       player;
    public SplineContainer splineContainer;
    public TrackScroller   trackScroller;

    [Header("Offset")]
    public float behindDistance = 10f;   // Distance behind player along spline direction
    public float heightOffset   = 3f;    // Height above player along spline up
    public float pitchAngle     = 10f;   // Extra downward tilt

    [Header("Smoothing")]
    public float rotationSmooth = 5f;
    public float positionSmooth = 10f;

    private float      splineT      = 0f;
    private Quaternion targetRot;
    private Vector3    posVelocity  = Vector3.zero;
    private float      splineLength = 0f;

    void Start()
    {
        targetRot    = transform.rotation;
        if (splineContainer != null)
            splineLength = splineContainer.Spline.GetLength();
    }

    void LateUpdate()
    {
        if (player == null || splineContainer == null || trackScroller == null) return;

        // ── Advance T along spline ────────────────────────────────────────
        splineT += (trackScroller.scrollSpeed * Time.deltaTime) / splineLength;
        splineT  = Mathf.Clamp01(splineT);

        // ── Sample spline direction at current T ──────────────────────────
        float3  tan      = math.normalizesafe(splineContainer.Spline.EvaluateTangent(splineT));
        Vector3 forward  = new Vector3(tan.x, tan.y, tan.z);

        // Strip X so no sideways roll — only pitch with ramps
        forward          = new Vector3(0f, forward.y, forward.z).normalized;

        // Build right and up from forward
        Vector3 right    = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up       = Vector3.Cross(forward, right).normalized;

        // ── Position camera behind and above player ALONG spline axes ─────
        // Using spline-relative axes keeps player centred in frame on ramps
        Vector3 targetPos = player.position
                          - forward * behindDistance   // Behind along spline direction
                          + up      * heightOffset;    // Above along spline up

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref posVelocity,
            1f / positionSmooth
        );

        // ── Rotation faces forward along spline + extra pitch ─────────────
        Quaternion splineRot   = Quaternion.LookRotation(forward, Vector3.up);
        Quaternion finalRot    = splineRot * Quaternion.Euler(pitchAngle, 0f, 0f);

        targetRot          = Quaternion.Slerp(targetRot, finalRot, rotationSmooth * Time.deltaTime);
        transform.rotation = targetRot;
    }
}