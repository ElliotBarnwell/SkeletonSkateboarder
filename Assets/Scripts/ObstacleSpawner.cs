using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spikes")]
    public bool        spawnSpikes      = true;
    public GameObject  spikePrefab;
    public float       spikeHalfHeight  = 1.5f;
    public int         spikeCount       = 20;
    public float       spikeStartZ      = 15f;
    public float       spikeEndZ        = 200f;
    public float       spikeMinSpacing  = 3f;

    [Header("Coins")]
    public bool        spawnCoins       = true;
    public GameObject  coinPrefab;
    public int         coinLineCount    = 8;
    public int         coinsPerLine     = 5;
    public float       coinSpacing      = 2f;
    public float       coinHeightAbove  = 0.5f;
    public float       coinSpikeZBuffer = 5f;
    public float       coinLineZSpacing = 15f;
    public float       coinStartZ       = 15f;
    public float       coinEndZ         = 200f;

    [Header("Walls")]
    public bool        spawnWalls       = true;
    public GameObject  wallPrefab;
    public float       wallThickness    = 0.3f;
    public float       wallStartZ       = 50f;
    public float       wallEndZ         = 500f;
    public float       wallZSpacing     = 250f;
    public float       wallClearZBuffer = 10f;

    [Header("Speed Bars")]
    public bool        spawnBars        = true;
    public float       barStartZ        = 5f;
    public float       barEndZ          = 200f;
    public int         barCount         = 15;
    public float       barMinSpacing    = 5f;
    [Tooltip("Thickness of each bar in world units.")]
    public float       barLineWidth     = 0.12f;
    [Tooltip("Bar colour — lower alpha = more subtle.")]
    public Color       barColor         = new Color(1f, 1f, 1f, 0.3f);
    [Tooltip("Optional material. Leave empty to auto-assign a transparent shader.")]
    public Material    barMaterial;
    [Tooltip("How much extra scroll speed each bar adds when the player passes through it.")]
    public float       barBoostAmount   = 10f;

    [Header("Gaps")]
    public bool   spawnGaps      = true;
    public int    gapCount       = 5;
    public float  gapLength      = 4f;
    public float  gapStartZ      = 20f;
    public float  gapEndZ        = 200f;
    public float  gapMinSpacing  = 20f;
    [Tooltip("Must match the layer name used in PlayerController's Obstacle Layer mask.")]
    public string gapLayerName   = "Obstacle";

    [Header("Obstacle Buffers")]
    [Tooltip("Clear zone around each gap that spikes must avoid.")]
    public float gapSpikeBuffer  = 8f;
    [Tooltip("Clear zone around each gap that coins must avoid.")]
    public float gapCoinBuffer   = 8f;
    [Tooltip("Clear zone around each gap that walls must avoid.")]
    public float gapWallBuffer   = 8f;
    [Tooltip("Clear zone around each gap that speed bars must avoid.")]
    public float gapBarBuffer    = 8f;
    [Tooltip("Clear zone around each spike that speed bars must avoid.")]
    public float spikeBarBuffer  = 4f;
    [Tooltip("Clear zone around each coin line that speed bars must avoid.")]
    public float coinBarBuffer   = 4f;

    [Header("References")]
    public HalfPipeExtruder halfPipe;
    public Transform        trackRoot;

    private readonly List<Vector3> spikePositions = new List<Vector3>();
    private readonly List<Vector3> coinPositions  = new List<Vector3>();
    private readonly List<float>   coinLineZs     = new List<float>();
    private readonly List<float>   gapZs          = new List<float>();
    private readonly List<float>   spikeZs        = new List<float>();

    void Start()
    {
        if (trackRoot == null || halfPipe == null)
        {
            Debug.LogError("ObstacleSpawner: trackRoot or halfPipe not assigned.");
            return;
        }

        SplineContainer sc = halfPipe.splineContainer;
        if (sc == null)
        {
            Debug.LogError("ObstacleSpawner: HalfPipeExtruder has no SplineContainer.");
            return;
        }

        spikePositions.Clear();
        coinPositions.Clear();
        coinLineZs.Clear();
        gapZs.Clear();
        spikeZs.Clear();

        Spline spline    = sc.Spline;
        float  halfFlat  = halfPipe.flatBottomWidth * 0.5f;
        float  radius    = halfPipe.radius;
        float  splineLen = spline.GetLength();
        float  minSqr    = spikeMinSpacing * spikeMinSpacing;

        const int maxAttempts = 20;

        // ── Gaps (highest priority — spawned first) ───────────────────────
        if (spawnGaps)
            SpawnGaps(sc, spline, splineLen, halfFlat, radius);

        // ── Spikes ────────────────────────────────────────────────────────
        if (spawnSpikes)
        {
            if (spikePrefab == null)
                Debug.LogWarning("ObstacleSpawner: spikePrefab not assigned, skipping spikes.");

            float spikeTMin = Mathf.Clamp01(spikeStartZ / splineLen);
            float spikeTMax = Mathf.Clamp01(spikeEndZ   / splineLen);

            for (int i = 0; i < spikeCount; i++)
            {
                if (spikePrefab == null) break;

                float t = Random.Range(spikeTMin, spikeTMax);

                float3  p3  = spline.EvaluatePosition(t);
                float3  tan = math.normalizesafe(spline.EvaluateTangent(t));
                Vector3 pos = new Vector3(p3.x, p3.y, p3.z);
                Vector3 fwd = new Vector3(tan.x, tan.y, tan.z);

                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 up    = Vector3.Cross(fwd, right).normalized;

                int     section = Random.Range(0, 3);
                Vector3 crossOffset, normal;

                if (section == 0)
                {
                    float x = Random.Range(-halfFlat, halfFlat);
                    crossOffset = right * x;
                    normal      = up;
                }
                else
                {
                    float baseX    = section == 2 ? halfFlat : -halfFlat;
                    float minAngle = section == 2 ? 270f : 180f;
                    float maxAngle = section == 2 ? 360f : 270f;
                    float angle    = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
                    float cosA     = Mathf.Cos(angle);
                    float sinA     = Mathf.Sin(angle);

                    crossOffset = right * (baseX + cosA * radius) + up * (radius + sinA * radius);
                    normal      = (-right * cosA - up * sinA).normalized;
                }

                Vector3 worldSurface = sc.transform.TransformPoint(pos + crossOffset);
                Vector3 worldNormal  = sc.transform.TransformDirection(normal).normalized;
                Vector3 localSpawn   = trackRoot.InverseTransformPoint(worldSurface + worldNormal * spikeHalfHeight);

                bool tooClose = false;
                foreach (float gz in gapZs)
                {
                    if (Mathf.Abs(worldSurface.z - gz) < gapSpikeBuffer)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                {
                    foreach (Vector3 p in spikePositions)
                    {
                        if ((localSpawn - p).sqrMagnitude < minSqr)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }
                if (tooClose) continue;

                spikePositions.Add(localSpawn);
                spikeZs.Add(worldSurface.z);

                Vector3 localNormal = trackRoot.InverseTransformDirection(worldNormal).normalized;

                GameObject spike = Instantiate(spikePrefab, trackRoot);
                spike.transform.localPosition = localSpawn;
                spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localNormal);
            }
        }

        // ── Coins ─────────────────────────────────────────────────────────
        if (spawnCoins && coinPrefab != null)
        {
            float     coinTMin           = Mathf.Clamp01(coinStartZ / splineLen);
            float     coinTMax           = Mathf.Clamp01(coinEndZ   / splineLen);
            float     coinMinSqr         = coinSpacing * coinSpacing;
            Vector3[] coinLocalPositions = new Vector3[coinsPerLine];

            for (int line = 0; line < coinLineCount; line++)
            {
                bool placed = false;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    float t = Random.Range(coinTMin, coinTMax);

                    float3  p3      = spline.EvaluatePosition(t);
                    float3  tan     = math.normalizesafe(spline.EvaluateTangent(t));
                    Vector3 linePos = new Vector3(p3.x, p3.y, p3.z);
                    Vector3 fwd     = new Vector3(tan.x, tan.y, tan.z);
                    Vector3 right   = Vector3.Cross(Vector3.up, fwd).normalized;
                    Vector3 up      = Vector3.Cross(fwd, right).normalized;

                    float lineCentreZ = sc.transform.TransformPoint(linePos).z;

                    bool lineBlocked = false;

                    foreach (float lz in coinLineZs)
                    {
                        if (Mathf.Abs(lineCentreZ - lz) < coinLineZSpacing)
                        {
                            lineBlocked = true;
                            break;
                        }
                    }
                    if (lineBlocked) continue;

                    foreach (float gz in gapZs)
                    {
                        if (Mathf.Abs(lineCentreZ - gz) < gapCoinBuffer)
                        {
                            lineBlocked = true;
                            break;
                        }
                    }
                    if (lineBlocked) continue;

                    int     section = Random.Range(0, 3);
                    Vector3 crossOffset, coinNormal;

                    if (section == 0)
                    {
                        float x = Random.Range(-halfFlat * 0.75f, halfFlat * 0.75f);
                        crossOffset = right * x;
                        coinNormal  = up;
                    }
                    else
                    {
                        float baseX    = section == 2 ? halfFlat : -halfFlat;
                        float minAngle = section == 2 ? 270f : 180f;
                        float maxAngle = section == 2 ? 360f : 270f;
                        float angle    = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
                        float cosA     = Mathf.Cos(angle);
                        float sinA     = Mathf.Sin(angle);

                        crossOffset = right * (baseX + cosA * radius) + up * (radius + sinA * radius);
                        coinNormal  = (-right * cosA - up * sinA).normalized;
                    }

                    for (int c = 0; c < coinsPerLine; c++)
                    {
                        float   zOffset  = (c - (coinsPerLine - 1) * 0.5f) * coinSpacing;
                        Vector3 scLocal  = linePos + fwd * zOffset + crossOffset + coinNormal * coinHeightAbove;
                        Vector3 worldPos = sc.transform.TransformPoint(scLocal);
                        Vector3 localPos = trackRoot.InverseTransformPoint(worldPos);

                        foreach (Vector3 sp in spikePositions)
                        {
                            if (Mathf.Abs(worldPos.z - trackRoot.TransformPoint(sp).z) < coinSpikeZBuffer)
                            {
                                lineBlocked = true;
                                break;
                            }
                        }
                        if (lineBlocked) break;

                        foreach (Vector3 cp in coinPositions)
                        {
                            if ((localPos - cp).sqrMagnitude < coinMinSqr)
                            {
                                lineBlocked = true;
                                break;
                            }
                        }
                        if (lineBlocked) break;

                        coinLocalPositions[c] = localPos;
                    }

                    if (lineBlocked) continue;

                    Quaternion coinRot = Quaternion.LookRotation(coinNormal, fwd);
                    coinLineZs.Add(lineCentreZ);

                    for (int c = 0; c < coinsPerLine; c++)
                    {
                        coinPositions.Add(coinLocalPositions[c]);
                        GameObject coin = Instantiate(coinPrefab, trackRoot);
                        coin.transform.localPosition = coinLocalPositions[c];
                        coin.transform.localRotation = coinRot;
                    }

                    placed = true;
                    break;
                }

                if (!placed)
                    Debug.LogWarning($"ObstacleSpawner: could not place coin line {line} after {maxAttempts} attempts.");
            }
        }
        else if (spawnCoins && coinPrefab == null)
        {
            Debug.LogWarning("ObstacleSpawner: coinPrefab not assigned, skipping coins.");
        }

        // ── Walls ─────────────────────────────────────────────────────────
        if (spawnWalls)
        {
            if (wallPrefab == null)
            {
                Debug.LogWarning("ObstacleSpawner: wallPrefab not assigned, skipping walls.");
            }
            else
            {
                float wallTMin   = Mathf.Clamp01(wallStartZ / splineLen);
                float wallTMax   = Mathf.Clamp01(wallEndZ   / splineLen);
                int   wallCount  = Mathf.Max(1, Mathf.FloorToInt((wallEndZ - wallStartZ) / wallZSpacing));
                float wallWidth  = halfFlat * 2f + radius * 2f;
                float wallHeight = radius;

                List<float> wallZs = new List<float>();

                for (int w = 0; w < wallCount; w++)
                {
                    bool wallPlaced = false;

                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        float t = Random.Range(wallTMin, wallTMax);

                        float3  p3    = spline.EvaluatePosition(t);
                        float3  tan   = math.normalizesafe(spline.EvaluateTangent(t));
                        Vector3 pos   = new Vector3(p3.x, p3.y, p3.z);
                        Vector3 fwd   = new Vector3(tan.x, tan.y, tan.z);
                        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                        Vector3 up    = Vector3.Cross(fwd, right).normalized;

                        Vector3 worldPos = sc.transform.TransformPoint(pos + up * (wallHeight * 0.5f));
                        float   worldZ   = worldPos.z;

                        bool blocked = false;

                        foreach (float wz in wallZs)
                        {
                            if (Mathf.Abs(worldZ - wz) < wallZSpacing)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) continue;

                        foreach (Vector3 sp in spikePositions)
                        {
                            if (Mathf.Abs(worldZ - trackRoot.TransformPoint(sp).z) < wallClearZBuffer)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) continue;

                        foreach (Vector3 cp in coinPositions)
                        {
                            if (Mathf.Abs(worldZ - trackRoot.TransformPoint(cp).z) < wallClearZBuffer)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) continue;

                        foreach (float gz in gapZs)
                        {
                            if (Mathf.Abs(worldZ - gz) < gapWallBuffer)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) continue;

                        wallZs.Add(worldZ);

                        Vector3    localPos  = trackRoot.InverseTransformPoint(worldPos);
                        Vector3    worldFwd  = sc.transform.TransformDirection(fwd).normalized;
                        Vector3    worldUp   = sc.transform.TransformDirection(up).normalized;
                        Quaternion localRot  = Quaternion.Inverse(trackRoot.rotation)
                                             * Quaternion.LookRotation(worldFwd, worldUp);

                        GameObject wall = Instantiate(wallPrefab, trackRoot);
                        wall.transform.localPosition = localPos;
                        wall.transform.localRotation = localRot;
                        wall.transform.localScale    = new Vector3(wallWidth, wallHeight, wallThickness);

                        wallPlaced = true;
                        break;
                    }

                    if (!wallPlaced)
                        Debug.LogWarning($"ObstacleSpawner: could not place wall {w} after {maxAttempts} attempts.");
                }
            }
        }

        // ── Speed Bars ────────────────────────────────────────────────────
        if (spawnBars)
            SpawnSpeedBars(sc, spline, splineLen, halfFlat, radius);
    }

    void SpawnGaps(SplineContainer sc, Spline spline, float splineLen, float halfFlat, float radius)
    {
        const int maxAttempts  = 20;
        int       gapLayer     = LayerMask.NameToLayer(gapLayerName);
        if (gapLayer < 0)
        {
            Debug.LogWarning($"ObstacleSpawner: layer '{gapLayerName}' not found — gap colliders won't kill the player. " +
                              "Set Gap Layer Name to match your obstacle layer.");
            gapLayer = 0;
        }

        float      gapTMin  = Mathf.Clamp01(gapStartZ / splineLen);
        float      gapTMax  = Mathf.Clamp01(gapEndZ   / splineLen);
        List<float>                    placedZs  = new List<float>();
        List<HalfPipeExtruder.GapRange> gapRanges = new List<HalfPipeExtruder.GapRange>();
        float halfGapT = (gapLength * 0.5f) / splineLen;

        for (int g = 0; g < gapCount; g++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float t = Random.Range(gapTMin, gapTMax);

                float3  p3  = spline.EvaluatePosition(t);
                float3  tan = math.normalizesafe(spline.EvaluateTangent(t));
                Vector3 pos = new Vector3(p3.x, p3.y, p3.z);
                Vector3 fwd = new Vector3(tan.x, tan.y, tan.z);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 up    = Vector3.Cross(fwd, right).normalized;

                Vector3 worldPos = sc.transform.TransformPoint(pos);

                bool tooClose = false;
                foreach (float pz in placedZs)
                {
                    if (Mathf.Abs(worldPos.z - pz) < gapMinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                Vector3 localFwd = trackRoot.InverseTransformDirection(
                                       sc.transform.TransformDirection(fwd)).normalized;
                Vector3 localUp  = trackRoot.InverseTransformDirection(
                                       sc.transform.TransformDirection(up)).normalized;
                Vector3 localPos = trackRoot.InverseTransformPoint(worldPos);

                placedZs.Add(worldPos.z);
                gapZs.Add(worldPos.z);
                gapRanges.Add(new HalfPipeExtruder.GapRange {
                    startT = Mathf.Clamp01(t - halfGapT),
                    endT   = Mathf.Clamp01(t + halfGapT)
                });

                // ── Root ─────────────────────────────────────────────────
                GameObject gap = new GameObject("Gap");
                gap.transform.SetParent(trackRoot, worldPositionStays: false);
                gap.transform.localPosition = localPos;
                gap.transform.localRotation = Quaternion.LookRotation(localFwd, localUp);

                // ── Kill zone collider across the flat bottom ─────────────
                // Spans only the flat section (not the curved walls) so the
                // player can ride the walls safely and must jump on the floor.
                GameObject killZone = new GameObject("GapKillZone");
                killZone.transform.SetParent(gap.transform, worldPositionStays: false);
                killZone.transform.localPosition = new Vector3(0f, -(radius + 1f), 0f);
                killZone.layer = gapLayer;
                killZone.AddComponent<Obstacle>();
                BoxCollider bc = killZone.AddComponent<BoxCollider>();
                bc.size        = new Vector3(halfFlat * 2f, 0.5f, gapLength);

                placed = true;
                break;
            }

            if (!placed)
                Debug.LogWarning($"ObstacleSpawner: could not place gap {g} after {maxAttempts} attempts.");
        }

        // Rebuild the pipe mesh with holes cut where the gaps are.
        halfPipe.SetGapRanges(gapRanges);
    }

    void SpawnSpeedBars(SplineContainer sc, Spline spline, float splineLen, float halfFlat, float radius)
    {
        const int maxAttempts = 20;
        Material  mat     = ResolveBarMaterial();
        Vector3[] profile = BuildBarProfile(halfFlat, radius, halfPipe.curveSegments);

        List<float> placedZs = new List<float>();

        for (int b = 0; b < barCount; b++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Randomise directly in world-Z space so bars are uniformly
                // distributed regardless of spline parameterisation.
                float targetZ = Random.Range(barStartZ, barEndZ);
                float t       = Mathf.Clamp01(targetZ / splineLen);

                bool tooClose = false;
                foreach (float pz in placedZs)
                {
                    if (Mathf.Abs(targetZ - pz) < barMinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                foreach (float gz in gapZs)
                {
                    if (Mathf.Abs(targetZ - gz) < gapBarBuffer) { tooClose = true; break; }
                }
                if (!tooClose)
                {
                    foreach (float sz in spikeZs)
                    {
                        if (Mathf.Abs(targetZ - sz) < spikeBarBuffer) { tooClose = true; break; }
                    }
                }
                if (!tooClose)
                {
                    foreach (float cz in coinLineZs)
                    {
                        if (Mathf.Abs(targetZ - cz) < coinBarBuffer) { tooClose = true; break; }
                    }
                }
                if (tooClose) continue;

                float3  p3    = spline.EvaluatePosition(t);
                float3  tan   = math.normalizesafe(spline.EvaluateTangent(t));
                Vector3 pos   = new Vector3(p3.x, p3.y, p3.z);
                Vector3 fwd   = new Vector3(tan.x, tan.y, tan.z);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 up    = Vector3.Cross(fwd, right).normalized;

                Vector3 localFwd = trackRoot.InverseTransformDirection(
                                       sc.transform.TransformDirection(fwd)).normalized;
                Vector3 localUp  = trackRoot.InverseTransformDirection(
                                       sc.transform.TransformDirection(up)).normalized;
                Vector3 localPos = trackRoot.InverseTransformPoint(
                                       sc.transform.TransformPoint(pos));

                placedZs.Add(targetZ);

                GameObject bar = new GameObject("SpeedBar");
                bar.transform.SetParent(trackRoot, worldPositionStays: false);
                bar.transform.localPosition = localPos;
                bar.transform.localRotation = Quaternion.LookRotation(localFwd, localUp);

                LineRenderer lr      = bar.AddComponent<LineRenderer>();
                lr.useWorldSpace     = false;
                lr.positionCount     = profile.Length;
                lr.SetPositions(profile);
                lr.startWidth        = barLineWidth;
                lr.endWidth          = barLineWidth;
                lr.startColor        = barColor;
                lr.endColor          = barColor;
                lr.loop              = false;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows    = false;
                lr.sharedMaterial    = mat;

                var boost        = bar.AddComponent<SpeedBoostTrigger>();
                boost.boostAmount = barBoostAmount;

                placed = true;
                break;
            }

            if (!placed)
                Debug.LogWarning($"ObstacleSpawner: could not place speed bar {b} after {maxAttempts} attempts.");
        }
    }

    // Replicates HalfPipeExtruder's profile in Vector3 (z = 0), deduplicating
    // the joints between the arcs and the flat bottom.
    Vector3[] BuildBarProfile(float halfFlat, float radius, int curveSegs)
    {
        var pts = new List<Vector3>();

        // Left arc: 180° → 270°  (top-left lip → flat-bottom left corner)
        for (int i = 0; i <= curveSegs; i++)
        {
            float a = Mathf.Lerp(180f, 270f, (float)i / curveSegs) * Mathf.Deg2Rad;
            pts.Add(new Vector3(
                -halfFlat + Mathf.Cos(a) * radius,
                 Mathf.Sin(a) * radius + radius,
                 0f));
        }

        // Arc ends at (-halfFlat, 0). Add only the right end of the flat bottom
        // to avoid a duplicate at the left joint.
        pts.Add(new Vector3(halfFlat, 0f, 0f));

        // Right arc: 270° → 360°  (flat-bottom right corner → top-right lip).
        // Start at i = 1 to skip the duplicate (halfFlat, 0) at the left joint.
        for (int i = 1; i <= curveSegs; i++)
        {
            float a = Mathf.Lerp(270f, 360f, (float)i / curveSegs) * Mathf.Deg2Rad;
            pts.Add(new Vector3(
                 halfFlat + Mathf.Cos(a) * radius,
                 Mathf.Sin(a) * radius + radius,
                 0f));
        }

        return pts.ToArray();
    }

    Material ResolveBarMaterial()
    {
        if (barMaterial != null)
            return barMaterial;

        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                     ?? Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader == null)
        {
            Debug.LogWarning("[ObstacleSpawner] Could not find a transparent shader for speed bars. " +
                             "Assign a Material to the Bar Material field manually.");
            return null;
        }

        return new Material(shader) { color = barColor };
    }
}
