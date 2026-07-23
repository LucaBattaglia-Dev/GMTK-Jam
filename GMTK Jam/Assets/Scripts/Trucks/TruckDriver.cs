using UnityEngine;

public class TruckDriver : MonoBehaviour
{
    [Header("Movement Speed Ranges (Constant per truck)")]
    public float minMoveSpeed = 5.0f;
    public float maxMoveSpeed = 13.0f;

    [Header("Turn Speed Ranges")]
    public float minTurnSpeed = 1.0f;
    public float maxTurnSpeed = 3.0f;

    [Header("Waypoint Generation Settings")]
    public float minForwardDistance = 10f;
    public float maxForwardDistance = 35f;

    [Header("Road Width Range")]
    public float minRoadWidth = 1.0f;
    public float maxRoadWidth = 3.0f;

    [Header("Waypoint Arrival Ranges")]
    public float minWaypointTolerance = 3.0f;
    public float maxWaypointTolerance = 7.0f;
    public float maxOvershotDistance = 3.0f; // Max meters behind Z-axis before abandoning point
    public LayerMask roadLayer;

    [Header("Debug Visualization")]
    public bool showDebugLines = true;

    // Fixed constant speed set once at spawn
    private float constantMoveSpeed;

    // Dynamic leg-by-leg values
    private float currentTurnSpeed;
    private float currentWaypointTolerance;
    private float currentRoadWidth;

    private Vector3 currentTargetPoint;
    private bool hasValidTarget = false;

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
        if (showDebugLines && hasValidTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTargetPoint, currentWaypointTolerance > 0 ? currentWaypointTolerance : minWaypointTolerance);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTargetPoint);
        }
    }
}