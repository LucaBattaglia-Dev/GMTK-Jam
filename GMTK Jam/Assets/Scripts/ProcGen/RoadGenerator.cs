using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
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

    [Header("Traffic Spawning Ranges & Settings")]
    [Tooltip("Drag your Truck Prefab here")]
    [SerializeField] private GameObject truckPrefab;
    [Tooltip("Time interval in seconds between traffic spawn waves")]
    [SerializeField] private float trafficSpawnInterval = 3.0f;
    [Tooltip("Minimum number of trucks to spawn per wave")]
    [SerializeField] private int minTrucksPerWave = 1;
    [Tooltip("Maximum number of trucks to spawn per wave")]
    [SerializeField] private int maxTrucksPerWave = 5;
    [Tooltip("Minimum distance away from the player trucks are allowed to spawn")]
    [SerializeField] private float minSpawnDistance = 20f;
    [Tooltip("Maximum distance away from the player trucks are allowed to spawn")]
    [SerializeField] private float maxSpawnDistance = 50f;
    [Tooltip("Radius of the overlap check to prevent trucks from spawning on top of each other")]
    [SerializeField] private float truckCheckRadius = 3.0f;
    [Tooltip("Layer mask used to check if a spawn point is already blocked by another truck")]
    [SerializeField] private LayerMask truckCheckLayer;

    [Header("Building Settings")]
    [Tooltip("Pool of building prefabs with individual sizing, offset, and rotation settings")]
    [SerializeField] private BuildingConfig[] buildingPrefabs = new BuildingConfig[15];

    [Tooltip("Gap space between buildings in tile units (0.25 = a quarter tile space)")]
    [SerializeField] private float buildingSpacingTiles = 0.25f;

    [Tooltip("Moves all front-row buildings closer to the road (20 units = 4 tiles)")]
    [SerializeField] private float buildingProximityOffset = 20.0f;

    [Header("Multi-Row Building Settings")]
    [Tooltip("Total number of building rows deep on each side of the road (e.g., 3, 5, 9)")]
    [Min(1)]
    [SerializeField] private int buildingRowsCount = 2;

    [Tooltip("Distance step outward between each consecutive row of buildings")]
    [SerializeField] private float rowSpacingDistance = 25.0f;

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

    // Dynamic lists to track independent Z positions and last building indices for each row
    private readonly List<float> leftRowNextZ = new List<float>();
    private readonly List<float> rightRowNextZ = new List<float>();
    private readonly List<int> leftRowLastIndex = new List<int>();
    private readonly List<int> rightRowLastIndex = new List<int>();

    // Spawner Activation State
    private bool isSpawnerActivated = false;
    private float trafficTimer = 0f;

    private void Start()
    {
        // Ensure BoxCollider is set as a trigger automatically
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.isTrigger = true;
        }

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

        InitializeRowTrackers();
        UpdateEndlessRoad();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Activate traffic spawning when player enters the trigger box collider
        if (!isSpawnerActivated && other.CompareTag("Player"))
        {
            isSpawnerActivated = true;
            Debug.Log("Player triggered traffic spawner!");
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Automatically update tracker lists if buildingRowsCount is changed in the Inspector during play
        if (leftRowNextZ.Count != buildingRowsCount)
        {
            InitializeRowTrackers();
        }

        UpdateEndlessRoad();

        // Handle Traffic Spawning only if activated by the trigger box
        if (isSpawnerActivated && truckPrefab != null)
        {
            trafficTimer += Time.deltaTime;
            if (trafficTimer >= trafficSpawnInterval)
            {
                trafficTimer = 0f;
                SpawnChaoticTrafficWave();
            }
        }
    }

    private void InitializeRowTrackers()
    {
        leftRowNextZ.Clear();
        rightRowNextZ.Clear();
        leftRowLastIndex.Clear();
        rightRowLastIndex.Clear();

        for (int i = 0; i < buildingRowsCount; i++)
        {
            leftRowNextZ.Add(0f);
            rightRowNextZ.Add(0f);
            leftRowLastIndex.Add(-1);
            rightRowLastIndex.Add(-1);
        }
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

        // 3. Generate buildings independently across all rows ahead on both sides
        float targetMaxZ = targetMaxSegment * segmentLength;

        for (int r = 0; r < buildingRowsCount; r++)
        {
            while (leftRowNextZ[r] < targetMaxZ)
            {
                float currentZ = leftRowNextZ[r];
                int lastIdx = leftRowLastIndex[r];
                SpawnBuildingRow(isLeft: true, rowIndex: r, ref currentZ, ref lastIdx);
                leftRowNextZ[r] = currentZ;
                leftRowLastIndex[r] = lastIdx;
            }

            while (rightRowNextZ[r] < targetMaxZ)
            {
                float currentZ = rightRowNextZ[r];
                int lastIdx = rightRowLastIndex[r];
                SpawnBuildingRow(isLeft: false, rowIndex: r, ref currentZ, ref lastIdx);
                rightRowNextZ[r] = currentZ;
                rightRowLastIndex[r] = lastIdx;
            }
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

    private void SpawnChaoticTrafficWave()
    {
        int trucksToSpawn = Random.Range(minTrucksPerWave, maxTrucksPerWave + 1);

        for (int i = 0; i < trucksToSpawn; i++)
        {
            TrySpawnSingleTruck();
        }
    }

    private void TrySpawnSingleTruck()
    {
        float roadCenter = transform.position.x;
        float[] laneOffsets = new float[] 
        { 
            roadCenter - (roadTileWidth * 1.5f), 
            roadCenter - (roadTileWidth * 0.5f), 
            roadCenter + (roadTileWidth * 0.5f), 
            roadCenter + (roadTileWidth * 1.5f) 
        };

        // Shuffle lane options for randomness
        for (int i = 0; i < laneOffsets.Length; i++)
        {
            int rnd = Random.Range(0, laneOffsets.Length);
            float temp = laneOffsets[rnd];
            laneOffsets[rnd] = laneOffsets[i];
            laneOffsets[i] = temp;
        }

        bool spawnAhead = Random.value > 0.5f;
        float distanceOffset = Random.Range(minSpawnDistance, maxSpawnDistance);
        float targetZ = player.position.z + (spawnAhead ? distanceOffset : -distanceOffset);

        foreach (float laneX in laneOffsets)
        {
            Vector3 candidatePos = new Vector3(laneX, transform.position.y, targetZ);

            bool isSpotBlocked = Physics.CheckSphere(candidatePos, truckCheckRadius, truckCheckLayer);

            if (!isSpotBlocked)
            {
                Instantiate(truckPrefab, candidatePos, transform.rotation);
                return;
            }
        }
    }

    private void SpawnBuildingRow(bool isLeft, int rowIndex, ref float currentZ, ref int lastIndex)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        int bgIndex = GetRandomBuildingIndex(lastIndex);
        if (bgIndex == -1) return;
        lastIndex = bgIndex;

        BuildingConfig bConfig = buildingPrefabs[bgIndex];

        float lengthInTiles = bConfig.lengthInTiles > 0 ? bConfig.lengthInTiles : 1f;
        float widthInTiles = bConfig.widthInTiles > 0 ? bConfig.widthInTiles : 1f;

        float buildingLengthUnits = lengthInTiles * segmentLength;
        float spacingUnits = buildingSpacingTiles * segmentLength;

        float centerZ = currentZ + (buildingLengthUnits * 0.5f);
        int segmentZIndex = Mathf.FloorToInt(currentZ / segmentLength);

        if (activeSegments.TryGetValue(segmentZIndex, out GameObject rowParent))
        {
            float startX = transform.position.x;
            float leftCurbX  = startX - (roadTileWidth * 2.0f) - (curbTileWidth * 0.5f);
            float rightCurbX = startX + (roadTileWidth * 2.0f) + (curbTileWidth * 0.5f);

            float buildingX;
            Vector3 targetRotation = bConfig.rotation;
            Vector3 targetOffset = bConfig.offset;

            float rowOffset = rowIndex * rowSpacingDistance;

            if (isLeft)
            {
                float farLeftSidewalkX = leftCurbX - (curbTileWidth * 0.5f) - (sidewalkTileWidth * sidewalkCount);
                buildingX = farLeftSidewalkX - (widthInTiles * roadTileWidth * 0.5f);
                buildingX += buildingProximityOffset;
                buildingX -= rowOffset;
            }
            else
            {
                float farRightSidewalkX = rightCurbX + (curbTileWidth * 0.5f) + (sidewalkTileWidth * sidewalkCount);
                buildingX = farRightSidewalkX + (widthInTiles * roadTileWidth * 0.5f);
                buildingX -= buildingProximityOffset;
                buildingX += rowOffset;

                targetRotation.y += 180f;

                targetOffset.x = -targetOffset.x;
                targetOffset.z = -targetOffset.z;
            }

            Vector3 basePosition = new Vector3(buildingX, transform.position.y, centerZ);
            Vector3 finalPosition = basePosition + targetOffset;
            Quaternion finalRotation = transform.rotation * Quaternion.Euler(targetRotation);

            if (bConfig.prefab != null)
            {
                GameObject spawnedBuilding = Instantiate(bConfig.prefab, finalPosition, finalRotation, rowParent.transform);
                
                int buildingLayer = LayerMask.NameToLayer("Building");
                if (buildingLayer != -1)
                {
                    SetLayerRecursively(spawnedBuilding, buildingLayer);
                }
            }
        }

        currentZ += buildingLengthUnits + spacingUnits;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
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