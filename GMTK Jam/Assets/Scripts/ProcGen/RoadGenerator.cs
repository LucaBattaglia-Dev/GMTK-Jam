using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct TileConfig
    {
        public GameObject prefab;
        public Vector3 rotation;
        public Vector3 offset;
    }

    [Header("Player Reference")]
    [Tooltip("Drag your Player transform here (or leave empty to auto-find by 'Player' tag)")]
    [SerializeField] private Transform player;

    [Header("Endless Generation Rules")]
    [Tooltip("How many road segments to maintain ahead of the player")]
    [SerializeField] private int segmentsAhead = 15;

    [Tooltip("Segments further behind than this number will be destroyed (> 12)")]
    [SerializeField] private int despawnDistanceSegments = 12;

    [Header("Sidewalk Settings (Shared for Left & Right)")]
    [SerializeField] private TileConfig sidewalk;
    [Min(1)]
    [SerializeField] private int sidewalkCount = 1;
    [SerializeField] private float sidewalkTileWidth = 5.0f;

    [Header("Road & Curb Prefabs")]
    [SerializeField] private TileConfig leftCurb;
    [SerializeField] private TileConfig leftRoad;
    [SerializeField] private TileConfig leftMidRoad;   // Left road with yellow center line
    [SerializeField] private TileConfig rightMidRoad;  // Right road with yellow center line
    [SerializeField] private TileConfig rightRoad;
    [SerializeField] private TileConfig rightCurb;

    [Header("Tile Dimensions")]
    [Tooltip("Length of each tile piece along the Z axis (forward)")]
    [SerializeField] private float segmentLength = 5.0f;

    [Tooltip("Width of standard road lane tiles along the X axis")]
    [SerializeField] private float roadTileWidth = 5.0f;

    [Tooltip("Width of curb tiles along the X axis")]
    [SerializeField] private float curbTileWidth = 5.0f;

    // Track active segments by their Z index
    private readonly Dictionary<int, GameObject> activeSegments = new Dictionary<int, GameObject>();
    private int highestSpawnedIndex = 0;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) 
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("RoadGenerator: No Player assigned or tagged as 'Player'.");
                return;
            }
        }

        // Initialize road around starting position
        UpdateEndlessRoad();
    }

    private void Update()
    {
        if (player == null) return;

        UpdateEndlessRoad();
    }

    private void UpdateEndlessRoad()
    {
        // 1. Calculate which Z segment index the player is currently inside
        int playerSegmentIndex = Mathf.FloorToInt(player.position.z / segmentLength);

        // 2. Generate new segments ahead
        int targetMaxSegment = playerSegmentIndex + segmentsAhead;
        for (int z = highestSpawnedIndex; z <= targetMaxSegment; z++)
        {
            if (!activeSegments.ContainsKey(z))
            {
                SpawnRow(z);
            }
        }
        highestSpawnedIndex = Mathf.Max(highestSpawnedIndex, targetMaxSegment);

        // 3. Destroy segments that fall > 12 segments behind the player
        List<int> indicesToRemove = new List<int>();
        foreach (var kvp in activeSegments)
        {
            int segmentZ = kvp.Key;

            if (playerSegmentIndex - segmentZ > despawnDistanceSegments)
            {
                Destroy(kvp.Value); // Destroys the whole parent row GameObject
                indicesToRemove.Add(segmentZ);
            }
        }

        foreach (int index in indicesToRemove)
        {
            activeSegments.Remove(index);
        }
    }

    private void SpawnRow(int zIndex)
    {
        // Create a single parent object for the entire row to keep the hierarchy clean & make destruction easy
        GameObject rowParent = new GameObject($"RoadSegment_{zIndex}");
        rowParent.transform.SetParent(transform);

        float zPos = zIndex * segmentLength;
        Vector3 rowOrigin = new Vector3(transform.position.x, transform.position.y, zPos);

        // Calculate X offsets from center line outwards
        float leftMidX  = rowOrigin.x - (roadTileWidth * 0.5f);
        float rightMidX = rowOrigin.x + (roadTileWidth * 0.5f);

        float leftRoadX  = rowOrigin.x - (roadTileWidth * 1.5f);
        float rightRoadX = rowOrigin.x + (roadTileWidth * 1.5f);

        float leftCurbX  = rowOrigin.x - (roadTileWidth * 2.0f) - (curbTileWidth * 0.5f);
        float rightCurbX = rowOrigin.x + (roadTileWidth * 2.0f) + (curbTileWidth * 0.5f);

        // 1. Left Sidewalks
        for (int s = 0; s < sidewalkCount; s++)
        {
            float leftSidewalkX = leftCurbX - (curbTileWidth * 0.5f) - (sidewalkTileWidth * 0.5f) - (s * sidewalkTileWidth);
            SpawnTile(sidewalk, new Vector3(leftSidewalkX, rowOrigin.y, zPos), rowParent.transform);
        }

        // 2. Left Curb & Lanes
        SpawnTile(leftCurb, new Vector3(leftCurbX, rowOrigin.y, zPos), rowParent.transform);
        SpawnTile(leftRoad, new Vector3(leftRoadX, rowOrigin.y, zPos), rowParent.transform);
        SpawnTile(leftMidRoad, new Vector3(leftMidX, rowOrigin.y, zPos), rowParent.transform);

        // 3. Right Lanes & Curb
        SpawnTile(rightMidRoad, new Vector3(rightMidX, rowOrigin.y, zPos), rowParent.transform);
        SpawnTile(rightRoad, new Vector3(rightRoadX, rowOrigin.y, zPos), rowParent.transform);
        SpawnTile(rightCurb, new Vector3(rightCurbX, rowOrigin.y, zPos), rowParent.transform);

        // 4. Right Sidewalks
        for (int s = 0; s < sidewalkCount; s++)
        {
            float rightSidewalkX = rightCurbX + (curbTileWidth * 0.5f) + (sidewalkTileWidth * 0.5f) + (s * sidewalkTileWidth);
            SpawnTile(sidewalk, new Vector3(rightSidewalkX, rowOrigin.y, zPos), rowParent.transform);
        }

        // Add to tracking dictionary
        activeSegments.Add(zIndex, rowParent);
    }

    private void SpawnTile(TileConfig config, Vector3 basePosition, Transform parent)
    {
        if (config.prefab != null)
        {
            Vector3 finalPosition = basePosition + config.offset;
            Quaternion finalRotation = transform.rotation * Quaternion.Euler(config.rotation);
            Instantiate(config.prefab, finalPosition, finalRotation, parent);
        }
    }
}