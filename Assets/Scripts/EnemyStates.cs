using UnityEngine;

// IDLE: no active target. Also the decision hub:
// Hitstun and Recovery come back here and the enemy re-evaluates.
public class IdleState : EnemyState
{
    public IdleState(EnemyController e) : base(e) { }

    public override void Tick()
    {
        var config = enemyController.Config;

        if (enemyController.IsGrounded())
            enemyController.StopHorizontal();

        // No target: nothing to decide
        if (enemyController.DistanceToPlayer() > config.detectionRange)
            return;

        bool inRange = enemyController.IsPlayerInAttackRange();
        bool inDash = enemyController.IsPlayerInDashRange();

        switch (enemyController.Decider.Current)
        {
            case EnemyAction.Attack:
                if (inRange && enemyController.IsGrounded())
                    enemyController.StartAttack(AttackKind.Melee);
                else if (!inRange)
                    enemyController.ChangeState(enemyController.Chase);
                break;

            case EnemyAction.DashAttack:
                if (inDash)
                    enemyController.StartAttack(AttackKind.Dash);
                else if (!inRange)
                    enemyController.ChangeState(enemyController.Chase);
                break;

            case EnemyAction.Chase:
                if (!inRange)
                    enemyController.ChangeState(enemyController.Chase);
                break;

            // Wait: stay in Idle
        }
    }
}

// CHASE: move toward the player, jump if needed.
public class ChaseState : EnemyState
{
    public ChaseState(EnemyController e) : base(e) { }

    public override void Tick()
    {
        var config = enemyController.Config;

        if (enemyController.DistanceToPlayer() > config.detectionRange)
        {
            enemyController.ChangeState(enemyController.Idle);
            return;
        }

        EnemyAction action = enemyController.Decider.Current;
        bool inRange = enemyController.IsPlayerInAttackRange();

        if (action == EnemyAction.Attack && inRange && enemyController.IsGrounded())
        {
            enemyController.StartAttack(AttackKind.Melee);
            return;
        }

        if (action == EnemyAction.DashAttack && enemyController.IsPlayerInDashRange())
        {
            enemyController.StartAttack(AttackKind.Dash);
            return;
        }

        // Decided to wait, or in range but not attacking: back to the decision hub
        if (action == EnemyAction.Wait || inRange)
        {
            enemyController.ChangeState(enemyController.Idle);
            return;
        }

        float heightGap = enemyController.VerticalOffsetToPlayer();
        bool playerAbove = heightGap > config.jumpHeightThreshold;
        bool reachable = heightGap <= enemyController.MaxJumpHeight();

        float dx = enemyController.Player.position.x - enemyController.transform.position.x;
        bool underPlayer = Mathf.Abs(dx) < 0.3f;

        // Directly below the player: stop steering, no left-right jitter
        if (playerAbove && underPlayer)
        {
            enemyController.StopHorizontal();
            if (reachable && enemyController.CanJump())
                enemyController.Jump();
            return;
        }

        enemyController.MoveHorizontal(enemyController.DirectionToPlayer() * config.moveSpeed);

        if (enemyController.CanJump() && ((playerAbove && reachable) || enemyController.IsObstacleAhead()))
            enemyController.Jump();
    }
}

// ATTACK: placeholder, a single timed block.
// Split into Windup / Hit1 / Hit2 once the attacks are designed.
public class AttackState : EnemyState
{
    enum Phase { Windup, Active }

    Phase phase;
    AttackKind kind;
    float timer;
    int dashDir;

    public AttackState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        var c = enemyController.Config;
        kind = enemyController.CurrentAttackKind;

        enemyController.StopHorizontal();

        // Direction is locked at the start of the windup: the player can dodge it
        dashDir = enemyController.DirectionToPlayer();
        enemyController.Face(dashDir);

        phase = Phase.Windup;
        timer = kind == AttackKind.Dash ? c.dashWindup : c.meleeWindup;
    }

    public override void Tick()
    {
        var c = enemyController.Config;
        timer -= Time.deltaTime;

        if (phase == Phase.Windup)
        {
            if (timer > 0f) return;

            phase = Phase.Active;
            timer = kind == AttackKind.Dash ? c.dashDuration : c.meleeActiveDuration;
            // TODO: enable the hitbox here
            return;
        }

        // Active phase
        if (kind == AttackKind.Dash)
        {
            enemyController.MoveHorizontal(dashDir * c.dashSpeed);

            // A wall ends the dash early
            if (enemyController.IsObstacleAhead())
                timer = 0f;
        }

        if (timer <= 0f)
        {
            enemyController.StopHorizontal();
            enemyController.ChangeState(enemyController.Recovery);
        }
    }

    public override void Exit()
    {
        // TODO: disable the hitbox
        // Do NOT touch the velocity here: Exit is also called when a hit
        // interrupts the attack, and it would cancel the knockback.
    }
}

// RECOVERY: punish window, stands still and vulnerable.
public class RecoveryState : EnemyState
{
    float timer;

    public RecoveryState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        var c = enemyController.Config;
        enemyController.StopHorizontal();
        timer = enemyController.CurrentAttackKind == AttackKind.Dash
            ? c.dashRecoveryDuration
            : c.recoveryDuration;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            enemyController.ChangeState(enemyController.Idle);
    }

    public override void Exit()
    {
        enemyController.Decider.ForceRedecide();
    }
}

// HITSTUN: just got hit. Does NOT touch velocity, so the knockback
// from Health.cs plays out. Re-entering resets the timer (combos).
public class HitstunState : EnemyState
{
    float timer;

    public HitstunState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        timer = enemyController.Config.hitstunDuration;
        // TODO: hit animation / flash
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f && enemyController.IsGrounded())
            enemyController.ChangeState(enemyController.Knockdown);
    }

    public override void Exit()
    {
        enemyController.Decider.ForceRedecide();
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
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            enemyController.ChangeState(enemyController.Idle);
    }

    public override void Exit()
    {
        enemyController.Decider.ForceRedecide();
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
