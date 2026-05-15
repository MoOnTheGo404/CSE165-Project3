using UnityEngine;

public class AgentMovementController : MonoBehaviour
{
    [SerializeField] private HandGestureDetector handGestureDetector;
    [SerializeField] private Transform agentRoot;
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float destinationDistance = 2f;
    [SerializeField] private float stoppingDistance = 0.08f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleCheckRadius = 0.25f;
    [SerializeField] private float obstacleCheckDistance = 0.35f;

    public bool IsMoving { get; private set; }
    public bool IsBlocked { get; private set; }
    public Vector3 CurrentDestination { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }

    private bool previousGestureActive;
    private Vector3 lastObstacleCheckOrigin;
    private Vector3 lastObstacleCheckDirection = Vector3.forward;
    private float lastObstacleCheckDistance;

    private void Awake()
    {
        if (agentRoot == null)
        {
            agentRoot = transform;
        }
    }

    private void Update()
    {
        if (handGestureDetector == null || agentRoot == null)
        {
            StopMovement();
            previousGestureActive = false;
            return;
        }

        bool gestureActive = handGestureDetector.IsGestureActive;
        if (gestureActive && !previousGestureActive)
        {
            SetDestinationFromGesture();
        }

        previousGestureActive = gestureActive;
        UpdateMovement();
    }

    private void SetDestinationFromGesture()
    {
        Vector3 flatDirection = FlattenDirection(handGestureDetector.GestureDirectionWorld);
        if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            IsBlocked = true;
            return;
        }

        Vector3 destination = handGestureDetector.GestureOriginWorld + flatDirection * destinationDistance;
        destination.y = agentRoot.position.y;
        CurrentDestination = destination;
        IsMoving = true;
        IsBlocked = false;
    }

    private void UpdateMovement()
    {
        if (!IsMoving)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        Vector3 toDestination = CurrentDestination - agentRoot.position;
        toDestination.y = 0f;

        float distanceRemaining = toDestination.magnitude;
        if (distanceRemaining <= stoppingDistance)
        {
            StopMovement();
            return;
        }

        Vector3 moveDirection = toDestination / distanceRemaining;
        float moveDistance = Mathf.Min(speed * Time.deltaTime, distanceRemaining);

        if (WillHitObstacle(moveDirection, moveDistance))
        {
            IsBlocked = true;
            StopMovement();
            return;
        }

        Vector3 movement = moveDirection * moveDistance;
        agentRoot.position += movement;
        CurrentVelocity = movement / Mathf.Max(Time.deltaTime, Mathf.Epsilon);
        IsBlocked = false;

        RotateToward(moveDirection);
    }

    private bool WillHitObstacle(Vector3 moveDirection, float moveDistance)
    {
        if (moveDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        lastObstacleCheckOrigin = agentRoot.position + Vector3.up * obstacleCheckRadius;
        lastObstacleCheckDirection = moveDirection.normalized;
        lastObstacleCheckDistance = moveDistance + obstacleCheckDistance;

        return Physics.SphereCast(
            lastObstacleCheckOrigin,
            obstacleCheckRadius,
            lastObstacleCheckDirection,
            out _,
            lastObstacleCheckDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        agentRoot.rotation = Quaternion.RotateTowards(
            agentRoot.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;
    }

    private void StopMovement()
    {
        IsMoving = false;
        CurrentVelocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Transform root = agentRoot != null ? agentRoot : transform;

        Gizmos.color = IsBlocked ? Color.red : Color.green;
        Gizmos.DrawWireSphere(CurrentDestination, 0.12f);
        Gizmos.DrawLine(root.position, CurrentDestination);

        Gizmos.color = Color.yellow;
        Vector3 checkOrigin = Application.isPlaying
            ? lastObstacleCheckOrigin
            : root.position + Vector3.up * obstacleCheckRadius;
        Vector3 checkDirection = Application.isPlaying
            ? lastObstacleCheckDirection
            : root.forward;
        float checkDistance = Application.isPlaying
            ? lastObstacleCheckDistance
            : obstacleCheckDistance;

        Gizmos.DrawWireSphere(checkOrigin, obstacleCheckRadius);
        Gizmos.DrawLine(checkOrigin, checkOrigin + checkDirection.normalized * checkDistance);
        Gizmos.DrawWireSphere(checkOrigin + checkDirection.normalized * checkDistance, obstacleCheckRadius);
    }
}
