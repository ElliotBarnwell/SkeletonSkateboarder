using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject spikePrefab;
    public float      spikeHalfHeight = 1.5f;

    [Header("Spawn Settings")]
    public int   spikeCount = 20;
    public float startZ     = 15f;
    public float endZ       = 200f;
    public float minSpacing = 3f;

    [Header("References")]
    public HalfPipeExtruder halfPipe;
    public Transform        trackRoot;

    private readonly List<Vector3> placed = new List<Vector3>();

    void Start()
    {
        if (spikePrefab == null || trackRoot == null || halfPipe == null) return;

        SplineContainer sc = halfPipe.splineContainer;
        if (sc == null) return;

        placed.Clear();

        Spline spline    = sc.Spline;
        float  halfFlat  = halfPipe.flatBottomWidth * 0.5f;
        float  radius    = halfPipe.radius;
        float  splineLen = spline.GetLength();
        float  tMin      = Mathf.Clamp01(startZ / splineLen);
        float  tMax      = Mathf.Clamp01(endZ   / splineLen);
        float  minSqr    = minSpacing * minSpacing;

        for (int i = 0; i < spikeCount; i++)
        {
            float t = Random.Range(tMin, tMax);

            float3  p3  = spline.EvaluatePosition(t);
            float3  tan = math.normalizesafe(spline.EvaluateTangent(t));
            Vector3 pos = new Vector3(p3.x,  p3.y,  p3.z);
            Vector3 fwd = new Vector3(tan.x, tan.y, tan.z);

            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 up    = Vector3.Cross(fwd, right).normalized;

            // section: 0 = flat bottom, 1 = left wall, 2 = right wall
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
            foreach (Vector3 p in placed)
            {
                if ((localSpawn - p).sqrMagnitude < minSqr)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            placed.Add(localSpawn);

            Vector3 localNormal = trackRoot.InverseTransformDirection(worldNormal).normalized;

            GameObject spike = Instantiate(spikePrefab, trackRoot);
            spike.transform.localPosition = localSpawn;
            spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localNormal);
        }
    }
}
