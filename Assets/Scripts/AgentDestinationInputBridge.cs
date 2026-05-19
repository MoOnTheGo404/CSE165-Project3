using UnityEngine;

public class AgentDestinationInputBridge : MonoBehaviour
{
    [SerializeField] private OVRHandGestureInput handInput;
    [SerializeField] private AgentMovementController agentMovement;
    [SerializeField] private bool useHandTarget = true;
    [SerializeField] private bool requireValidTarget = true;

    private bool previousGestureActive;

    private void Update()
    {
        if (!useHandTarget || handInput == null || agentMovement == null)
        {
            return;
        }

        bool currentActive = handInput.IsGestureActive;
        if (currentActive && !previousGestureActive)
        {
            if (!requireValidTarget || handInput.HasValidTarget)
            {
                Vector3 destination = handInput.TargetPointWorld;
                agentMovement.SetExternalDestination(destination);
            }
        }

        previousGestureActive = currentActive;
    }
}
