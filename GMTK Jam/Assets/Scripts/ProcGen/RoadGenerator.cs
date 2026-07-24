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

    [Header("Sidewalk Settings (Shared for Left & Right)")]
    [Tooltip("Single sidewalk tile prefab used on both outer ends")]
    [SerializeField] private TileConfig sidewalk;

    [Tooltip("How many sidewalk tiles extend outward on EACH side of the road")]
    [Min(1)]
    [SerializeField] private int sidewalkCount = 1;

    [Tooltip("Width of sidewalk tiles along the X axis")]
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

    [Header("Generation Settings")]
    [SerializeField] private int totalSegments = 10;

    private void Start()
    {
        GenerateRoad();
    }

    [ContextMenu("Generate Road")]
    public void GenerateRoad()
    {
        // Clear existing generated child tiles
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Vector3 startPos = transform.position;

        // Calculate X positions from center (0,0) outward:
        float leftMidX  = startPos.x - (roadTileWidth * 0.5f);
        float rightMidX = startPos.x + (roadTileWidth * 0.5f);

        float leftRoadX  = startPos.x - (roadTileWidth * 1.5f);
        float rightRoadX = startPos.x + (roadTileWidth * 1.5f);

        float leftCurbX  = startPos.x - (roadTileWidth * 2.0f) - (curbTileWidth * 0.5f);
        float rightCurbX = startPos.x + (roadTileWidth * 2.0f) + (curbTileWidth * 0.5f);

        // Generate row by row along the Z axis
        for (int z = 0; z < totalSegments; z++)
        {
            float zPos = startPos.z + (z * segmentLength);

            // 1. Left Sidewalks (Extending outwards to the left)
            for (int s = 0; s < sidewalkCount; s++)
            {
                float leftSidewalkX = leftCurbX - (curbTileWidth * 0.5f) - (sidewalkTileWidth * 0.5f) - (s * sidewalkTileWidth);
                SpawnTile(sidewalk, new Vector3(leftSidewalkX, startPos.y, zPos));
            }

            // 2. Left Curb & Lanes
            SpawnTile(leftCurb, new Vector3(leftCurbX, startPos.y, zPos));
            SpawnTile(leftRoad, new Vector3(leftRoadX, startPos.y, zPos));
            SpawnTile(leftMidRoad, new Vector3(leftMidX, startPos.y, zPos));

            // 3. Right Lanes & Curb
            SpawnTile(rightMidRoad, new Vector3(rightMidX, startPos.y, zPos));
            SpawnTile(rightRoad, new Vector3(rightRoadX, startPos.y, zPos));
            SpawnTile(rightCurb, new Vector3(rightCurbX, startPos.y, zPos));

            // 4. Right Sidewalks (Extending outwards to the right)
            for (int s = 0; s < sidewalkCount; s++)
            {
                float rightSidewalkX = rightCurbX + (curbTileWidth * 0.5f) + (sidewalkTileWidth * 0.5f) + (s * sidewalkTileWidth);
                SpawnTile(sidewalk, new Vector3(rightSidewalkX, startPos.y, zPos));
            }
        }
    }

    private void SpawnTile(TileConfig config, Vector3 basePosition)
    {
        if (config.prefab != null)
        {
            Vector3 finalPosition = basePosition + config.offset;
            Quaternion finalRotation = transform.rotation * Quaternion.Euler(config.rotation);
            Instantiate(config.prefab, finalPosition, finalRotation, transform);
        }
    }
}