using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HalfPipeExtruder : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer splineContainer;

    [Header("Half-Pipe Shape")]
    public float radius          = 4f;
    public float flatBottomWidth = 4f;

    [Header("Extrusion")]
    public int splineSteps   = 120;
    public int curveSegments = 4;

    void Start()
    {
        BuildMesh();
    }

    public void BuildMesh()
    {
        if (splineContainer == null)
        {
            Debug.LogError("HalfPipeExtruder: Assign your SplineContainer!");
            return;
        }

        Spline spline      = splineContainer.Spline;
        Vector2[] profile  = BuildFlatBottomProfile();
        int profileCount   = profile.Length;

        List<Vector3> vertices  = new List<Vector3>();
        List<int>     triangles = new List<int>();
        List<Vector2> uvs       = new List<Vector2>();

        // ── Rotation minimising frames ────────────────────────────────────
        Vector3[] pos_arr   = new Vector3[splineSteps + 1];
        Vector3[] right_arr = new Vector3[splineSteps + 1];
        Vector3[] up_arr    = new Vector3[splineSteps + 1];
        Vector3[] fwd_arr   = new Vector3[splineSteps + 1];

        for (int i = 0; i <= splineSteps; i++)
        {
            float  t   = (float)i / splineSteps;
            float3 p   = spline.EvaluatePosition(t);
            float3 tan = math.normalizesafe(spline.EvaluateTangent(t));
            pos_arr[i] = new Vector3(p.x, p.y, p.z);
            fwd_arr[i] = new Vector3(tan.x, tan.y, tan.z);
        }

        right_arr[0] = Vector3.Cross(Vector3.up, fwd_arr[0]).normalized;
        up_arr[0]    = Vector3.Cross(fwd_arr[0], right_arr[0]).normalized;

        for (int i = 1; i <= splineSteps; i++)
        {
            Vector3 axis  = Vector3.Cross(fwd_arr[i - 1], fwd_arr[i]);
            float   angle = Vector3.SignedAngle(fwd_arr[i - 1], fwd_arr[i],
                            axis.magnitude > 0.0001f ? axis : Vector3.up);
            Quaternion rot = axis.magnitude > 0.0001f
                           ? Quaternion.AngleAxis(angle, axis)
                           : Quaternion.identity;

            right_arr[i] = rot * right_arr[i - 1];
            up_arr[i]    = Vector3.Cross(fwd_arr[i], right_arr[i]).normalized;
        }

        // ── Extrude ───────────────────────────────────────────────────────
        for (int step = 0; step <= splineSteps; step++)
        {
            for (int p = 0; p < profileCount; p++)
            {
                Vector2 pt       = profile[p];
                Vector3 worldPos = pos_arr[step]
                                 + right_arr[step] * pt.x
                                 + up_arr[step]    * pt.y;
                vertices.Add(worldPos);
                uvs.Add(new Vector2((float)p / (profileCount - 1), (float)step / splineSteps));
            }
        }

        // ── Triangles ─────────────────────────────────────────────────────
        for (int step = 0; step < splineSteps; step++)
        {
            int ringA = step       * profileCount;
            int ringB = (step + 1) * profileCount;

            for (int p = 0; p < profileCount - 1; p++)
            {
                int a = ringA + p; int b = ringA + p + 1;
                int c = ringB + p; int d = ringB + p + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        // ── Build mesh ────────────────────────────────────────────────────
        Mesh mesh        = new Mesh();
        mesh.name        = "FlatBottomHalfPipe";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices    = vertices.ToArray();
        mesh.triangles   = triangles.ToArray();
        mesh.uv          = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

#if UNITY_EDITOR
        // ── Save mesh as permanent asset so collider can always find it ───
        string folder = "Assets/Meshes";
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);

        string path = folder + "/FlatBottomHalfPipe.asset";

        // Overwrite if exists
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        // Assign saved mesh to MeshCollider if one exists
        MeshCollider col = GetComponent<MeshCollider>();
        if (col != null)
        {
            col.sharedMesh = null;
            col.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Debug.Log("Mesh saved to " + path + " and assigned to MeshCollider!");
        }
        else
        {
            Debug.Log("Mesh saved to " + path + " — Add a MeshCollider and assign it manually.");
        }
#endif
    }

    Vector2[] BuildFlatBottomProfile()
    {
        List<Vector2> points   = new List<Vector2>();
        float         halfFlat = flatBottomWidth * 0.5f;

        for (int i = 0; i <= curveSegments; i++)
        {
            float angle = Mathf.Lerp(180f, 270f, (float)i / curveSegments);
            float rad   = angle * Mathf.Deg2Rad;
            points.Add(new Vector2(
                -halfFlat + Mathf.Cos(rad) * radius,
                 Mathf.Sin(rad) * radius + radius));
        }

        points.Add(new Vector2(-halfFlat, 0f));
        points.Add(new Vector2( halfFlat, 0f));

        for (int i = 0; i <= curveSegments; i++)
        {
            float angle = Mathf.Lerp(270f, 360f, (float)i / curveSegments);
            float rad   = angle * Mathf.Deg2Rad;
            points.Add(new Vector2(
                 halfFlat + Mathf.Cos(rad) * radius,
                 Mathf.Sin(rad) * radius + radius));
        }

        return points.ToArray();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HalfPipeExtruder))]
public class HalfPipeExtruderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("BUILD HALF PIPE", GUILayout.Height(40)))
        {
            ((HalfPipeExtruder)target).BuildMesh();
        }
    }
}
#endif
