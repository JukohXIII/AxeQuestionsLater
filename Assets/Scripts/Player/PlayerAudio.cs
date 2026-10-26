using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerAudio : MonoBehaviour
{
    private Health health;

    void Awake() => health = GetComponent<Health>();

    void OnEnable()
    {
        health.OnHit += Hurt;
        health.OnDeath += Died;
        health.OnBlocked += Blocked;
    }

    void OnDisable()
    {
        health.OnHit -= Hurt;
        health.OnDeath -= Died;
        health.OnBlocked -= Blocked;
    }

    void Hurt() => AudioManager.Play(health.LastHitCausedKnockdown ? Sfx.PlayerHurtHeavy : Sfx.PlayerHurt);
    void Died() => AudioManager.Play(Sfx.PlayerDeath);
    void Blocked() => AudioManager.Play(Sfx.Block);
}