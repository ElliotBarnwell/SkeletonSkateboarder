using UnityEngine;

// No CharacterController, no Rigidbody — fully custom physics.
// Player snaps to the track surface via raycast and jumps via a parabolic arc.
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float lateralSpeed  = 8f;
    public float lateralBound  = 10f;  // Max X from centre — match to half-pipe edge

    [Header("Grounding")]
    public float groundOffset    = 0.5f;
    public float groundRayLength = 8f;
    public LayerMask trackLayer;

    [Header("Jump")]
    public float jumpHeight   = 4f;
    public float jumpDuration = 0.6f;

    [Header("Collision")]
    public float collisionRadius = 0.5f;
    public LayerMask obstacleLayer;

    private bool  isJumping = false;
    private float jumpTimer = 0f;
    private float groundY   = 0f;

    public Vector3 SlopeNormal { get; private set; } = Vector3.up;
    public bool    IsGrounded  { get; private set; } = true;

    void Update()
    {
        // ── Lateral input ─────────────────────────────────────────────────
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal =  1f;

        // ── Jump input ────────────────────────────────────────────────────
        if (!isJumping && Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
            jumpTimer = 0f;
        }

        // ── Raycast to find surface ───────────────────────────────────────
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z);
        RaycastHit hit;
        bool onSurface = Physics.Raycast(rayOrigin, Vector3.down, out hit, groundRayLength, trackLayer);

        if (onSurface)
        {
            groundY     = hit.point.y;
            SlopeNormal = hit.normal;
        }

        // ── Compute target Y ──────────────────────────────────────────────
        float targetY;

        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = jumpTimer / jumpDuration;

            if (t >= 1f)
            {
                isJumping  = false;
                IsGrounded = true;
                targetY    = groundY + groundOffset;
            }
            else
            {
                float arc  = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                IsGrounded = false;
                targetY    = groundY + groundOffset + arc;
            }
        }
        else
        {
            IsGrounded = true;
            targetY    = groundY + groundOffset;
        }

        // ── Apply movement ────────────────────────────────────────────────
        Vector3 pos = transform.position;
        pos.x  = Mathf.Clamp(pos.x + horizontal * lateralSpeed * Time.deltaTime, -lateralBound, lateralBound);
        pos.y  = targetY;
        transform.position = pos;

        // ── Obstacle collision ────────────────────────────────────────────
        Collider[] hits = Physics.OverlapSphere(transform.position, collisionRadius, obstacleLayer);
        if (hits.Length > 0)
            GameManager.Instance.TriggerGameOver();
    }
}
