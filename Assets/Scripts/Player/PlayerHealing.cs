public static class PlayerHealing
{
    public static float Boost(float amount) => amount * PlayerStats.Apply(StatType.HealingPower, 1f);
}