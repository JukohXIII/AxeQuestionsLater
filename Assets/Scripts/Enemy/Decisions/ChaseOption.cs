using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Options/Chase")]
public class ChaseOption : DecisionOption
{
    [Tooltip("Weight multiplier when the player has an attack incoming nearby.")]
    public float threatMultiplier = 0.3f;

    public override float GetWeight(EnemyController enemy)
    {
        if (enemy.IsPlayerInAttackRange()) return 0f;
        float weight = baseWeight;
        if(enemy.IsPlayerAttackIncoming) weight *= threatMultiplier;
        return weight;
    }

    public override void Execute(EnemyController enemy)
    {
        enemy.GoTo(enemy.Chase);
    }
}