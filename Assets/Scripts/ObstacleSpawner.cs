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

    [Header("Coins")]
    public GameObject coinPrefab;
    public int        coinLineCount    = 8;
    public int        coinsPerLine     = 5;
    public float      coinSpacing      = 2f;
    public float      coinHeightAbove  = 0.5f;
    public float      coinSpikeZBuffer = 5f;   // min Z distance from any spike

    [Header("Spawn Settings")]
    public float startZ     = 15f;
    public float endZ       = 200f;
    public float minSpacing = 3f;

    [Header("References")]
    public HalfPipeExtruder halfPipe;
    public Transform        trackRoot;

    private readonly List<Vector3> spikePositions = new List<Vector3>();
    private readonly List<Vector3> coinPositions  = new List<Vector3>();

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

        Spline spline    = sc.Spline;
        float  halfFlat  = halfPipe.flatBottomWidth * 0.5f;
        float  radius    = halfPipe.radius;
        float  splineLen = spline.GetLength();
        float  tMin      = Mathf.Clamp01(startZ / splineLen);
        float  tMax      = Mathf.Clamp01(endZ   / splineLen);
        float  minSqr    = minSpacing * minSpacing;

        // ── Spikes ────────────────────────────────────────────────────────
        if (spikePrefab == null)
            Debug.LogWarning("ObstacleSpawner: spikePrefab not assigned, skipping spikes.");

        for (int i = 0; i < spikeCount; i++)
        {
            if (spikePrefab == null) break;

            float t = Random.Range(tMin, tMax);

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
        {
            Debug.LogWarning("ObstacleSpawner: coinPrefab not assigned, skipping coins.");
            return;
        }

        float     coinMinSqr         = coinSpacing * coinSpacing;
        Vector3[] coinLocalPositions = new Vector3[coinsPerLine];

        const int maxAttempts = 20;

        for (int line = 0; line < coinLineCount; line++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float t = Random.Range(tMin, tMax);

                float3  p3      = spline.EvaluatePosition(t);
                float3  tan     = math.normalizesafe(spline.EvaluateTangent(t));
                Vector3 linePos = new Vector3(p3.x, p3.y, p3.z);
                Vector3 fwd     = new Vector3(tan.x, tan.y, tan.z);
                Vector3 right   = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 up      = Vector3.Cross(fwd, right).normalized;

                float x = Random.Range(-halfFlat * 0.75f, halfFlat * 0.75f);

                bool lineBlocked = false;

                for (int c = 0; c < coinsPerLine; c++)
                {
                    float   zOffset  = (c - (coinsPerLine - 1) * 0.5f) * coinSpacing;
                    Vector3 scLocal  = linePos + fwd * zOffset + right * x + up * coinHeightAbove;
                    Vector3 worldPos = sc.transform.TransformPoint(scLocal);
                    Vector3 localPos = trackRoot.InverseTransformPoint(worldPos);

                    foreach (Vector3 sp in spikePositions)
                    {
                        Vector3 spikeWorld = trackRoot.TransformPoint(sp);
                        if (Mathf.Abs(worldPos.z - spikeWorld.z) < coinSpikeZBuffer)
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

                for (int c = 0; c < coinsPerLine; c++)
                {
                    coinPositions.Add(coinLocalPositions[c]);
                    GameObject coin = Instantiate(coinPrefab, trackRoot);
                    coin.transform.localPosition = coinLocalPositions[c];
                    coin.transform.localRotation = coinPrefab.transform.localRotation;
                }

                placed = true;
                break;
            }

            if (!placed)
                Debug.LogWarning($"ObstacleSpawner: could not place coin line {line} after {maxAttempts} attempts.");
        }
    }
}
