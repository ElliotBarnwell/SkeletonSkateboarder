using UnityEngine;

[CreateAssetMenu(fileName = "MapSplineData", menuName = "SkeletonSkateboarder/Map Spline")]
public class MapSplineData : ScriptableObject
{
    public bool closed = false;
    public KnotData[] knots;

    [System.Serializable]
    public class KnotData
    {
        public Vector3 position;
        public Vector3 rotation;
        public KnotMode mode         = KnotMode.Auto;
        public float   tangentLength = 20f;
    }

    public enum KnotMode { Auto, Linear, Bezier }
}
