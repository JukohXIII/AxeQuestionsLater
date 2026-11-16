using UnityEngine;

// Data that survives between scenes for one run.
public static class RunData
{
    public const float HealNodeFraction = 0.3f; // share of max health restored by a heal node

    public static float PlayerDamage;   // damage Lars carries into the next stage
    public static float PlayerMaxHealth; // max health at the end of the last stage , used by the map

    public static void HealPlayer(float fractionOfMax)
    {
        float amount = PlayerHealing.Boost(fractionOfMax * PlayerMaxHealth);
        PlayerDamage = Mathf.Max(0f, PlayerDamage - amount);
    }
}