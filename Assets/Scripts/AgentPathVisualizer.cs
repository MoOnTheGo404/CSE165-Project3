using UnityEngine;

public class AgentPathVisualizer : MonoBehaviour
{
    [SerializeField] private AgentMovementController movementController;
    [SerializeField] private Transform agentRoot;
    [SerializeField] private Transform destinationMarker;
    [SerializeField] private LineRenderer pathLine;
    [SerializeField] private float floorY = 0f;
    [SerializeField] private float markerHeightOffset = 0.03f;
    [SerializeField] private float lineHeightOffset = 0.05f;

    private void Awake()
    {
        if (agentRoot == null)
        {
            agentRoot = transform;
        }

        HideVisuals();
    }

    private void LateUpdate()
    {
        if (movementController == null || agentRoot == null)
        {
            HideVisuals();
            return;
        }

        bool shouldShow = movementController.IsMoving || movementController.IsBlocked;
        if (!shouldShow)
        {
            HideVisuals();
            return;
        }

        Vector3 destination = movementController.CurrentDestination;
        Vector3 markerPosition = new Vector3(
            destination.x,
            floorY + markerHeightOffset,
            destination.z
        );
        Vector3 lineStart = new Vector3(
            agentRoot.position.x,
            floorY + lineHeightOffset,
            agentRoot.position.z
        );
        Vector3 lineEnd = new Vector3(
            destination.x,
            floorY + lineHeightOffset,
            destination.z
        );

        UpdateDestinationMarker(markerPosition);
        UpdatePathLine(lineStart, lineEnd);
    }

    private void UpdateDestinationMarker(Vector3 destination)
    {
        if (destinationMarker == null)
        {
            return;
        }

        if (!destinationMarker.gameObject.activeSelf)
        {
            destinationMarker.gameObject.SetActive(true);
        }

        destinationMarker.position = destination;
    }

    private void UpdatePathLine(Vector3 origin, Vector3 destination)
    {
        if (pathLine == null)
        {
            return;
        }

        if (!pathLine.enabled)
        {
            pathLine.enabled = true;
        }

        pathLine.useWorldSpace = true;
        pathLine.positionCount = 2;
        pathLine.SetPosition(0, origin);
        pathLine.SetPosition(1, destination);
    }

    private void HideVisuals()
    {
        if (destinationMarker != null && destinationMarker.gameObject.activeSelf)
        {
            destinationMarker.gameObject.SetActive(false);
        }

        if (pathLine != null)
        {
            pathLine.enabled = false;
            pathLine.positionCount = 0;
        }
    }
}
