using UnityEngine;

/// <summary>
/// Drives the Enemy clips: Running while moving, Hitting on attack.
/// </summary>
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController controller;

    private static readonly int Hit = Animator.StringToHash("Hit");

    private void Awake()
    {
        animator = AnimationRig.BindAnimator(gameObject, animator, controller);
    }

    public void PlayHit()
    {
        if (AnimationRig.IsReady(animator)) animator.SetTrigger(Hit);
    }
}
