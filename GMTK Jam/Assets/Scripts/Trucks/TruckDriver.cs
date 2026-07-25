using UnityEngine;

public class TruckDriver : MonoBehaviour
{
    [Header("Movement Speed Ranges (Constant per truck)")]
    public float minMoveSpeed = 5.0f;
    public float maxMoveSpeed = 15.0f;

    [Header("Turn Speed Ranges")]
    public float minTurnSpeed = 1.5f;
    public float maxTurnSpeed = 3.0f;

    [Header("Waypoint Generation Settings")]
    public float minForwardDistance = 15f;
    public float maxForwardDistance = 40f;

    [Header("Road Width Range")]
    public float minRoadWidth = 1.0f;
    public float maxRoadWidth = 2.125f;

    [Header("Waypoint Arrival Ranges")]
    public float minWaypointTolerance = 4.0f;
    public float maxWaypointTolerance = 9.0f;
    public float maxOvershotDistance = 2.0f; 
    public LayerMask roadLayer;

    [Header("Crash & Vision Settings")]
    public LayerMask buildingLayer;
    public LayerMask truckLayer;
    public float visionDistance = 2.5f;
    public float visionWidth = 1.5f;
    public float minBuildingImpactSpeed = 1.0f;
    public float minTruckImpactSpeed = 7.0f;

    [Header("Distance & Stream Settings")]
    public Transform player;
    public float activeRange = 60f;      // Distance within which normal AI/waypoints run
    public float despawnDistance = 90f;  // Distance at which the truck gets destroyed

    [Header("Debug Visualization")]
    public bool showDebugLines = true;

    private float constantMoveSpeed;
    private float currentTurnSpeed;
    private float currentWaypointTolerance;
    private float currentRoadWidth;

    private Vector3 currentTargetPoint;
    private bool hasValidTarget = false;
    private bool isCrashed = false;

    private void Start()
    {
        // Find player automatically if unassigned
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        constantMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
        RandomizeLegStats();
        GenerateNextWaypoint();
    }

    private void Update()
    {
        if (isCrashed) return;

        // Check distance to player for despawning or switching behavior
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);

            // Despawn if too far away
            if (distToPlayer > despawnDistance)
            {
                Destroy(gameObject);
                return;
            }

            // If player is too far away, enter Passive State (drive straight smoothly, slower)
            if (distToPlayer > activeRange)
            {
                DriveStraightPassive();
                return;
            }
        }

        // --- Active State (Player is in range) ---
        CheckForwardVision();
        if (isCrashed) return;

        if (!hasValidTarget)
        {
            GenerateNextWaypoint();
            return;
        }

        MoveAndSteer();
        CheckWaypointDistance();
    }

    private void DriveStraightPassive()
    {
        // Drive straight forward at a reduced, steady speed to prevent erratic swerving when off-screen
        float passiveSpeed = constantMoveSpeed * 0.75f;
        transform.Translate(Vector3.forward * passiveSpeed * Time.deltaTime);
    }

    private void MoveAndSteer()
    {
        Vector3 targetDirection = (currentTargetPoint - transform.position);
        targetDirection.y = 0;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * currentTurnSpeed);
        }

        transform.Translate(Vector3.forward * constantMoveSpeed * Time.deltaTime);
    }

    private void CheckForwardVision()
    {
        Vector3 centerOrigin = transform.position + Vector3.up * 1f;
        Vector3[] rayOrigins = new Vector3[]
        {
            centerOrigin,
            centerOrigin + (transform.right * (visionWidth * 0.5f)),
            centerOrigin - (transform.right * (visionWidth * 0.5f))
        };

        foreach (Vector3 origin in rayOrigins)
        {
            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, visionDistance, buildingLayer))
            {
                TriggerCrash();
                return;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isCrashed) return;

        int hitLayer = collision.gameObject.layer;
        float impactSpeed = collision.relativeVelocity.magnitude;

        if (((1 << hitLayer) & buildingLayer.value) != 0)
        {
            if (impactSpeed >= minBuildingImpactSpeed) TriggerCrash();
        }
        else if (((1 << hitLayer) & truckLayer.value) != 0)
        {
            if (impactSpeed >= minTruckImpactSpeed) TriggerCrash();
        }
    }

    private void TriggerCrash()
    {
        isCrashed = true;
        hasValidTarget = false;
        Debug.Log("truck crashed");
    }

    private void CheckWaypointDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, currentTargetPoint);
        Vector3 localTargetPoint = transform.InverseTransformPoint(currentTargetPoint);

        if (distanceToTarget <= currentWaypointTolerance || localTargetPoint.z < -maxOvershotDistance)
        {
            RandomizeLegStats(); 
            GenerateNextWaypoint();
        }
    }

    private void RandomizeLegStats()
    {
        currentTurnSpeed = Random.Range(minTurnSpeed, maxTurnSpeed);
        currentWaypointTolerance = Random.Range(minWaypointTolerance, maxWaypointTolerance);
        currentRoadWidth = Random.Range(minRoadWidth, maxRoadWidth);
    }

    private void GenerateNextWaypoint()
    {
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            float randomForward = Random.Range(minForwardDistance, maxForwardDistance);
            float randomSideOffset = Random.Range(-currentRoadWidth, currentRoadWidth);

            Vector3 candidatePoint = transform.position 
                                   + (transform.forward * randomForward) 
                                   + (transform.right * randomSideOffset);

            Vector3 rayOrigin = candidatePoint + Vector3.up * 20f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 40f, roadLayer))
            {
                currentTargetPoint = hit.point;
                hasValidTarget = true;
                return;
            }
        }

        currentTargetPoint = transform.position + (transform.forward * minForwardDistance);
        hasValidTarget = true;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugLines) return;

        Gizmos.color = isCrashed ? Color.red : Color.cyan;
        Vector3 centerOrigin = transform.position + Vector3.up * 2.5f;
        Gizmos.DrawRay(centerOrigin, transform.forward * visionDistance);
        Gizmos.DrawRay(centerOrigin + (transform.right * (visionWidth * 0.5f)), transform.forward * visionDistance);
        Gizmos.DrawRay(centerOrigin - (transform.right * (visionWidth * 0.5f)), transform.forward * visionDistance);

        if (hasValidTarget && !isCrashed)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTargetPoint, currentWaypointTolerance > 0 ? currentWaypointTolerance : minWaypointTolerance);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTargetPoint);
        }
    }
}