using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class CurveTrackBuilder : MonoBehaviour
{
    void Awake()
    {
        SplineContainer container = GetComponent<SplineContainer>();
        Spline spline = container.Spline;
        spline.Clear();

        // 4 corners of a simple NASCAR oval
        // Straights run along Z axis, 150m long
        // Track is 80m wide
        Vector3[] corners = new Vector3[]
        {
            new Vector3( 0,  0,   0),   // Bottom Left
            new Vector3( 0,  0, 150),   // Top Left
            new Vector3(80,  0, 150),   // Top Right
            new Vector3(80,  0,   0),   // Bottom Right
        };

        foreach (Vector3 pos in corners)
        {
            spline.Add(new BezierKnot(new float3(pos.x, pos.y, pos.z)), 
                       TangentMode.AutoSmooth);
        }

        spline.Closed = true;

        Debug.Log("curve track spline built with " + spline.Count + " knots.");
    }
}