using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject[] obstaclePrefabs;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public float spawnAheadZ  = 60f;

    [Header("Flat Bottom Slots")]
    public float flatBottomWidth = 10f;  // Match to HalfPipeExtruder.flatBottomWidth
    public float heightAbove     = 0.5f;
    public int   maxBlockedSlots = 3;    // 1-3 — always leaves at least 1 open out of 4

    [Header("Wall Slots")]
    public bool  spawnOnWalls    = true;
    public float wallSlotX       = 8f;    // Inner wall lane X — must be > flatBottomWidth/2
    public float wallSlotX2      = 10f;   // Outer wall lane X — further up the curve
    public int   wallSlotsToBlock = 1;    // 0–4 wall slots blocked per wave

    [Header("Raycast")]
    public float raycastFromY = 60f;
    public LayerMask trackLayer;

    private float     timer = 0f;
    private Transform playerTransform;
    private float     trackScrollSpeed;

    private const int FLAT_SLOT_COUNT = 4;
    private const int WALL_SLOT_COUNT = 4;  // inner-left, outer-left, inner-right, outer-right

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        TrackScroller scroller = FindAnyObjectByType<TrackScroller>();
        if (scroller != null)
            trackScrollSpeed = scroller.scrollSpeed;
    }

    void Update()
    {
        if (playerTransform == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnPattern();
        }

        CleanupOldObstacles();
    }

    void SpawnPattern()
    {
        if (obstaclePrefabs.Length == 0) return;

        float spawnZ = playerTransform.position.z + spawnAheadZ;

        // ── Flat bottom slots ─────────────────────────────────────────────
        float half = flatBottomWidth * 0.5f;
        float step = (half * 0.75f * 2f) / (FLAT_SLOT_COUNT - 1);
        float[] flatSlotX = new float[FLAT_SLOT_COUNT]
        {
            -half * 0.75f,
            -half * 0.75f + step,
             half * 0.75f - step,
             half * 0.75f
        };

        int   blocked   = Mathf.Clamp(maxBlockedSlots, 1, FLAT_SLOT_COUNT - 1);
        int[] flatOrder = ShuffledIndices(FLAT_SLOT_COUNT);
        for (int i = 0; i < blocked; i++)
            TrySpawnAt(flatSlotX[flatOrder[i]], spawnZ);

        // ── Wall slots ────────────────────────────────────────────────────
        if (spawnOnWalls && wallSlotsToBlock > 0)
        {
            float[] wallPositions = new float[WALL_SLOT_COUNT] { -wallSlotX2, -wallSlotX, wallSlotX, wallSlotX2 };
            int     wallBlocked   = Mathf.Clamp(wallSlotsToBlock, 1, WALL_SLOT_COUNT);
            int[]   wallOrder     = ShuffledIndices(WALL_SLOT_COUNT);
            for (int i = 0; i < wallBlocked; i++)
                TrySpawnAt(wallPositions[wallOrder[i]], spawnZ);
        }
    }

    void TrySpawnAt(float x, float z)
    {
        Vector3    rayOrigin = new Vector3(x, raycastFromY, z);
        RaycastHit hit;

        bool foundTrack = trackLayer != 0
            ? Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastFromY * 2f, trackLayer)
            : Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastFromY * 2f);

        if (!foundTrack) return;

        // Align to surface normal so spike stands perpendicular to wall/floor
        Quaternion rot      = Quaternion.FromToRotation(Vector3.up, hit.normal);
        Vector3    spawnPos = hit.point + hit.normal * heightAbove;

        GameObject prefab   = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacle = Instantiate(prefab, spawnPos, rot);

        TrackScroller scroller = obstacle.AddComponent<TrackScroller>();
        scroller.scrollSpeed   = trackScrollSpeed;

        obstacle.tag   = "Obstacle";
        obstacle.layer = LayerMask.NameToLayer("Obstacles");
    }

    int[] ShuffledIndices(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }

    void CleanupOldObstacles()
    {
        float destroyBehind  = playerTransform.position.z - 10f;
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obs in obstacles)
        {
            if (obs != null && obs.transform.position.z < destroyBehind)
                Destroy(obs);
        }
    }
}
