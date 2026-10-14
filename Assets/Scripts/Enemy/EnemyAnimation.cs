using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private EnemyController enemy;
    private IEnemyState lastState;

    void Awake()
    {
        enemy = GetComponent<EnemyController>();
    }

    void Update()
    {
        IEnemyState state = enemy.CurrentState;
        if (state == lastState) return;
        lastState = state;

        if (state == enemy.Chase)
            animator.Play("Mob_01_walk");
        else if (state == enemy.Attack)
            animator.Play("Mob_01_attack", 0, 0f);
        else if (state == enemy.Hitstun || state == enemy.Knockdown || state == enemy.Dead)
            animator.Play("Mob_01_getHit", 0, 0f);
        else
            animator.Play("Mob_01_idle");
    }
}