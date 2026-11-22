using UnityEngine;

// Static ranged enemy ("spammer") with a snowball stock that refills in the background.
// Reuses Recovery, Hitstun and Dead from EnemyController. Idle and Attack are replaced.
public class TurretEnemyController : EnemyController
{
    private const float KnockbackMultiplier = 2f; // light and squishy: flies further than a normal mob
    
    public const int MaxAmmo = 3;
    private const float ReloadTime = 2f;
    // Time between two throws of a burst. Matches the length of the throw clip (0.55 s)
    public const float ThrowInterval = 0.55f;
    // The animation listens to this to restart the throw clip at every snowball
    public event System.Action Thrown;
    public void NotifyThrown() => Thrown?.Invoke();
    // The snowman takes a decision at random moments, between these two delays (seconds)
    private const float MinDecisionDelay = 0.4f;
    private const float MaxDecisionDelay = 1.6f;

    // At each decision, chance to fire when the stock holds 1, 2, 3... snowballs. Otherwise it waits,
    // which lets the stock grow. One entry per snowball of MaxAmmo.
    private static readonly float[] FireChance = { 0.3f, 0.5f, 0.8f };

    [SerializeField] private EnemyAttackData attack;

    public int Ammo { get; private set; }
    private float reloadTimer;
    private float nextDecisionTime;
    protected override void Awake()
    {
        base.Awake();
        Health.KnockbackTakenMultiplier = KnockbackMultiplier;
        ScheduleNextDecision();
    }

    protected override void Update()
    {
        base.Update();
        TickReload();
    }

    protected override IEnemyState CreateIdle() => new TurretIdleState(this, attack);
    protected override IEnemyState CreateAttack() => new TurretAttackState(this);

    // The reload never stops, whatever the state (idle, attacking, stunned)
    void TickReload()
    {
        if (Health.IsDead || Ammo >= MaxAmmo) return;

        reloadTimer += Time.deltaTime;
        if (reloadTimer < ReloadTime) return;

        reloadTimer = 0f;
        Ammo++;
    }

    // Called by the Idle while the player is in range. At a random moment, fires a random number
    // of snowballs between 1 and the current stock.
    public bool ShouldFire()
    {
        if (Time.time < nextDecisionTime) return false;

        if (Ammo <= 0)
        {
            ScheduleNextDecision();   // nothing to throw: try again later
            return false;
        }

        if (Random.value > FireChance[Ammo - 1])
        {
            ScheduleNextDecision();   // decided to wait for more snowballs
            return false;
        }

        return true;   // the number of snowballs is decided throw after throw, not here
    }

    // After each throw of a burst, chance to keep throwing while the stock allows it
    private const float ContinueChance = 0.5f;

    public bool ShouldContinueBurst() => Ammo > 0 && Random.value < ContinueChance;

    public void ScheduleNextDecision() =>
        nextDecisionTime = Time.time + Random.Range(MinDecisionDelay, MaxDecisionDelay);

    public void ConsumeAmmo() => Ammo = Mathf.Max(0, Ammo -1);
}