using UnityEngine;

public class OVRHandGestureInput : MonoBehaviour
{
    [SerializeField] private OVRHand hand;
    [SerializeField] private OVRSkeleton skeleton;
    [SerializeField] private bool requireHighConfidence = true;
    [SerializeField] private bool requireIndexPinch = true;
    [SerializeField] private float minimumHoldTime = 0.2f;
    [SerializeField] private LayerMask targetSurfaceMask;
    [SerializeField] private float raycastDistance = 5f;

    public bool IsGestureActive { get; private set; }
    public bool HasValidTarget { get; private set; }
    public Vector3 RayOriginWorld { get; private set; }
    public Vector3 RayDirectionWorld { get; private set; } = Vector3.forward;
    public Vector3 TargetPointWorld { get; private set; }
    public Transform LastHitTransform { get; private set; }

    private const float TargetChangeLogDistance = 0.05f;

    private float candidateHoldTime;
    private bool previousGestureActive;
    private bool previousHasValidTarget;
    private Vector3 previousTargetPointWorld;
    private Transform previousHitTransform;

    private void Awake()
    {
        if (hand == null)
        {
            hand = GetComponent<OVRHand>();
        }

        if (skeleton == null)
        {
            skeleton = GetComponent<OVRSkeleton>();
        }
    }

    private void Update()
    {
        bool candidateActive = TryUpdateRayPose() && MeetsGestureRequirements();
        UpdateDebouncedGesture(candidateActive);
        UpdateTarget();
        LogStateChanges();
    }

    private bool MeetsGestureRequirements()
    {
        if (hand == null || !hand.IsTracked)
        {
            return false;
        }

        if (requireHighConfidence && !hand.IsDataHighConfidence)
        {
            return false;
        }

        if (requireIndexPinch && !hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            return false;
        }

        return true;
    }

    private bool TryUpdateRayPose()
    {
        if (hand != null && hand.IsPointerPoseValid && hand.PointerPose != null)
        {
            RayOriginWorld = hand.PointerPose.position;
            RayDirectionWorld = hand.PointerPose.forward.normalized;
            return RayDirectionWorld.sqrMagnitude > Mathf.Epsilon;
        }

        return TryUpdateRayPoseFromSkeleton();
    }

    private bool TryUpdateRayPoseFromSkeleton()
    {
        if (skeleton == null || skeleton.Bones == null)
        {
            return false;
        }

        Transform wristRoot = null;
        Transform indexTip = null;

        foreach (OVRBone bone in skeleton.Bones)
        {
            if (bone == null)
            {
                continue;
            }

            if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot)
            {
                wristRoot = bone.Transform;
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
            {
                indexTip = bone.Transform;
            }
        }

        if (wristRoot == null || indexTip == null)
        {
            return false;
        }

        Vector3 direction = indexTip.position - wristRoot.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        RayOriginWorld = indexTip.position;
        RayDirectionWorld = direction.normalized;
        return true;
    }

    private void UpdateDebouncedGesture(bool candidateActive)
    {
        if (!candidateActive)
        {
            candidateHoldTime = 0f;
            IsGestureActive = false;
            return;
        }

        candidateHoldTime += Time.deltaTime;
        IsGestureActive = candidateHoldTime >= minimumHoldTime;
    }

    private void UpdateTarget()
    {
        HasValidTarget = false;
        LastHitTransform = null;

        if (!IsGestureActive)
        {
            return;
        }

        if (!Physics.Raycast(
            RayOriginWorld,
            RayDirectionWorld,
            out RaycastHit hit,
            raycastDistance,
            targetSurfaceMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            return;
        }

        TargetPointWorld = hit.point;
        LastHitTransform = hit.transform;
        HasValidTarget = true;
    }

    private void LogStateChanges()
    {
        if (IsGestureActive != previousGestureActive)
        {
            Debug.Log($"OVRHandGestureInput gesture {(IsGestureActive ? "active" : "inactive")}", this);
            previousGestureActive = IsGestureActive;
        }

        bool targetChanged =
            HasValidTarget != previousHasValidTarget ||
            LastHitTransform != previousHitTransform ||
            (HasValidTarget && Vector3.Distance(TargetPointWorld, previousTargetPointWorld) > TargetChangeLogDistance);

        if (targetChanged)
        {
            Debug.Log(
                HasValidTarget
                    ? $"OVRHandGestureInput target: {LastHitTransform.name} at {TargetPointWorld:0.00}"
                    : "OVRHandGestureInput target cleared",
                this
            );

            previousHasValidTarget = HasValidTarget;
            previousTargetPointWorld = TargetPointWorld;
            previousHitTransform = LastHitTransform;
        }
    }
}
