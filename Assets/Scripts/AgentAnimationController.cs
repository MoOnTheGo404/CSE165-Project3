using UnityEngine;

public class AgentAnimationController : MonoBehaviour
{
    [SerializeField] private AgentMovementController movementController;
    [SerializeField] private Animator animator;
    [SerializeField] private string isWalkingParameter = "IsWalking";
    [SerializeField] private string speedParameter = "Speed";

    private int isWalkingHash;
    private int speedHash;
    private bool hasIsWalkingParameter;
    private bool hasSpeedParameter;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimatorParameters();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            CacheAnimatorParameters();
        }
    }

    private void Update()
    {
        if (animator == null || movementController == null)
        {
            return;
        }

        if (hasIsWalkingParameter)
        {
            animator.SetBool(isWalkingHash, movementController.IsMoving);
        }

        if (hasSpeedParameter)
        {
            animator.SetFloat(speedHash, movementController.CurrentVelocity.magnitude);
        }
    }

    private void CacheAnimatorParameters()
    {
        isWalkingHash = Animator.StringToHash(isWalkingParameter);
        speedHash = Animator.StringToHash(speedParameter);
        hasIsWalkingParameter = false;
        hasSpeedParameter = false;

        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == isWalkingHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsWalkingParameter = true;
            }

            if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParameter = true;
            }
        }
    }
}
