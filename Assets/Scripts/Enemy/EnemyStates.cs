using System.Collections.Generic;
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

        enemyController.Decider.Tick();
    }
}

// CHASE: move toward the player. Platforms are handled by the navigator.
public class ChaseState : EnemyState
{
    public ChaseState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        enemyController.Navigator.Reset();
    }

    public override void Tick()
    {
        var config = enemyController.Config;
        var nav = enemyController.Navigator;

        if (enemyController.DistanceToPlayer() > config.detectionRange)
        {
            enemyController.ChangeState(enemyController.Idle);
            return;
        }

        // Mid-jump between platforms: committed until landing, no decisions
        if (nav.IsAirborne && nav.Step()) return;

        // May switch to Attack, Retreat or Idle (wait)
        enemyController.Decider.Tick();
        if (enemyController.CurrentState != this) return;

        // Close enough but nothing chosen: back to the decision hub
        if (enemyController.IsPlayerInAttackRange())
        {
            enemyController.ChangeState(enemyController.Idle);
            return;
        }

        // Player on another platform: follow the platform path
        if (config.usePlatformNavigation && nav.Step()) return;

        // Same platform (or no path): walk toward the player, hop over obstacles
        enemyController.MoveHorizontal(enemyController.DirectionToPlayer() * config.moveSpeed);

        if (enemyController.CanJump() && enemyController.IsObstacleAhead())
            enemyController.Jump();
    }
}

// ATTACK: runs whatever EnemyAttackData it was started with
public class AttackState : EnemyState
{
    enum Phase { Windup, Active }

    Phase phase;
    EnemyAttackData attack;
    float timer;
    int moveDir;
    readonly HashSet<Health> alreadyHit = new HashSet<Health>();

    public AttackState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        attack = enemyController.CurrentAttack;

        enemyController.StopHorizontal();

        moveDir = enemyController.DirectionToPlayer();
        enemyController.Face(moveDir);

        alreadyHit.Clear();
        phase = Phase.Windup;
        timer = attack.windup;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;

        if (phase == Phase.Windup)
        {
            if (timer > 0f) return;

            phase = Phase.Active;
            timer = attack.activeDuration;
            return;
        }

        if (attack.moveSpeed > 0f)
        {
            enemyController.MoveHorizontal(moveDir * attack.moveSpeed);

            // A wall ends the movement early
            if (enemyController.IsObstacleAhead())
                timer = 0f;
        }

        bool hitSomeone = enemyController.DealHit(attack.hit, alreadyHit);
        if (attack.endsOnHit && hitSomeone)
            timer = 0f;   // e.g. a dash ends as soon as it connects

        if (timer <= 0f)
        {
            enemyController.StopHorizontal();
            enemyController.ChangeState(enemyController.Recovery);
        }
    }

    // The hit zone is only tested during the Active phase, so an attack interrupted
    // by Hitstun can't hurt anyone. Do NOT touch the velocity here: it would cancel the knockback.
}

// RECOVERY: punish window, stands still and vulnerable.
public class RecoveryState : EnemyState
{
    float timer;

    public RecoveryState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        enemyController.StopHorizontal();
        timer = enemyController.CurrentAttack.recovery;
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
    bool causeKnockdown;
    float savedGravity;

    public HitstunState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        timer = enemyController.Config.hitstunDuration;
        if (enemyController.Health.LastHitCausedKnockdown) causeKnockdown = true;
        // TODO: hit animation / flash
        savedGravity = enemyController.Rb.gravityScale;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        bool floating = timer > 0f && !causeKnockdown && !enemyController.IsGrounded();
        enemyController.Rb.gravityScale = floating ? 0f : savedGravity;
        if (timer <= 0f && enemyController.IsGrounded())
        {
            IEnemyState next = causeKnockdown ? enemyController.Knockdown : enemyController.Idle;
            causeKnockdown = false;
            enemyController.ChangeState(next);
        }
    }

    public override void Exit()
    {
        enemyController.Rb.gravityScale = savedGravity;
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
        enemyController.Health.IsInvincible = true;
    }

    public override void Exit()
    {
        enemyController.Health.IsInvincible = false;
        enemyController.Decider.ForceRedecide();
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            enemyController.ChangeState(enemyController.Idle);
    }
}

public class RetreatState : EnemyState
{
    float timer;

    public RetreatState(EnemyController e) : base(e) { }

    public override void Enter()
    {
        timer = enemyController.Config.retreatDuration;
    }

    public override void Tick()
    {
        var config = enemyController.Config;
        timer -= Time.deltaTime;

        int away = -enemyController.DirectionToPlayer();
        enemyController.Face(enemyController.DirectionToPlayer());
        enemyController.Rb.linearVelocity = new Vector2(
            away * config.moveSpeed * config.retreatSpeedMultiplier,
            enemyController.Rb.linearVelocity.y
        );

        if (timer <= 0f)
        {
            enemyController.StopHorizontal();
            enemyController.ChangeState(enemyController.Idle);
        }
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