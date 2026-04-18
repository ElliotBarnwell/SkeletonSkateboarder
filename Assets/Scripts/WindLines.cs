using UnityEngine;

/// Spawns a sphere of speed-streak particles around the camera that stream
/// backward, selling the illusion of high velocity.
///
/// Setup:
///   1. Add this component to the Camera GameObject (the one with CameraFollow).
///   2. Unity will auto-add a ParticleSystem — leave it at defaults.
///   3. Optionally assign a particle Material; if empty the script finds one automatically.
[RequireComponent(typeof(ParticleSystem))]
public class WindLines : MonoBehaviour
{
    [Header("Spawn Shape")]
    [Tooltip("Radius of the sphere shell spawned around the spawn point.")]
    public float spawnRadius = 6f;
    [Tooltip("Units in front of the camera to centre the spawn sphere. " +
             "Pushes streaks ahead so they fly toward and past the lens.")]
    public float forwardOffset = 8f;

    [Header("Streaks")]
    [Tooltip("How fast streaks fly backward (units/sec). 22 is slightly faster than the 18 u/s track scroll.")]
    public float streakSpeed    = 22f;
    [Tooltip("Seconds each streak is visible.")]
    public float streakLifetime = 0.45f;
    [Tooltip("Cross-section thickness of each streak.")]
    public float streakWidth    = 0.05f;
    [Tooltip("Length multiplier for the stretched billboard — higher = longer streaks.")]
    public float streakLength   = 5f;

    [Header("Emission")]
    [Tooltip("Streaks spawned per second.")]
    public int emissionRate = 80;

    [Header("Appearance")]
    [Tooltip("Soft blue-white reads well against dark tracks.")]
    public Color streakColor = new Color(0.85f, 0.95f, 1f, 1f);
    [Tooltip("Particle material. Leave empty to auto-assign from available shaders.")]
    public Material lineMaterial;

    private ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        Configure();
        _ps.Play();
    }

    void Configure()
    {
        // ── Main ─────────────────────────────────────────────────────────────
        var main             = _ps.main;
        main.loop            = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // stay in world as camera moves
        main.startLifetime   = streakLifetime;
        main.startSpeed      = 0f;       // velocity controlled entirely by VelocityOverLifetime
        main.startSize       = streakWidth;
        main.startColor      = streakColor;
        main.gravityModifier = 0f;
        main.maxParticles    = 400;

        // ── Emission ──────────────────────────────────────────────────────────
        var emission          = _ps.emission;
        emission.rateOverTime = emissionRate;

        // ── Shape: sphere shell offset forward in front of the camera ─────────
        // forwardOffset pushes the spawn centre toward the player so particles
        // spawn ahead of the lens and rush backward past it, filling the screen.
        // Shell-only emission (radiusThickness = 0) keeps streaks near the edges.
        var shape             = _ps.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Sphere;
        shape.radius          = spawnRadius;
        shape.radiusThickness = 0f;
        shape.position        = new Vector3(0f, 0f, forwardOffset); // local-space forward

        // ── Velocity over Lifetime: constant world-space rush backward ─────────
        var vel     = _ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = 0f;
        vel.y       = 0f;
        vel.z       = -streakSpeed;

        // ── Color over Lifetime: snap in, hold, then fade out ─────────────────
        var col      = _ps.colorOverLifetime;
        col.enabled  = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(1f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = gradient;

        // ── Renderer: stretched billboard elongated along velocity ─────────────
        var rend               = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode        = ParticleSystemRenderMode.Stretch;
        rend.velocityScale     = 0f;   // length from lengthScale, not runtime velocity
        rend.lengthScale       = streakLength;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        // Prefer user-supplied material, then try shader fallbacks for
        // Built-in RP, URP, and Legacy pipelines.
        if (lineMaterial != null)
        {
            rend.sharedMaterial = lineMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader != null)
                rend.sharedMaterial = new Material(shader);
            else
                Debug.LogWarning("[WindLines] Could not find a particle shader automatically. " +
                                 "Assign a Material to the WindLines component manually.");
        }
    }
}
