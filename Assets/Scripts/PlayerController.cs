using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float lateralSpeed  = 8f;

    [Header("Jump")]
    public float jumpHeight    = 4f;
    public float gravity       = -30f;

    [Header("Slope Handling")]
    public float slopeRayLength = 1.5f;
    public float maxJumpSlope   = 20f;    // Only jump if surface is flatter than this
    public LayerMask trackLayer;

    private CharacterController cc;
    private float   verticalVelocity = 0f;
    private Vector3 slopeNormal      = Vector3.up;
    private float   slopeAngle       = 0f;

    void Start()
    {
        cc                 = GetComponent<CharacterController>();
        cc.slopeLimit      = 60f;
        cc.stepOffset      = 0.3f;
        cc.skinWidth       = 0.08f;
        cc.minMoveDistance = 0f;
    }

    void Update()
    {
        // ── Slope detection ───────────────────────────────────────────────
        RaycastHit hit;
        bool onSlope = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            out hit,
            slopeRayLength,
            trackLayer
        );

        if (onSlope)
        {
            slopeNormal = hit.normal;
            slopeAngle  = Vector3.Angle(slopeNormal, Vector3.up);
        }
        else
        {
            slopeNormal = Vector3.up;
            slopeAngle  = 0f;
        }

        // ── Grounded ──────────────────────────────────────────────────────
        bool grounded = cc.isGrounded;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -4f;

        // ── Jump — flat surface only ───────────────────────────────────────
        bool canJump = grounded && slopeAngle < maxJumpSlope;
        if (canJump && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // ── Gravity ───────────────────────────────────────────────────────
        verticalVelocity += gravity * Time.deltaTime;

        // ── Input ─────────────────────────────────────────────────────────
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal =  1f;

        // ── Move ──────────────────────────────────────────────────────────
        Vector3 move;

        if (grounded && onSlope && verticalVelocity <= 0f)
        {
            // Project onto slope — prevents clipping through ramps
            Vector3 slopeMove = Vector3.ProjectOnPlane(
                new Vector3(horizontal * lateralSpeed, 0f, 0f),
                slopeNormal
            );
            move = new Vector3(slopeMove.x, slopeMove.y - 4f, slopeMove.z);
        }
        else
        {
            move = new Vector3(horizontal * lateralSpeed, verticalVelocity, 0f);
        }

        cc.Move(move * Time.deltaTime);
    }
}