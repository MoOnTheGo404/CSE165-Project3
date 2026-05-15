using UnityEngine;

public class HandGestureDetector : MonoBehaviour
{
    [SerializeField] private Transform indexFingertip;
    [SerializeField] private Transform indexKnuckle;
    [SerializeField] private Transform palmOrWrist;
    [SerializeField] private Transform thumbTip;
    [SerializeField, Range(0.01f, 1f)] private float directionSmoothing = 0.18f;

    public bool IsGestureActive { get; private set; }
    public Vector3 GestureOriginWorld { get; private set; }
    public Vector3 GestureDirectionWorld { get; private set; } = Vector3.forward;

    private bool previousGestureActive;

    private void Update()
    {
        bool active = TryUpdateEditorFallback();

        if (!active)
        {
            active = TryUpdatePointingGesture();
        }

        SetGestureActive(active);
    }

    private bool TryUpdatePointingGesture()
    {
        if (indexFingertip == null || indexKnuckle == null)
        {
            return false;
        }

        Vector3 rawDirection = indexFingertip.position - indexKnuckle.position;
        if (rawDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        GestureOriginWorld = palmOrWrist != null ? palmOrWrist.position : indexKnuckle.position;
        SmoothDirection(rawDirection.normalized);
        return true;
    }

    private bool TryUpdateEditorFallback()
    {
#if UNITY_EDITOR
        if (!Input.GetKey(KeyCode.Space))
        {
            return false;
        }

        Vector3 inputDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            inputDirection += Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            inputDirection += Vector3.back;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            inputDirection += Vector3.left;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            inputDirection += Vector3.right;
        }

        if (inputDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            inputDirection = transform.forward;
        }

        GestureOriginWorld = palmOrWrist != null ? palmOrWrist.position : transform.position;
        SmoothDirection(inputDirection.normalized);
        return true;
#else
        return false;
#endif
    }

    private void SmoothDirection(Vector3 targetDirection)
    {
        if (GestureDirectionWorld.sqrMagnitude <= Mathf.Epsilon)
        {
            GestureDirectionWorld = targetDirection;
            return;
        }

        GestureDirectionWorld = Vector3.Slerp(
            GestureDirectionWorld.normalized,
            targetDirection,
            directionSmoothing
        ).normalized;
    }

    private void SetGestureActive(bool active)
    {
        IsGestureActive = active;

        if (IsGestureActive == previousGestureActive)
        {
            return;
        }

        Debug.Log($"HandGestureDetector gesture {(IsGestureActive ? "active" : "inactive")}", this);
        previousGestureActive = IsGestureActive;
    }
}
