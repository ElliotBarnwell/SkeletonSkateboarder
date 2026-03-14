using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject[] obstaclePrefabs;

    [Header("Spawn Settings")]
    public float spawnInterval  = 3f;      // Seconds between patterns
    public float spawnAheadZ    = 60f;     // How far ahead to spawn
    public float despawnZ       = -20f;    // Destroy when past this Z

    [Header("Track Settings")]
    public float trackWidth     = 3f;      // Left/right spread of obstacles
    public float heightAbove    = 0.5f;    // How high above track surface to place

    [Header("Raycast")]
    public float raycastFromY   = 20f;     // Cast from this height down to find track
    public LayerMask trackLayer;           // Set to your track layer

    private float     timer = 0f;
    private Transform playerTransform;
    private float     trackScrollSpeed;

    enum Pattern { FullRow, GapLeft, GapMiddle, GapRight, Zigzag }

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Get scroll speed from the track scroller
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

        Pattern pattern = (Pattern)Random.Range(0, System.Enum.GetValues(typeof(Pattern)).Length);
        float   spawnZ  = playerTransform.position.z + spawnAheadZ;

        switch (pattern)
        {
            case Pattern.FullRow:
                TrySpawnAt(-trackWidth, spawnZ);
                TrySpawnAt(0f,          spawnZ);
                TrySpawnAt( trackWidth, spawnZ);
                break;

            case Pattern.GapLeft:
                TrySpawnAt(0f,          spawnZ);
                TrySpawnAt( trackWidth, spawnZ);
                break;

            case Pattern.GapMiddle:
                TrySpawnAt(-trackWidth, spawnZ);
                TrySpawnAt( trackWidth, spawnZ);
                break;

            case Pattern.GapRight:
                TrySpawnAt(-trackWidth, spawnZ);
                TrySpawnAt(0f,          spawnZ);
                break;

            case Pattern.Zigzag:
                TrySpawnAt(-trackWidth, spawnZ);
                TrySpawnAt( trackWidth, spawnZ + 5f);
                TrySpawnAt(-trackWidth, spawnZ + 10f);
                break;
        }
    }

    void TrySpawnAt(float x, float z)
    {
        // Raycast down from above to find exact track surface Y
        Vector3    rayOrigin = new Vector3(x, raycastFromY, z);
        RaycastHit hit;

        bool foundTrack = trackLayer != 0
            ? Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastFromY * 2f, trackLayer)
            : Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastFromY * 2f);

        if (!foundTrack)
        {
            Debug.Log("No track found at X:" + x + " Z:" + z + " — obstacle skipped");
            return;
        }

        // Place obstacle on track surface
        Vector3    spawnPos  = new Vector3(x, hit.point.y + heightAbove, z);
        GameObject prefab    = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacle  = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Add scroller so it moves with the track
        TrackScroller scroller    = obstacle.AddComponent<TrackScroller>();
        scroller.scrollSpeed      = trackScrollSpeed;

        obstacle.tag = "Obstacle";
    }

    void CleanupOldObstacles()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obs in obstacles)
        {
            if (obs != null && obs.transform.position.z < despawnZ)
                Destroy(obs);
        }
    }
}