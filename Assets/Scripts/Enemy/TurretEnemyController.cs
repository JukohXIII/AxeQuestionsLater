using UnityEngine;

// Static ranged enemy ("spammer"). Reuses Attack, Recovery, Hitstun and Dead from EnemyController.
// Only the Idle is replaced, and it never goes to Chase or Retreat, so it never moves on its own.
public class TurretEnemyController : EnemyController
{
    [SerializeField] private EnemyAttackData attack;

    protected override IEnemyState CreateIdle() => new TurretIdleState(this, attack);
}