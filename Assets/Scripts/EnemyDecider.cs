using UnityEngine;

public enum EnemyAction { Chase, Attack, DashAttack, Wait }
public enum AttackKind {Melee, Dash}

public class EnemyDecider
{
    readonly EnemyController enemyController;
    float nextDecisionTime;
    EnemyAction currentAction = EnemyAction.Chase;

    public EnemyDecider(EnemyController theEnemyController)
    {
        enemyController = theEnemyController;
    }

    public EnemyAction Current
    {
        get
        {
            if(Time.time >= nextDecisionTime)
            {
                currentAction = Decide();
                nextDecisionTime = Time.time + enemyController.Config.decisionInterval;
            }
            return currentAction;
        }
    }

    EnemyAction Decide()
    {
        var config = enemyController.Config;

        if (Random.value < config.decisionNoise)
            return (EnemyAction)Random.Range(0, 4);   // 4 = number of EnemyAction values

        bool inMelee = enemyController.IsPlayerInAttackRange();
        bool inDash  = enemyController.IsPlayerInDashRange();

        float attack = inMelee ? config.aggression : 0f;
        float dash   = inDash ? config.dashAggression : 0f;
        float chase  = inMelee ? 0.1f : 1f - config.hesitation;
        float wait   = config.hesitation;

        float roll = Random.value * (attack + dash + chase + wait);
        if (roll < attack) return EnemyAction.Attack;
        if (roll < attack + dash) return EnemyAction.DashAttack;
        if (roll < attack + dash + chase) return EnemyAction.Chase;
        return EnemyAction.Wait;
    }

    public void ForceRedecide() => nextDecisionTime = 0f;
}