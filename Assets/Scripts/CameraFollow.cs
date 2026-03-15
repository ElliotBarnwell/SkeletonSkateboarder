using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public SplineContainer  splineContainer;

    [Header("Offset")]
    public float behindDistance = 10f;
    public float heightOffset   = 5f;

    [Header("Rotation")]
    public float basePitch        = 10f;   // Constant downward tilt (degrees)
    public float slopePitchAmount = 25f;   // How much slope affects pitch

    [Header("Smoothing")]
    public float positionSmoothTime = 0.1f;
    public float rotationSmoothTime = 0.15f;
    public float splineTSmoothTime  = 0.1f;

    private Vector3    posVelocity = Vector3.zero;
    private Quaternion targetRot   = Quaternion.identity;
    private float      rotVelocity = 0f;
    private float      splineT     = 0f;
    private float      splineTVel  = 0f;

    void Start()
    {
        if (player == null) return;
        Vector3 startPos   = player.transform.position + new Vector3(0f, heightOffset, -behindDistance);
        transform.position = startPos;
        posVelocity        = Vector3.zero;
        targetRot          = TargetRotation(Vector3.forward);
        transform.rotation = targetRot;
    }

    void LateUpdate()
    {
        if (player == null || splineContainer == null) return;

        // ── Find spline T nearest to player ───────────────────────────────
        // Convert player world pos to spline local space (spline container moves).
        float3 localPos = splineContainer.transform.InverseTransformPoint(player.transform.position);
        SplineUtility.GetNearestPoint(splineContainer.Spline, localPos, out _, out float rawT, 8, 4);
        splineT = Mathf.SmoothDamp(splineT, rawT, ref splineTVel, splineTSmoothTime);

        // ── Sample tangent at smoothed T ──────────────────────────────────
        // Tangent is in spline local space — transform direction to world space.
        float3  localTan   = math.normalizesafe(splineContainer.Spline.EvaluateTangent(splineT));
        Vector3 worldForward = splineContainer.transform.TransformDirection(
            new Vector3(localTan.x, localTan.y, localTan.z)
        );

        // ── Position: world-space offsets, Y tracks player height ─────────
        transform.position = Vector3.SmoothDamp(
            transform.position,
            TargetPosition(),
            ref posVelocity,
            positionSmoothTime
        );

        // ── Rotation: pitch from spline slope ─────────────────────────────
        Quaternion finalRot    = TargetRotation(worldForward);
        float      angle       = Quaternion.Angle(targetRot, finalRot);
        float      smoothAngle = Mathf.SmoothDamp(0f, angle, ref rotVelocity, rotationSmoothTime);
        targetRot              = angle > 0.001f
            ? Quaternion.Slerp(targetRot, finalRot, smoothAngle / angle)
            : finalRot;
        transform.rotation = targetRot;
    }

    Vector3 TargetPosition()
    {
        Transform t = player.transform;
        return new Vector3(
            t.position.x,
            t.position.y + heightOffset,
            t.position.z - behindDistance
        );
    }

    Quaternion TargetRotation(Vector3 splineForward)
    {
        // Derive pitch from how much the spline points up or down.
        // Positive Y in forward = upslope, negative = downslope.
        float slopeAngle = Mathf.Asin(Mathf.Clamp(splineForward.y, -1f, 1f)) * Mathf.Rad2Deg;
        float pitch      = basePitch - slopeAngle * (slopePitchAmount / 90f);
        return Quaternion.Euler(pitch, 0f, 0f);
    }
}
