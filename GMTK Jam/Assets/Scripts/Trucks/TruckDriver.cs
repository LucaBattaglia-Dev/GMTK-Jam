using UnityEngine;

public class TruckDriver : MonoBehaviour
{
    [Header("Global Modifiers")]
    [Tooltip("Changes the speed of ALL trucks. Set this to 0.1f from your power-up script to slow time.")]
    public static float GlobalSpeedMultiplier = 1f;

    [Header("References")]
    [Tooltip("Auto-assigned by the TrafficSpawner")]
    public Transform player;

    [Header("Movement Settings")]
    [Tooltip("Possible speeds the truck can randomly choose from when it spawns")]
    [SerializeField] private float[] possibleSpeeds = new float[] { 6f, 9f, 11f, 13f };
    
    [Tooltip("Check if the truck should drive forward relative to its local rotation")]
    [SerializeField] private bool driveForward = true;

    [Header("Despawn Settings")]
    [Tooltip("Distance in tiles away from the player before the truck is destroyed")]
    [SerializeField] private float despawnTiles = 25f;
    [Tooltip("The size of one tile in world units (matches your segment length)")]
    [SerializeField] private float tileSize = 5.0f;

    // The base speed this specific truck will use
    private float baseMoveSpeed;

    private void Start()
    {
        // Randomly select a speed from the possible options when spawned
        if (possibleSpeeds.Length > 0)
        {
            int randomIndex = Random.Range(0, possibleSpeeds.Length);
            baseMoveSpeed = possibleSpeeds[randomIndex];
        }
        else
        {
            baseMoveSpeed = 9f; 
        }
    }

    private void Update()
    {
        // 1. Calculate the active speed by applying the global multiplier
        float activeSpeed = baseMoveSpeed * GlobalSpeedMultiplier;
        float direction = driveForward ? 1f : -1f;
        
        // 2. Move the truck continuously
        transform.Translate(Vector3.forward * activeSpeed * direction * Time.deltaTime);

        // 3. Check if the truck exceeds the despawn distance from the player
        if (player != null)
        {
            float distanceInUnits = Vector3.Distance(transform.position, player.position);
            float maxAllowedDistance = despawnTiles * tileSize;

            if (distanceInUnits >= maxAllowedDistance)
            {
                Destroy(gameObject);
            }
        }
    }
}