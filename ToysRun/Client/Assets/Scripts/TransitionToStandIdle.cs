using UnityEngine;

public class TransitionToStandIdle : StateMachineBehaviour
{
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var avatarManager = animator.gameObject.GetComponent<AvatarManager>();
        avatarManager.OnAnimatorStandIdle();
    }
}
