using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Option/Retreat")]
public class RetreatOption : DecisionOption
{
    public override float GetWeight(EnemyController enemy) => 
        enemy.IsPlayerAttackIncoming ? baseWeight : 0f;

    public override void Execute(EnemyController enemy) => enemy.GoTo(enemy.Retreat);
}