using UnityEngine;

public static class DevTools
{
    public static void ResetEverything()
    {
        GameProgress.ResetAll();
        PlayerStats.ClearPermanent();
        PlayerStats.ClearRun();
        RunCards.Clear();
        RunData.PlayerDamage = 0f;
        Debug.Log("[DevTools] Save and run state reset");
    }
}