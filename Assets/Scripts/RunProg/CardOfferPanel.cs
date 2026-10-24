using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CardOfferPanel : MonoBehaviour
{
    [SerializeField] private CardButton[] slots;

    private PlayerInput player;

    public void Open(List<CardDefinition> offer)
    {
        gameObject.SetActive(true);

        for (int i = 0; i < slots.Length; i++)
        {
            bool used = i < offer.Count;
            slots[i].gameObject.SetActive(used);
            if (used) slots[i].Bind(offer[i], Choose);
        }

        LockPlayer();

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
    }

    private void Choose(CardDefinition card)
    {
        RunCards.Take(card);
        gameObject.SetActive(false);
    }

    private void LockPlayer()
    {
      player = FindFirstObjectByType<PlayerInput>();
      if (player == null) return;
      player.DeactivateInput();
      var move = player.GetComponent<PlayerMovement>();
      if (move != null) move.ClearInput();  
    }

    private void OnDisable()
    {
        if (player != null) player.ActivateInput();
    }
}