using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Mirror";
    [SerializeField] private StatUpgradeData[] upgrades;
    [SerializeField] private UpgradeMenu menu;

    public string Prompt => $"[E] {stationName}";

    public void Interact(PlayerInteractor interactor)
    {
        menu.Open(stationName, upgrades, interactor);
    }
}