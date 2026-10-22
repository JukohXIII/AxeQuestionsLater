using UnityEngine;

[System.Serializable]
public class EnemyHitData
{
    public float damage = 10f;
    public float knockback = 12f;
    [Tooltip("Launch direction of the target. X is mirrored by the enemy's facing: write it for an enemy facing right.")]
    public Vector2 launchDirection = new Vector2(1f, 0.5f);
    [Tooltip("Center of the hit zone, relative to the enemy. X is mirrored by the facing.")]
    public Vector2 hitboxOffset = new Vector2(0.8f, 0f);
    public Vector2 hitboxSize = new Vector2(1f, 0.8f);
    public bool causesKnockback;
    public bool breaksGuard;
}


[CreateAssetMenu(fileName = "NewEnemyAttack", menuName = "Enemies/Attack")]
public class EnemyAttackData : ScriptableObject
{
    [Header("Timing(seconds)")]
    [Tooltip("Telegraph before the attack lands. Long = easy to react to.")]
    public float windup = 0.25f;
    [Tooltip("How long the hit zone is live (and how long the enemy moves, if it moves).")]
    public float activeDuration = 0.2f;
    [Tooltip("Punish window after the attack")]
    public float recovery = 0.6f;

    [Header("Movement during the active phase")]
    [Tooltip("0 = the enemy stays in place. Above 0 = it dashes toward where the player was when the windup started.")]
    public float moveSpeed = 0f;
    [Tooltip("The attack ends as soon as it connects (e.g. a dash that stops on contact).")]
    public bool endsOnHit;

    [Header("Damage")]
    public EnemyHitData hit = new EnemyHitData();

    [Header("When it can be used")]
    [Tooltip("Minimum horizontal distance to the player.")]
    public float minRange = 0f;
    [Tooltip("Maximum horizontal distance to the player.")]
    public float maxRange = 1.5f;
    public float cooldown = 0f;
    public bool requiresGrounded = true;

    [Header("Animation")]
    public string animationClip = "Mob_01_attack";
}