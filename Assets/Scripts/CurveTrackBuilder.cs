using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SplineContainer))]
public class CurveTrackBuilder : MonoBehaviour
{
    public MapSplineData splineData;

    void Awake()
    {
        Build();
    }

    public void Build()
    {
        if (splineData == null)
        {
            Debug.LogError("CurveTrackBuilder: assign a MapSplineData asset.");
            return;
        }

        SplineContainer container = GetComponent<SplineContainer>();
        Spline           spline   = container.Spline;
        spline.Clear();

        foreach (MapSplineData.KnotData k in splineData.knots)
        {
            float3     pos = new float3(k.position.x, k.position.y, k.position.z);
            Quaternion rot = Quaternion.Euler(k.rotation);
            quaternion q   = new quaternion(rot.x, rot.y, rot.z, rot.w);

            BezierKnot knot;
            TangentMode mode;

            switch (k.mode)
            {
                case MapSplineData.KnotMode.Linear:
                    knot = new BezierKnot(pos, float3.zero, float3.zero, q);
                    mode = TangentMode.Linear;
                    break;

                case MapSplineData.KnotMode.Bezier:
                    // Tangents in knot local space — rotation handles direction
                    float3 tOut = new float3(0f, 0f,  k.tangentLength);
                    float3 tIn  = new float3(0f, 0f, -k.tangentLength);
                    knot = new BezierKnot(pos, tIn, tOut, q);
                    mode = TangentMode.Broken;
                    break;

                default: // Auto
                    knot = new BezierKnot(pos, float3.zero, float3.zero, q);
                    mode = TangentMode.AutoSmooth;
                    break;
            }

            spline.Add(knot, mode);
        }

        spline.Closed = splineData.closed;

        Debug.Log($"CurveTrackBuilder: built spline with {spline.Count} knots.");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CurveTrackBuilder))]
public class CurveTrackBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("BUILD SPLINE", GUILayout.Height(40)))
            ((CurveTrackBuilder)target).Build();
    }
}
#endif
