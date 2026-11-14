using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StaminaBar : MonoBehaviour
{
    private PlayerGuard guard;
    [SerializeField] Color brokenColor;
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private Transform segmentParent;   // drag "Segments" here
    [SerializeField] private Color fullColor;
    [SerializeField] private Color chargingColor;
    private readonly List<Image> fills = new ();
    private int builtCount;

    void Start()
    {
        guard = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerGuard>();
        Build();
    }

    void Build()
    {
        foreach (Transform child in segmentParent) Destroy(child.gameObject);
        fills.Clear();
        builtCount = Mathf.CeilToInt(guard.MaxStamina);
        for (int i = 0; i < builtCount; i++)
        {
            GameObject seg = Instantiate(segmentPrefab, segmentParent);
            fills.Add(seg.transform.Find("Fill").GetComponent<Image>());
        }
    }

    void Update()
    {
        if (Mathf.CeilToInt(guard.MaxStamina) != builtCount) Build();   // max changed (upgrade)

        for (int i = 0; i < fills.Count; i++)
        {
            float amount = Mathf.Clamp01(guard.Stamina - i);   // segment i: 0 = empty, 1 = full
            fills[i].fillAmount = amount;
            fills[i].color = guard.IsGuardBroken ? brokenColor : (amount >= 1f ? fullColor : chargingColor);
        }
    }
}