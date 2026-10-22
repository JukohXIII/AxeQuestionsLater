using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Options/Attack")]
public class AttackOption : DecisionOption
{
    public EnemyAttackData attack;

    [Tooltip("Extra weight when the player is recovering from an attack.")]
    public float punishBonus = 0f;
    [Tooltip("Weight multiplier when the player has an attack incoming nearby. 0 = never.")]
    public float threatMultiplier = 0.3f;
    [Tooltip("Skip this attack when the player is airborne or dashing (too easy to dodge).")]
    public bool skipIfPlayerAirborne = false;

    public override float GetWeight(EnemyController enemy)
    {
        if (attack == null || !enemy.CanUse(attack)) return 0f;

        var movement = enemy.PlayerMovement;
        if(skipIfPlayerAirborne && movement != null && (!movement.IsGrounded) || movement.IsDashing)
            return 0f;

        float weight = baseWeight;
        if (enemy.IsPlayerRecovering) weight += punishBonus;
        if (enemy.IsPlayerAttackIncoming) weight *= threatMultiplier;
        return weight;
    }

    public override void Execute(EnemyController enemy)
    {
        enemy.StartAttack(attack);
    }
}