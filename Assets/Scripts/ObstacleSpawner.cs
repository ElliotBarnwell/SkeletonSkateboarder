using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spikes")]
    public GameObject spikePrefab;
    public float      spikeHalfHeight  = 1.5f;
    public int        spikeCount       = 20;
    public float      spikeStartZ      = 15f;
    public float      spikeEndZ        = 200f;
    public float      spikeMinSpacing  = 3f;

    [Header("Coins")]
    public GameObject coinPrefab;
    public int        coinLineCount    = 8;
    public int        coinsPerLine     = 5;
    public float      coinSpacing      = 2f;
    public float      coinHeightAbove  = 0.5f;
    public float      coinSpikeZBuffer = 5f;
    public float      coinLineZSpacing = 15f;
    public float      coinStartZ       = 15f;
    public float      coinEndZ         = 200f;

    [Header("Walls")]
    public GameObject wallPrefab;
    public float      wallThickness    = 0.3f;
    public float      wallStartZ       = 50f;
    public float      wallEndZ         = 500f;
    public float      wallZSpacing     = 250f;
    public float      wallClearZBuffer = 10f;

    [Header("References")]
    public HalfPipeExtruder halfPipe;
    public Transform        trackRoot;

    private readonly List<Vector3> spikePositions = new List<Vector3>();
    private readonly List<Vector3> coinPositions  = new List<Vector3>();
    private readonly List<float>   coinLineZs     = new List<float>();

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

        Spline spline    = sc.Spline;
        float  halfFlat  = halfPipe.flatBottomWidth * 0.5f;
        float  radius    = halfPipe.radius;
        float  splineLen = spline.GetLength();
        float  minSqr    = spikeMinSpacing * spikeMinSpacing;

        const int maxAttempts = 20;

        // ── Spikes ────────────────────────────────────────────────────────
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
            foreach (Vector3 p in spikePositions)
            {
                if ((localSpawn - p).sqrMagnitude < minSqr)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            spikePositions.Add(localSpawn);

            Vector3 localNormal = trackRoot.InverseTransformDirection(worldNormal).normalized;

            GameObject spike = Instantiate(spikePrefab, trackRoot);
            spike.transform.localPosition = localSpawn;
            spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localNormal);
        }

        // ── Coins ─────────────────────────────────────────────────────────
        if (coinPrefab == null)
            Debug.LogWarning("ObstacleSpawner: coinPrefab not assigned, skipping coins.");

        if (coinPrefab != null)
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

                    int     section     = Random.Range(0, 3);
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

        // ── Walls ─────────────────────────────────────────────────────────
        if (wallPrefab == null)
        {
            Debug.LogWarning("ObstacleSpawner: wallPrefab not assigned, skipping walls.");
            return;
        }

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

                // Centre of wall in cross-section: x=0, y=radius/2
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
