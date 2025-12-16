using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        var hitbox = animator.GetComponentInChildren<MeleeHitbox>();
        if (hitbox != null)
            hitbox.BeginHit();
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        var hitbox = animator.GetComponentInChildren<MeleeHitbox>();
        if (hitbox != null)
            hitbox.EndHit();
    }
}
