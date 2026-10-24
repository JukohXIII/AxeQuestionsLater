using UnityEngine;

public class HubRunReset : MonoBehaviour
{
    private void Awake() => RunCards.Clear();
}