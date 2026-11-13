using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private EnemyController enemy;
    private IEnemyState lastState;
    private Grabbable grabbable;
    private bool wasGrabbed;
    private bool lastDeadSettled;

    void Awake()
    {
        enemy = GetComponent<EnemyController>();
        grabbable = GetComponent<Grabbable>();
    }

    void Update()
    {
        bool grabbed = grabbable != null && grabbable.IsGrabbed;
        if (grabbed)
        {
            if (!wasGrabbed) animator.Play("Mob_01_inGrab", 0, 0f);
            wasGrabbed = true;
            return;
        }
        if (wasGrabbed)
        {
            wasGrabbed = false;
            lastState = null; // force the normal clip to play again after release
        }
        IEnemyState state = enemy.CurrentState;
        bool deadSettled = state == enemy.Dead && enemy.Dead is DeadState d && d.Settled;
        if (state == lastState && deadSettled == lastDeadSettled) return;
        lastState = state;
        lastDeadSettled = deadSettled;

        if (state == enemy.Chase || state == enemy.Retreat)
            animator.Play("Mob_01_walk");
        else if (state == enemy.Attack)
            animator.Play(enemy.CurrentAttack.animationClip, 0, 0f);
        else if (state == enemy.Dead)
            animator.Play(deadSettled ? "Mob_01_dying" : "Mob_01_getHit", 0, 0f);
        else if (state == enemy.Hitstun)
            animator.Play("Mob_01_getHit", 0, 0f);
        else
            animator.Play("Mob_01_idle");
    }
}