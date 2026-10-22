using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text healthText;
    private Health health;

    void Start()
    {
        // Find Lars by his tag, so this works in every stage without dragging anything
        health = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
    }

    void Update()
    {
        float current = Mathf.Max(0f, health.CurrentHealth);
        fill.fillAmount = current / health.MaxHealth;
        healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(health.MaxHealth);
    }
}