using System;
using UnityEngine;

public class AgentMovementController : MonoBehaviour
{
    [SerializeField] private HandGestureDetector handGestureDetector;
    [SerializeField] private Transform agentRoot;
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.25f;
    [SerializeField] private float obstacleCheckRadius = 0.25f;
    [SerializeField] private LayerMask obstacleMask;

    public bool IsMoving { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool HasDestination { get; private set; }
    public Vector3 CurrentDestination { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }

    private const float GestureDestinationDistance = 1.5f;
    private static readonly string[] UnsafeRootNameParts =
    {
        "Camera",
        "Floor",
        "Wall",
        "Marker",
        "Bridge",
        "Gesture"
    };

    private bool previousGestureActive;

    private void Awake()
    {
        if (agentRoot == null)
        {
            agentRoot = transform;
        }

        WarnIfAgentRootLooksUnsafe();
    }

    private void Update()
    {
        CurrentVelocity = Vector3.zero;
        IsMoving = false;

        if (agentRoot == null)
        {
            HasDestination = false;
            previousGestureActive = false;
            return;
        }

        UpdateGestureDestination();
        UpdateMovement();
    }

    public void SetExternalDestination(Vector3 destination)
    {
        if (agentRoot == null)
        {
            return;
        }

        destination.y = agentRoot.position.y;
        CurrentDestination = ClampDestinationBeforeObstacle(destination, out bool wasClamped);
        HasDestination = true;
        IsBlocked = wasClamped;
    }

    private void UpdateGestureDestination()
    {
        if (handGestureDetector == null)
        {
            previousGestureActive = false;
            return;
        }

        bool gestureActive = handGestureDetector.IsGestureActive;
        if (gestureActive && !previousGestureActive)
        {
            Vector3 flatDirection = FlattenDirection(handGestureDetector.GestureDirectionWorld);
            if (flatDirection.sqrMagnitude > Mathf.Epsilon)
            {
                SetExternalDestination(agentRoot.position + flatDirection * GestureDestinationDistance);
            }
        }

        previousGestureActive = gestureActive;
    }

    private void UpdateMovement()
    {
        if (!HasDestination)
        {
            return;
        }

        Vector3 currentPosition = agentRoot.position;
        Vector3 toDestination = CurrentDestination - currentPosition;
        toDestination.y = 0f;

        if (toDestination.magnitude <= stoppingDistance)
        {
            HasDestination = false;
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            CurrentDestination,
            moveSpeed * Time.deltaTime
        );
        nextPosition.y = currentPosition.y;

        Vector3 movement = nextPosition - currentPosition;
        agentRoot.position = nextPosition;

        if (Time.deltaTime > Mathf.Epsilon)
        {
            CurrentVelocity = movement / Time.deltaTime;
        }

        IsMoving = CurrentVelocity.sqrMagnitude > Mathf.Epsilon;

        if (movement.sqrMagnitude > Mathf.Epsilon)
        {
            RotateToward(movement.normalized);
        }
    }

    private Vector3 ClampDestinationBeforeObstacle(Vector3 destination, out bool wasClamped)
    {
        wasClamped = false;

        Vector3 origin = agentRoot.position;
        Vector3 toDestination = destination - origin;
        toDestination.y = 0f;

        float distance = toDestination.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return destination;
        }

        Vector3 direction = toDestination / distance;
        if (!Physics.SphereCast(
            origin,
            obstacleCheckRadius,
            direction,
            out RaycastHit hit,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            return destination;
        }

        float safeDistance = Mathf.Max(hit.distance - stoppingDistance, 0f);
        Vector3 clampedDestination = origin + direction * safeDistance;
        clampedDestination.y = origin.y;
        wasClamped = true;
        return clampedDestination;
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        agentRoot.rotation = Quaternion.Slerp(
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

    private void WarnIfAgentRootLooksUnsafe()
    {
        if (agentRoot == null)
        {
            return;
        }

        string rootName = agentRoot.name;
        for (int i = 0; i < UnsafeRootNameParts.Length; i++)
        {
            string namePart = UnsafeRootNameParts[i];
            if (rootName.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            Debug.LogWarning(
                $"AgentMovementController agentRoot is '{rootName}'. Only the EmbodiedAgent root should be assigned here.",
                this
            );
            return;
        }
    }
}
