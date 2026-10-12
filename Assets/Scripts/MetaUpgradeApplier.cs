using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class MetaUpgradeApplier : MonoBehaviour
{
    private PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void OnEnable()
    {
        MetaProgression.OnChanged += Apply;
        Apply(); 
    }

    void OnDisable()
    {
        MetaProgression.OnChanged -= Apply;
    }

    void Apply() => MetaProgression.ApplyTo(stats);
}