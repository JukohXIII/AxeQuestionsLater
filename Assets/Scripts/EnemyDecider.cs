using UnityEngine;

public enum EnemyAction { Chase, Attack, Wait }

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

        // Occasionally ignore the weights entirely: a dumb, irrational choice
        if (Random.value < config.decisionNoise)
            return (EnemyAction)Random.Range(0, 3);

        bool inRange = enemyController.IsPlayerInAttackRange();
        float attack = inRange ? config.aggression : 0f;
        float chase  = inRange ? 0.1f : 1f - config.hesitation;
        float wait   = config.hesitation;

        float roll = Random.value * (attack + chase + wait);
        if (roll < attack) return EnemyAction.Attack;
        if (roll < attack + chase) return EnemyAction.Chase;
        return EnemyAction.Wait;
    }

    public void ForceRedecide() => nextDecisionTime = 0f;
}