using System.Collections;
using UnityEngine;

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
}