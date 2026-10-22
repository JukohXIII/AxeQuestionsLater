using UnityEngine;

public abstract class DecisionOption : ScriptableObject
{
    [Tooltip("Base weight when this option is available. Higher = picked more often.")]
    public float baseWeight = 1f;

    public abstract float GetWeight(EnemyController enemy);

    public abstract void Execute(EnemyController enemy);
}