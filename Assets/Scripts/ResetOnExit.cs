using UnityEngine;

public class ResetOnExit : StateMachineBehaviour {
    [SerializeField] string locomotionState = "Locomotion";
    [SerializeField] bool rebindOnExit = true;
    [SerializeField] bool disableHitbox = true;

    public override void OnStateExit(Animator animator, AnimatorStateInfo s, int layerIndex) {
        if (disableHitbox) animator.GetComponentInChildren<WeaponHitbox>()?.DisableHitbox();
        animator.CrossFadeInFixedTime(locomotionState, 0.05f);
        if (rebindOnExit) { animator.Rebind(); animator.Update(0f); }
    }
}
