using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string grabClip = "Mob_01_inGrab";
    private EnemyController enemy;
    private TurretEnemyController turret; // null for enemies without a burst
    private IEnemyState lastState;
    private Grabbable grabbable;
    private bool wasGrabbed;
    private bool lastDeadSettled;

    void Awake()
    {
        enemy = GetComponent<EnemyController>();
        turret = enemy as TurretEnemyController;
        grabbable = GetComponent<Grabbable>();
    }

    void OnEnable() { if (turret != null) turret.Thrown += OnThrown; }
    void OnDisable() { if (turret != null) turret.Thrown -= OnThrown; }

    void OnThrown() => animator.Play(enemy.CurrentAttack.throwClip, 0, 0f);

    void Update()
    {
        bool grabbed = grabbable != null && grabbable.IsGrabbed;
        if (grabbed)
        {
            if (!wasGrabbed) animator.Play(grabClip, 0, 0f);
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

        EnemyConfig cfg = enemy.Config;
        if (state == enemy.Chase || state == enemy.Retreat)
            animator.Play(cfg.walkClip);
        else if (state == enemy.Attack)
            animator.Play(enemy.CurrentAttack.animationClip, 0, 0f);
        else if (state == enemy.Dead)
            animator.Play(deadSettled ? cfg.dyingClip : cfg.hitClip, 0, 0f);
        else if (state == enemy.Hitstun)
            animator.Play(cfg.hitClip, 0, 0f);
        else
            animator.Play(cfg.idleClip);
    }
}