using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject truckPrefab;
    public Transform player;

    [Header("Spawn Settings")]
    public float spawnInterval = 3.5f;          // Time between spawn attempts
    public float spawnDistanceAhead = 70f;     // Distance in front of player to spawn traffic
    public float spawnDistanceBehind = 70f;    // Distance behind player to spawn traffic
    public float spawnSideOffsetRange = 3f;    // Random X/Z offset variation from road center

    private float timer;

    private void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
    }

    private void Update()
    {
        if (player == null || truckPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnTrafficBatch();
        }
    }

    private void SpawnTrafficBatch()
    {
        // 1. Spawn a truck ahead of the player
        Vector3 aheadSpawnPos = player.position + (player.forward * spawnDistanceAhead) + (player.right * Random.Range(-spawnSideOffsetRange, spawnSideOffsetRange));
        TrySpawnTruck(aheadSpawnPos, player.rotation);

        // 2. Spawn a truck behind the player (facing forward along player's orientation)
        Vector3 behindSpawnPos = player.position - (player.forward * spawnDistanceBehind) + (player.right * Random.Range(-spawnSideOffsetRange, spawnSideOffsetRange));
        TrySpawnTruck(behindSpawnPos, player.rotation);
    }

    private void TrySpawnTruck(Vector3 targetPos, Quaternion rotation)
    {
        // Cast a ray downward to snap the truck onto the road surface layer
        Vector3 rayOrigin = targetPos + Vector3.up * 30f;
        
        // Use your road layer mask logic (adjust layer name if needed)
        int roadLayerMask = LayerMask.GetMask("Road"); // Ensure you have a "Road" layer or replace with your layer

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 60f))
        {
            Vector3 spawnPosition = hit.point;
            GameObject newTruck = Instantiate(truckPrefab, spawnPosition, rotation);
            
            // Link player reference automatically if needed
            TruckDriver driver = newTruck.GetComponent<TruckDriver>();
            if (driver != null)
            {
                driver.player = player;
            }
        }
    }
}