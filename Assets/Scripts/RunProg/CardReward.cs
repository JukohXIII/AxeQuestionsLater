using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class CardReward : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private CardOfferPanel panel;
    [SerializeField, Min(1)] private int choices = 3;
    [SerializeField] private float delay = 1f;

    private void OnEnable() => spawner.OnAllWavesCleared += HandleCleared; 
    private void OnDisable() => spawner.OnAllWavesCleared -= HandleCleared; 

    private void HandleCleared() => StartCoroutine(OfferAfterDelay());

    [ContextMenu("Test offer")]
    private void TestOffer() => HandleCleared();
    private IEnumerator OfferAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        var offer = RunCards.Draw(choices);
        if (offer.Count > 0) panel.Open(offer);
    }

    [ContextMenu("Simulate 1000 offers")]
    private void Simulate()
    {
        var counts = new Dictionary<CardDefinition, int>();
        for (int i = 0; i < 1000; i++)
            foreach (var c in RunCards.Draw(choices))
                counts[c] = counts.TryGetValue(c, out int n) ? n + 1 : 1;

        var sb = new System.Text.StringBuilder("[Cards] 1000 offres :\n");
        foreach (var kv in counts)
            sb.AppendLine($"{kv.Key.displayName} ({kv.Key.rarity}) : {kv.Value}");
        Debug.Log(sb.ToString());
    }
}