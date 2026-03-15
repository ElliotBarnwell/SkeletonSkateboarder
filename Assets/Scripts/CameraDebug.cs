using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// Attach to the same GameObject as CameraFollow.
public class CameraDebug : MonoBehaviour
{
    public SplineContainer  splineContainer;
    public PlayerController player;
    public TrackScroller    trackScroller;

    // Spike thresholds — log when values exceed these
    [Header("Log Thresholds")]
    public float playerYSpikeThreshold     = 0.05f;   // metres per frame
    public float tangentSpikeThreshold     = 3f;      // degrees per frame
    public float trackPosSpikeThreshold    = 0.5f;    // metres per frame
    public float cameraPosSpikethreshold   = 0.1f;    // metres per frame

    private Vector3 prevPlayerPos;
    private Vector3 prevTrackPos;
    private Vector3 prevCameraPos;
    private Vector3 prevTangent     = Vector3.forward;
    private float   splineTVel;
    private float   smoothedT;
    private int     frame;

    void Start()
    {
        if (player != null)      prevPlayerPos  = player.transform.position;
        if (trackScroller != null) prevTrackPos = trackScroller.transform.position;
        prevCameraPos = transform.position;
    }

    void LateUpdate()
    {
        frame++;

        // ── Player position delta ─────────────────────────────────────────
        if (player != null)
        {
            Vector3 playerPos   = player.transform.position;
            float   playerDeltaY = Mathf.Abs(playerPos.y - prevPlayerPos.y);
            float   playerDeltaX = Mathf.Abs(playerPos.x - prevPlayerPos.x);

            if (playerDeltaY > playerYSpikeThreshold)
                // Debug.LogWarning($"[Frame {frame}] PLAYER Y SPIKE: {playerDeltaY:F4}m  pos={playerPos}");

            if (playerDeltaX > playerYSpikeThreshold)
                // Debug.Log($"[Frame {frame}] Player X delta: {playerDeltaX:F4}m");

            prevPlayerPos = playerPos;
        }

        // ── Track position delta ──────────────────────────────────────────
        if (trackScroller != null)
        {
            Vector3 trackPos   = trackScroller.transform.position;
            float   trackDelta = Vector3.Distance(trackPos, prevTrackPos);

            if (trackDelta > trackPosSpikeThreshold)
                // Debug.LogWarning($"[Frame {frame}] TRACK POS SPIKE: {trackDelta:F4}m  (expected ~{trackScroller.scrollSpeed * Time.deltaTime:F4})");

            prevTrackPos = trackPos;
        }

        // ── Camera position delta ─────────────────────────────────────────
        {
            float cameraDelta = Vector3.Distance(transform.position, prevCameraPos);
            if (cameraDelta > cameraPosSpikethreshold)
                //Debug.LogWarning($"[Frame {frame}] CAMERA POS SPIKE: {cameraDelta:F4}m");
            prevCameraPos = transform.position;
        }

        // ── Spline tangent delta ──────────────────────────────────────────
        if (splineContainer != null && player != null)
        {
            float3 localPos = splineContainer.transform.InverseTransformPoint(player.transform.position);
            SplineUtility.GetNearestPoint(splineContainer.Spline, localPos, out _, out float rawT, 8, 4);
            smoothedT = Mathf.SmoothDamp(smoothedT, rawT, ref splineTVel, 0.1f);

            float3  localTan = math.normalizesafe(splineContainer.Spline.EvaluateTangent(rawT));
            Vector3 tangent  = splineContainer.transform.TransformDirection(
                new Vector3(localTan.x, localTan.y, localTan.z));

            float tangentDelta = Vector3.Angle(prevTangent, tangent);
            if (tangentDelta > tangentSpikeThreshold)
                //Debug.LogWarning($"[Frame {frame}] TANGENT SPIKE: {tangentDelta:F2}deg  rawT={rawT:F4}  smoothedT={smoothedT:F4}");

            prevTangent = tangent;
        }

        // ── Summary log every 60 frames ───────────────────────────────────
        if (frame % 60 == 0 && player != null)
        {
            // Debug.Log($"[Frame {frame}] STATUS  playerY={player.transform.position.y:F3}" +
            //           $"  grounded={player.GetComponent<CharacterController>()?.isGrounded}" +
            //           $"  slopeNormal={player.SlopeNormal}" +
            //           $"  splineT={smoothedT:F4}");
        }
    }
}
