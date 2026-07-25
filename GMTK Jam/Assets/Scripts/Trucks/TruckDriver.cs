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
    public float maxOvershotDistance = 2.0f; // Max meters behind Z-axis before abandoning point
    public LayerMask roadLayer;

    [Header("Crash & Vision Settings")]
    public LayerMask buildingLayer;
    public LayerMask truckLayer;

    [Tooltip("Distance in front of the truck to cast vision rays for buildings")]
    public float visionDistance = 2.5f;
    
    [Tooltip("Width spread of the front bumper vision rays")]
    public float visionWidth = 1.5f;

    [Tooltip("Impact speed required to trigger a crash when hitting a building")]
    public float minBuildingImpactSpeed = 1.0f;

    [Tooltip("Higher impact speed required to trigger a crash when hitting another truck")]
    public float minTruckImpactSpeed = 7.0f;

    [Header("Debug Visualization")]
    public bool showDebugLines = true;

    // Fixed constant speed set once at spawn
    public float constantMoveSpeed;

    // Dynamic leg-by-leg values
    private float currentTurnSpeed;
    private float currentWaypointTolerance;
    private float currentRoadWidth;

    private Vector3 currentTargetPoint;
    private bool hasValidTarget = false;
    private bool isCrashed = false; // Prevents further movement once crashed

    private void Start()
    {
        // 1. Roll the truck's permanent movement speed ONCE
        constantMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

        // 2. Roll initial leg stats & first waypoint
        RandomizeLegStats();
        GenerateNextWaypoint();
    }

    private void Update()
    {
        // Stop all processing if the truck has crashed
        if (isCrashed) return;

        // 1. Check forward vision rays for immediate wall/building detection
        CheckForwardVision();
        if (isCrashed) return;

        // 2. Waypoint navigation
        if (!hasValidTarget)
        {
            GenerateNextWaypoint();
            return;
        }

        MoveAndSteer();
        CheckWaypointDistance();
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
        // Raycast origin at truck center height (~1 unit up)
        Vector3 centerOrigin = transform.position + Vector3.up * 1f;

        // Cast 3 forward rays: Center, Left, and Right bumper offsets
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

        // 1. Collision with Building (Slight to moderate hit threshold)
        if (((1 << hitLayer) & buildingLayer.value) != 0)
        {
            if (impactSpeed >= minBuildingImpactSpeed)
            {
                TriggerCrash();
            }
        }
        // 2. Collision with Another Truck (Requires harder/more forceful impact)
        else if (((1 << hitLayer) & truckLayer.value) != 0)
        {
            if (impactSpeed >= minTruckImpactSpeed)
            {
                TriggerCrash();
            }
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

        // Convert target point into truck's local space to track Z axis position
        Vector3 localTargetPoint = transform.InverseTransformPoint(currentTargetPoint);

        // Condition 1: Reached point within tolerance distance
        // Condition 2: Point is behind truck's local Z-axis by more than maxOvershotDistance
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

        // Fallback straight line
        currentTargetPoint = transform.position + (transform.forward * minForwardDistance);
        hasValidTarget = true;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugLines) return;

        // Draw forward vision rays in Scene View
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