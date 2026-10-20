using UnityEngine;

public class EnemyDecider
{
    readonly EnemyController enemy;
    float nextDecisionTime;
    float[] weights;
    bool wasThreatened;
    bool wasPlayerRecovering;
    public DecisionOption LastDecision { get; private set; } // for debug display
    public EnemyDecider(EnemyController theEnemyController)
    {
        enemy = theEnemyController;
    }
    
    public void Tick()
    {
        WatchPlayer();
        if (Time.time < nextDecisionTime) return;

        nextDecisionTime = Time.time + enemy.Config.decisionInterval;
        LastDecision = Decide();
        LastDecision?.Execute(enemy);
    }


    // A change in the player's behavior can cut the decision timer short,
    // but only if the enemy "notices" it: that chance sets how sharp the enemy is.
    void WatchPlayer()
    {
        bool thretened = enemy.IsPlayerAttackIncoming;
        bool recovering = enemy.IsPlayerRecovering;

        bool changed = (thretened && !wasThreatened) || (recovering && !wasPlayerRecovering);
        if (changed && Random.value < enemy.Config.reactionChance)
            nextDecisionTime = 0f;

        wasThreatened = thretened;
        wasPlayerRecovering = recovering;
    }

    DecisionOption Decide()
    {
        var config = enemy.Config;
        var options = config.decisionOptions;
        if (options == null || options.Length == 0) return null;

        if(weights == null || weights.Length != options.Length)
            weights = new float[options.Length];

        // Every weight, computed once
        float total = 0f;
        int available = 0;
        for (int i = 0; i < options.Length; i++)
        {
            weights[i] = options[i] != null ? Mathf.Max(0f, options[i].GetWeight(enemy)) : 0f;
            total += weights[i];
            if (weights[i] > 0f) available++;
        }
        if (total <= 0f) return null;

        // A mistake: random pick among what IS possible, ignoring the weights
        if (Random.value < config.decisionNoise)
        {
            int pick = Random.Range(0, available);
            for (int i = 0; i < options.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                if(pick == 0) return options[i];
                pick--;
            }
        }

        // Weighted random pick
        float roll = Random.value * total;
        for (int i = 0; i < options.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f && weights[i] > 0f) return options[i];
        }

        for (int i = options.Length - 1; i >= 0; i--)
            if (weights[i] > 0f) return options[i];

        return null;
    }

    public void ForceRedecide() => nextDecisionTime = 0f;
}

    