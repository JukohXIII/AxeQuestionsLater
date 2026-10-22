using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Options/Wait")]
public class WaitOption : DecisionOption
{
    public override float GetWeight(EnemyController enemy) => baseWeight;

    public override void Execute(EnemyController enemy) => enemy.GoTo(enemy.Idle);
}