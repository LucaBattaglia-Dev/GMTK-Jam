using UnityEngine;

public class TruckPowerupAssigner : MonoBehaviour
{
    [Header("Powerup Spawn Settings")]
    [Tooltip("Percentage chance (0 to 100) that this truck will spawn with a powerup. 10 = 1 in 10 chance.")]
    [Range(0f, 100f)]
    public float spawnChance = 10f;

    [Tooltip("List of possible powerup prefabs to spawn (Time Stop, Extra Time, etc.)")]
    public GameObject[] powerupPrefabs;

    [Header("Positioning")]
    [Tooltip("Optional: A specific transform on the truck where the powerup should sit (like the roof). If left empty, it uses the offset below.")]
    public Transform attachmentPoint;

    [Tooltip("If no attachment point is assigned, it will spawn this many units relative to the truck's center.")]
    public Vector3 localOffset = new Vector3(0f, 3.5f, 0f);

    private void Start()
    {
        RollForPowerup();
    }

    private void RollForPowerup()
    {
        // 1. Check if we have any powerups to actually spawn
        if (powerupPrefabs == null || powerupPrefabs.Length == 0) return;

        // 2. Roll a random number between 0.0 and 100.0
        float roll = Random.Range(0f, 100f);

        // 3. If the roll is less than or equal to our chance, we spawn a powerup!
        if (roll <= spawnChance)
        {
            // 4. Pick a completely random powerup from your array
            int randomIndex = Random.Range(0, powerupPrefabs.Length);
            GameObject selectedPowerup = powerupPrefabs[randomIndex];

            if (selectedPowerup != null)
            {
                // 5. Determine where to place it
                Vector3 spawnPosition = attachmentPoint != null 
                    ? attachmentPoint.position 
                    : transform.position + transform.TransformVector(localOffset);
                
                // 6. Spawn the powerup and set the Truck as its parent so it moves with it
                GameObject spawnedPowerup = Instantiate(selectedPowerup, spawnPosition, transform.rotation, transform);
                
                // Keep the powerup perfectly aligned with the truck's orientation
                spawnedPowerup.transform.localRotation = Quaternion.identity;
            }
        }
    }
}