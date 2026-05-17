using UnityEngine;

public class AgentDestinationInputBridge : MonoBehaviour
{
    [SerializeField] private OVRHandGestureInput handInput;
    [SerializeField] private AgentMovementController agentMovement;
    [SerializeField] private bool useHandTarget = true;
    [SerializeField] private bool requireValidTarget = true;
    [SerializeField] private float repeatedCommandCooldown = 0.4f;
    [SerializeField] private float minDestinationChangeDistance = 0.15f;

    private float lastCommandTime = -Mathf.Infinity;
    private Vector3 lastCommandedDestination;
    private bool hasCommandedDestination;

    private void Update()
    {
        if (!useHandTarget || handInput == null || agentMovement == null)
        {
            return;
        }

        if (!handInput.IsGestureActive)
        {
            return;
        }

        if (requireValidTarget && !handInput.HasValidTarget)
        {
            return;
        }

        Vector3 destination = handInput.HasValidTarget
            ? handInput.TargetPointWorld
            : handInput.RayOriginWorld + handInput.RayDirectionWorld;

        if (!ShouldSendDestination(destination))
        {
            return;
        }

        agentMovement.SetExternalDestination(destination);
        lastCommandedDestination = destination;
        lastCommandTime = Time.time;
        hasCommandedDestination = true;
    }

    private bool ShouldSendDestination(Vector3 destination)
    {
        if (!hasCommandedDestination)
        {
            return true;
        }

        float distanceFromLastCommand = Vector3.Distance(destination, lastCommandedDestination);
        if (distanceFromLastCommand >= minDestinationChangeDistance)
        {
            return true;
        }

        return Time.time - lastCommandTime >= repeatedCommandCooldown;
    }
}
