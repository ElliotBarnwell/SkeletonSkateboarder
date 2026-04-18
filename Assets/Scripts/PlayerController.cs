using UnityEngine;

// No CharacterController, no Rigidbody — fully custom physics.
// Player snaps to the track surface via raycast and jumps via a parabolic arc.
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float lateralSpeed  = 8f;
    [HideInInspector] public float lateralBound; // Set automatically from HalfPipeExtruder at start

    [Header("References")]
    public HalfPipeExtruder halfPipe;
    public Transform        groundCheck; // empty child at base of player mesh

    [Header("Grounding")]
    public float groundRayLength = 8f;
    public float rotationSmoothSpeed = 15f;
    public LayerMask trackLayer;

    [Header("Jump")]
    public float jumpHeight            = 4f;
    public float spinSpeed             = 360f; // degrees per second during jump
    public float gravity               = 25f;  // Higher = snappier fall, lower = floatier
    public float hangtimeThreshold     = 3f;   // Velocity window (m/s) around apex where hangtime applies
    public float hangtimeGravityScale  = 0.1f; // Gravity multiplier at apex (0 = freeze, 1 = no hangtime)

    [Header("Collision")]
    public float collisionRadius  = 0.5f;
    public LayerMask obstacleLayer;
    [Tooltip("How far below the last known ground the player must fall before dying.")]
    public float fallDeathDepth   = 4f;

    private bool       isJumping          = false;
    private float      jumpVelocity       = 0f;
    private float      jumpArcHeight      = 0f;
    private float      groundY            = 0f;
    private Vector3    groundPoint        = Vector3.zero;
    private float      spinAngle          = 0f;
    private Quaternion smoothedSurfaceRot = Quaternion.identity;
    private float      _fallVelocity      = 0f;
    private int        _offSurfaceFrames  = 0; // grace period before falling starts

    public Vector3 SlopeNormal { get; private set; } = Vector3.up;
    public bool    IsGrounded  { get; private set; } = true;

    // Derived from HalfPipeExtruder at runtime
    private float pipeRadius        = 4f;
    private float pipeFlatHalfWidth = 2f;

    void Start()
    {
        if (halfPipe != null)
        {
            pipeRadius        = halfPipe.radius;
            pipeFlatHalfWidth = halfPipe.flatBottomWidth * 0.5f;
        }
        lateralBound = pipeFlatHalfWidth + pipeRadius;
    }

    void Update()
    {
        // ── Lateral input ─────────────────────────────────────────────────
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal =  1f;

        // ── Jump input ────────────────────────────────────────────────────
        if (!isJumping && Input.GetKeyDown(KeyCode.Space))
        {
            isJumping    = true;
            // v0 derived from kinematic: h = v0²/2g  →  v0 = sqrt(2gh)
            jumpVelocity  = Mathf.Sqrt(2f * gravity * jumpHeight);
            jumpArcHeight = 0f;
        }

        // ── Apply lateral movement before raycasting ──────────────────────
        // Move X first so the surface detection reflects the player's actual
        // new position this frame, not last frame's stale transform.position.
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + horizontal * lateralSpeed * Time.deltaTime, -lateralBound, lateralBound);

        // ── Raycast to find surface ───────────────────────────────────────
        // The wall curves from flat (normal=up) to nearly vertical at the lip.
        // A single downward ray goes blind on steep sections, so we fire three
        // rays at increasing inward angles and take whichever hits closest.
        //
        // Origin is offset along SlopeNormal (not world Y) so it lifts away from
        // the surface correctly even when the player is rotated on a steep wall.
        Transform checkPoint = groundCheck != null ? groundCheck : transform;
        Vector3   rayOrigin  = checkPoint.position + Vector3.up * 2f;

        RaycastHit hit       = default;
        bool       onSurface = false;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundRayLength, trackLayer))
            onSurface = true;

        if (onSurface)
        {
            groundY     = hit.point.y;
            groundPoint = hit.point;
            SlopeNormal = hit.normal;
        }

        // ── Compute jump arc via kinematic velocity ───────────────────────
        if (isJumping)
        {
            // Near the apex (low velocity) gravity is reduced for hangtime feel
            float apexFraction  = Mathf.InverseLerp(hangtimeThreshold, 0f, Mathf.Abs(jumpVelocity));
            float appliedGravity = Mathf.Lerp(gravity, gravity * hangtimeGravityScale, apexFraction);

            jumpVelocity  -= appliedGravity * Time.deltaTime;
            jumpArcHeight += jumpVelocity * Time.deltaTime;
            IsGrounded     = false;

            if (jumpArcHeight <= 0f)
            {
                jumpArcHeight = 0f;
                jumpVelocity  = 0f;
                isJumping     = false;
                IsGrounded    = true;
            }
        }
        else
        {
            IsGrounded = true;
        }

        // ── Apply movement ────────────────────────────────────────────────
        float bottomOffset = transform.position.y - checkPoint.position.y;

        if (onSurface)
        {
            pos.y             = groundPoint.y + bottomOffset + jumpArcHeight;
            _fallVelocity     = 0f;
            _offSurfaceFrames = 0;
        }
        else if (isJumping)
        {
            // Jumping over a gap — honour the arc, don't fall yet.
            pos.y             = groundY + bottomOffset + jumpArcHeight;
            _fallVelocity     = 0f;
            _offSurfaceFrames = 0;
        }
        else
        {
            // No surface hit and not jumping — player is over a gap.
            // Allow a 3-frame grace period so steep curved walls don't
            // falsely trigger a fall when the downward ray briefly misses.
            _offSurfaceFrames++;
            if (_offSurfaceFrames > 3)
            {
                _fallVelocity -= gravity * Time.deltaTime;
                pos.y         += _fallVelocity * Time.deltaTime;

                if (pos.y < groundY - fallDeathDepth)
                    GameManager.Instance?.TriggerGameOver();
            }
            else
            {
                pos.y = groundY + bottomOffset; // hold steady during grace period
            }
        }

        transform.position = pos;

        // ── Align rotation to surface normal ──────────────────────────────
        // Surface alignment is smoothed independently so the spin can be
        // composed on top without the two interfering with each other.
        Quaternion targetSurfaceRot = Quaternion.FromToRotation(Vector3.up, SlopeNormal);
        smoothedSurfaceRot = Quaternion.Slerp(smoothedSurfaceRot, targetSurfaceRot, rotationSmoothSpeed * Time.deltaTime);

        // ── Jump spin ─────────────────────────────────────────────────────
        if (isJumping)
            spinAngle += spinSpeed * Time.deltaTime;
        else
            spinAngle = 0f;

        transform.rotation = smoothedSurfaceRot * Quaternion.AngleAxis(spinAngle, Vector3.up);

        // ── Obstacle collision ────────────────────────────────────────────
        Collider[] hits = Physics.OverlapSphere(transform.position, collisionRadius, obstacleLayer);
        if (hits.Length > 0)
        {
            foreach (Collider col in hits)
                Debug.Log($"[PlayerController] Collider hit: {col.name} on {col.gameObject.name} (layer: {LayerMask.LayerToName(col.gameObject.layer)})");
            GameManager.Instance?.TriggerGameOver();
        }
    }
}
