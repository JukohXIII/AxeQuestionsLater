using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    private PlayerGuard guard;
    [SerializeField] Color normalColor;
    
    [SerializeField] Color brokenColor;

    void Start()
    {
        // Find Lars by his tag, so this works in every stage without dragging anything
        guard = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerGuard>();
    }

    void Update()
    {
        float current = Mathf.Max(0f, guard.StaminaRatio);
        fill.fillAmount = current;
        fill.color = guard.IsGuardBroken ? brokenColor : normalColor;
    }
}