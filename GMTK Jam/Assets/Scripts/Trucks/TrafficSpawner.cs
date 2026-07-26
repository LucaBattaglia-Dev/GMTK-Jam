using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject truckPrefab;
    public Transform player;

    [Header("Wave Spawn Settings")]
    [Tooltip("Time interval in seconds between each spawn wave")]
    public float spawnInterval = 3.0f;
    [Tooltip("Minimum number of trucks spawned per wave")]
    public int minTrucksPerWave = 2;
    [Tooltip("Maximum number of trucks spawned per wave")]
    public int maxTrucksPerWave = 5;

    [Header("Distance Ranges from Player")]
    [Tooltip("Minimum distance away from the player trucks can spawn")]
    public float minSpawnDistance = 20f;
    [Tooltip("Maximum distance away from the player trucks can spawn")]
    public float maxSpawnDistance = 60f;

    [Header("Lane Settings")]
    [Tooltip("Exact X coordinates/lanes where trucks are allowed to spawn")]
    public float[] laneXPositions = new float[] { -12.5f, -7.5f, -2.5f, 2.5f, 7.5f, 12.5f };

    [Header("Overlap Prevention")]
    [Tooltip("Radius to check for existing trucks to prevent stacking")]
    public float truckCheckRadius = 4f;
    [Tooltip("Layer mask for trucks so the spawner knows where existing trucks are located")]
    public LayerMask truckCheckLayer;

    private float timer;
    private Transform truckContainer; // Tracks the parent object for hierarchy organization

    private void Start()
    {
        // 1. Auto-find Player if not assigned
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 2. Find or create the "Trucks" container in the Hierarchy
        GameObject containerObj = GameObject.Find("Trucks");
        if (containerObj == null)
        {
            containerObj = new GameObject("Trucks");
        }
        truckContainer = containerObj.transform;
    }

    private void Update()
    {
        // Wait until the player crosses the start line before spawning traffic
        if (!TruckDriver.CanMove || player == null || truckPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnTrafficWave();
        }
    }

    private void SpawnTrafficWave()
    {
        int trucksToSpawn = Random.Range(minTrucksPerWave, maxTrucksPerWave + 1);

        for (int i = 0; i < trucksToSpawn; i++)
        {
            TrySpawnSingleTruck();
        }
    }

    private void TrySpawnSingleTruck()
    {
        // Randomly shuffle lanes so trucks don't always pick the same lane first when blocked
        float[] shuffledLanes = (float[])laneXPositions.Clone();
        for (int i = 0; i < shuffledLanes.Length; i++)
        {
            int rnd = Random.Range(0, shuffledLanes.Length);
            float temp = shuffledLanes[rnd];
            shuffledLanes[rnd] = shuffledLanes[i];
            shuffledLanes[i] = temp;
        }

        // Randomly choose ahead (+) or behind (-) the player
        bool spawnAhead = Random.value > 0.5f;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        float targetZ = player.position.z + (spawnAhead ? distance : -distance);

        // Try each lane until an unoccupied spot is found
        foreach (float laneX in shuffledLanes)
        {
            Vector3 candidatePos = new Vector3(laneX, player.position.y, targetZ);

            // Check if another truck is already occupying this spot
            bool isBlocked = Physics.CheckSphere(candidatePos, truckCheckRadius, truckCheckLayer);
            if (isBlocked) continue; // Try next lane

            // Raycast down to snap precisely onto the road surface layer
            Vector3 rayOrigin = candidatePos + Vector3.up * 30f;
            int roadLayerMask = LayerMask.GetMask("Road");

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 60f, roadLayerMask))
            {
                Vector3 spawnPosition = hit.point;

                // Final safety check at the exact road hit point
                if (!Physics.CheckSphere(spawnPosition, truckCheckRadius, truckCheckLayer))
                {
                    // Instantiate the truck and set its parent to the truckContainer
                    GameObject newTruck = Instantiate(truckPrefab, spawnPosition, Quaternion.identity, truckContainer);

                    // Link player reference automatically if needed
                    TruckDriver driver = newTruck.GetComponent<TruckDriver>();
                    if (driver != null)
                    {
                        driver.player = player;
                    }
                    return; // Successfully spawned a truck, exit method
                }
            }
        }
    }
}