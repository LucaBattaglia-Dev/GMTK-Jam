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

    [System.Serializable]
    public struct BuildingConfig
    {
        public GameObject prefab;
        public Vector3 rotation;
        public Vector3 offset;
        [Tooltip("Length of the building along the Z axis (in tile units, e.g., 3 = 3 tiles long)")]
        public float lengthInTiles;
        [Tooltip("Width/depth of the building along the X axis (in tile units)")]
        public float widthInTiles;
    }

    [Header("Player Reference")]
    [Tooltip("Drag your Player transform here (or leave empty to auto-find by 'Player' tag)")]
    [SerializeField] private Transform player;

    [Header("Endless Generation Rules")]
    [Tooltip("How many road segments to maintain ahead of the player")]
    [SerializeField] private int segmentsAhead = 15;

    [Tooltip("Segments further behind than this number will be destroyed (> 12)")]
    [SerializeField] private int despawnDistanceSegments = 12;

    [Header("Building Settings")]
    [Tooltip("Pool of building prefabs with individual sizing, offset, and rotation settings")]
    [SerializeField] private BuildingConfig[] buildingPrefabs = new BuildingConfig[15];

    [Tooltip("Gap space between buildings in tile units (0.25 = a quarter tile space)")]
    [SerializeField] private float buildingSpacingTiles = 0.25f;

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

    // Track active road row segments by Z index
    private readonly Dictionary<int, GameObject> activeSegments = new Dictionary<int, GameObject>();
    private int highestSpawnedIndex = 0;

    // Independent Z position trackers for building placement
    private float nextLeftBuildingZ = 0f;
    private float nextRightBuildingZ = 0f;

    // Prevent duplicate adjacent buildings
    private int lastLeftBuildingIndex = -1;
    private int lastRightBuildingIndex = -1;

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

        UpdateEndlessRoad();
    }

    private void Update()
    {
        if (player == null) return;

        UpdateEndlessRoad();
    }

    private void UpdateEndlessRoad()
    {
        // 1. Calculate player's current Z segment index
        int playerSegmentIndex = Mathf.FloorToInt(player.position.z / segmentLength);

        // 2. Generate new road rows ahead
        int targetMaxSegment = playerSegmentIndex + segmentsAhead;
        for (int z = highestSpawnedIndex; z <= targetMaxSegment; z++)
        {
            if (!activeSegments.ContainsKey(z))
            {
                SpawnRoadRow(z);
            }
        }
        highestSpawnedIndex = Mathf.Max(highestSpawnedIndex, targetMaxSegment);

        // 3. Generate buildings independently ahead on both sides
        float targetMaxZ = targetMaxSegment * segmentLength;

        while (nextLeftBuildingZ < targetMaxZ)
        {
            SpawnBuilding(isLeft: true, ref nextLeftBuildingZ, ref lastLeftBuildingIndex);
        }

        while (nextRightBuildingZ < targetMaxZ)
        {
            SpawnBuilding(isLeft: false, ref nextRightBuildingZ, ref lastRightBuildingIndex);
        }

        // 4. Despawn segments > 12 tiles behind player (cleans up associated buildings automatically)
        List<int> indicesToRemove = new List<int>();
        foreach (var kvp in activeSegments)
        {
            int segmentZ = kvp.Key;

            if (playerSegmentIndex - segmentZ > despawnDistanceSegments)
            {
                Destroy(kvp.Value);
                indicesToRemove.Add(segmentZ);
            }
        }

        foreach (int index in indicesToRemove)
        {
            activeSegments.Remove(index);
        }
    }

    private void SpawnRoadRow(int zIndex)
    {
        GameObject rowParent = new GameObject($"RoadSegment_{zIndex}");
        rowParent.transform.SetParent(transform);

        float zPos = zIndex * segmentLength;
        Vector3 rowOrigin = new Vector3(transform.position.x, transform.position.y, zPos);

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

        activeSegments.Add(zIndex, rowParent);
    }

    private void SpawnBuilding(bool isLeft, ref float currentZ, ref int lastIndex)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        int bgIndex = GetRandomBuildingIndex(lastIndex);
        if (bgIndex == -1) return;
        lastIndex = bgIndex;

        BuildingConfig bConfig = buildingPrefabs[bgIndex];

        // Sanity check to avoid zero or negative dimensions
        float lengthInTiles = bConfig.lengthInTiles > 0 ? bConfig.lengthInTiles : 1f;
        float widthInTiles = bConfig.widthInTiles > 0 ? bConfig.widthInTiles : 1f;

        float buildingLengthUnits = lengthInTiles * segmentLength;
        float spacingUnits = buildingSpacingTiles * segmentLength;

        // Position building center along Z
        float centerZ = currentZ + (buildingLengthUnits * 0.5f);

        // Find corresponding row parent segment for organized hierarchy and cleanup
        int segmentZIndex = Mathf.FloorToInt(currentZ / segmentLength);

        if (activeSegments.TryGetValue(segmentZIndex, out GameObject rowParent))
        {
            float startX = transform.position.x;
            float leftCurbX  = startX - (roadTileWidth * 2.0f) - (curbTileWidth * 0.5f);
            float rightCurbX = startX + (roadTileWidth * 2.0f) + (curbTileWidth * 0.5f);

            float buildingX;
            if (isLeft)
            {
                float farLeftSidewalkX = leftCurbX - (curbTileWidth * 0.5f) - (sidewalkTileWidth * sidewalkCount);
                buildingX = farLeftSidewalkX - (widthInTiles * roadTileWidth * 0.5f);
            }
            else
            {
                float farRightSidewalkX = rightCurbX + (curbTileWidth * 0.5f) + (sidewalkTileWidth * sidewalkCount);
                buildingX = farRightSidewalkX + (widthInTiles * roadTileWidth * 0.5f);
            }

            Vector3 basePosition = new Vector3(buildingX, transform.position.y, centerZ);
            Vector3 finalPosition = basePosition + bConfig.offset;
            Quaternion finalRotation = transform.rotation * Quaternion.Euler(bConfig.rotation);

            if (bConfig.prefab != null)
            {
                Instantiate(bConfig.prefab, finalPosition, finalRotation, rowParent.transform);
            }
        }

        // Advance current Z tracker by the building's length + gap space
        currentZ += buildingLengthUnits + spacingUnits;
    }

    private int GetRandomBuildingIndex(int previousIndex)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return -1;
        if (buildingPrefabs.Length == 1) return 0;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, buildingPrefabs.Length);
        } while (newIndex == previousIndex);

        return newIndex;
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