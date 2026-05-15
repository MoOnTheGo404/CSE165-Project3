using UnityEngine;
using UnityEngine.UI;

public class AgentHUD : MonoBehaviour
{
    [SerializeField] private HandGestureDetector handGestureDetector;
    [SerializeField] private AgentMovementController movementController;
    [SerializeField] private Text statusText;

    private void Update()
    {
        if (statusText == null)
        {
            return;
        }

        bool gestureActive = handGestureDetector != null && handGestureDetector.IsGestureActive;
        Vector3 direction = handGestureDetector != null
            ? handGestureDetector.GestureDirectionWorld
            : Vector3.zero;

        string agentState = GetAgentState();
        Vector3 destination = movementController != null
            ? movementController.CurrentDestination
            : Vector3.zero;

        statusText.text =
            $"Gesture: {(gestureActive ? "Active" : "Inactive")}\n" +
            $"Agent: {agentState}\n" +
            $"Direction: {FormatVector(direction)}\n" +
            $"Destination: {FormatVector(destination)}";
    }

    private string GetAgentState()
    {
        if (movementController == null)
        {
            return "Idle";
        }

        if (movementController.IsBlocked)
        {
            return "Blocked";
        }

        return movementController.IsMoving ? "Moving" : "Idle";
    }

    private string FormatVector(Vector3 value)
    {
        return $"{value.x:0.00}, {value.y:0.00}, {value.z:0.00}";
    }
}
