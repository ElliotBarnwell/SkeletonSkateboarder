using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject spikePrefab;
    public float      spikeHalfHeight = 1.5f; // half of spike height (3x3x3 spike → 1.5)

    [Header("Spawn Settings")]
    public int   spikeCount = 20;
    public float startZ     = 15f;   // approximate distance along spline to start spawning
    public float endZ       = 200f;  // approximate distance along spline to stop spawning
    public float minSpacing = 3f;

    [Header("References")]
    public HalfPipeExtruder halfPipe;
    public Transform        trackRoot;

    private List<Vector3> placed = new List<Vector3>();

    void Start()
    {
        if (spikePrefab == null || trackRoot == null || halfPipe == null) return;

        SplineContainer sc = halfPipe.splineContainer;
        if (sc == null) return;

        Spline spline  = sc.Spline;
        float halfFlat = halfPipe.flatBottomWidth * 0.5f;
        float radius   = halfPipe.radius;

        float splineLen = spline.GetLength();
        float tMin      = Mathf.Clamp01(startZ / splineLen);
        float tMax      = Mathf.Clamp01(endZ   / splineLen);

        for (int i = 0; i < spikeCount; i++)
        {
            float t = Random.Range(tMin, tMax);

            // Evaluate spline frame at t — matches HalfPipeExtruder's frame construction
            float3 p3, tan3, up3;
            spline.Evaluate(t, out p3, out tan3, out up3);

            Vector3 splinePos = new Vector3(p3.x, p3.y, p3.z);
            Vector3 forward   = new Vector3(tan3.x, tan3.y, tan3.z).normalized;

            // Build right/up frame the same way HalfPipeExtruder does
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up    = Vector3.Cross(forward, right).normalized;

            // Pick section: 0=flat bottom, 1=left wall, 2=right wall
            int section = Random.Range(0, 3);

            Vector3 crossOffset, normal;

            if (section == 0)
            {
                float x = Random.Range(-halfFlat, halfFlat);
                crossOffset = right * x;
                normal      = up;
            }
            else
            {
                float minAngle = section == 2 ? 270f : 180f;
                float maxAngle = section == 2 ? 360f : 270f;
                float angle    = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;

                float cx = (section == 2 ? halfFlat : -halfFlat) + Mathf.Cos(angle) * radius;
                float cy = radius + Mathf.Sin(angle) * radius;

                crossOffset = right * cx + up * cy;
                normal      = (-right * Mathf.Cos(angle) - up * Mathf.Sin(angle)).normalized;
            }

            // Surface position in SplineContainer local space, then to world, then to trackRoot local
            Vector3 scLocalPos    = splinePos + crossOffset;
            Vector3 worldSurface  = sc.transform.TransformPoint(scLocalPos);
            Vector3 worldNormal   = sc.transform.TransformDirection(normal).normalized;
            Vector3 worldSpawn    = worldSurface + worldNormal * spikeHalfHeight;
            Vector3 localSpawn    = trackRoot.InverseTransformPoint(worldSpawn);

            // Skip if too close to another spike
            bool tooClose = false;
            foreach (Vector3 p in placed)
            {
                if (Vector3.Distance(localSpawn, p) < minSpacing)
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
