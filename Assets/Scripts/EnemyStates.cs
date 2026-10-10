using UnityEngine;

// IDLE: no active target. Also the decision hub:
// Hitstun and Recovery come back here and the enemy re-evaluates.
public class IdleState : EnemyState
{
    public IdleState(EnemyController e) : base(e) { }

    public override void Tick()
    {
        if (enemyController.IsGrounded()) enemyController.StopHorizontal();

        if (enemyController.DistanceToPlayer() <= enemyController.Config.detectionRange)
            enemyController.ChangeState(enemyController.Chase);
    }
}

// CHASE: move toward the player, jump if needed.
public class ChaseState : EnemyState
{
    public ChaseState(EnemyController e) : base(e) { }

    public override void Tick()
    {
        var c = enemyController.Config;

        if (enemyController.DistanceToPlayer() > c.detectionRange)
        {
            enemyController.ChangeState(enemyController.Idle);
            return;
        }

        if (enemyController.IsPlayerInAttackRange() && enemyController.IsGrounded())
        {
            enemyController.ChangeState(enemyController.Attack);
            return;
        }

        enemyController.MoveHorizontal(enemyController.DirectionToPlayer() * c.moveSpeed);

        bool playerAbove = enemyController.VerticalOffsetToPlayer() > c.jumpHeightThreshold;
        if (enemyController.IsGrounded() && (playerAbove || enemyController.IsObstacleAhead()))
            enemyController.Jump();
    }
}

// ATTACK: placeholder, a single timed block.
// Split into Windup / Hit1 / Hit2 once the attacks are designed.
public class AttackState : EnemyState
{
    float timer;

    public AttackState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        enemyController.StopHorizontal();
        enemyController.Face(enemyController.DirectionToPlayer());
        timer = enemyController.Config.attackDuration;
        // TODO: trigger attack animation / enable hitbox
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            enemyController.ChangeState(enemyController.Recovery);
    }

    public override void Exit()
    {
        // TODO: disable the hitbox (the attack can be interrupted by a hit)
    }
    
}

// RECOVERY: punish window, stands still and vulnerable.
public class RecoveryState : EnemyState
{
    float timer;

    public RecoveryState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        enemyController.StopHorizontal();
        timer = enemyController.Config.recoveryDuration;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            enemyController.ChangeState(enemyController.Idle);
    }
}

// HITSTUN: just got hit. Does NOT touch velocity, so the knockback
// from Health plays out. Re-entering resets the timer (combos).
public class HitstunState : EnemyState
{
    float timer;
    bool causeKnockdown;

    public HitstunState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        timer = enemyController.Config.hitstunDuration;
        if (enemyController.Health.LastHitCausedKnockdown) causeKnockdown = true;
        // TODO: hit animation / flash
    }

    public override void Tick()
    {
        if (!enemyController.IsGrounded()) causeKnockdown = true;
        timer -= Time.deltaTime;
        if (timer <= 0f && enemyController.IsGrounded())
        {
            IEnemyState next = causeKnockdown ? enemyController.Knockdown : enemyController.Idle;
            causeKnockdown = false;
            enemyController.ChangeState(next);
        }
    }
}

public class KnockdownState : EnemyState
{
    float timer;

    public KnockdownState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        enemyController.StopHorizontal();
        timer = enemyController.Config.knockdownDuration;
        enemyController.Health.IsInvincible = true;
    }

    public override void Exit()
    {
        enemyController.Health.IsInvincible = false;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            enemyController.ChangeState(enemyController.Idle);
    }
}

// DEAD: terminal state.
public class DeadState : EnemyState
{
    public DeadState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        enemyController.StopHorizontal();
        // TODO: death animation / VFX
        Object.Destroy(enemyController.gameObject, enemyController.Config.deathDelay);
    }
}
