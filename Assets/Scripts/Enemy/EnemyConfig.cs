using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Enemies/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float jumpVelocity = 5f;
    [Tooltip("Minimum seconds between two jumps.")]
    public float jumpCooldown = 0.8f;

    [Header("Perception")]
    public float detectionRange = 5f;
    [Tooltip("Distance at which the enemy stops approaching and switches to deciding. Keep it close to the melee attack's max range.")]
    public float attackRange = 1.5f;
    [Tooltip("Max vertical gap with the player to allow an attack.")]
    public float verticalAttackTolerance = 1f;
    [Tooltip("Jump if the player is at least that much higher")]
    public float jumpHeightThreshold = 1.5f;
    [Tooltip("Distance ahead checked for obstacles to jump over.")]
    public float obstacleDetectionDistance = 0.5f;

    [Header("Decision making")]
    [Tooltip("Seconds between two decisions. Higher = slower reactions.")]
    public float decisionInterval = 0.5f;
    [Tooltip("Chance per decision to pick a random option among the possible ones, ignoring weights.")]
    [Range(0f, 1f)] public float decisionNoise = 0.3f;
    [Tooltip("What this enemy is able to do. Each option carries its own weight and rules.")]
    public DecisionOption[] decisionOptions;

    [Header("Reading the player")]
    [Tooltip("Chance to notice a change in the player's behavior (attack starting, recovery). 1 = always, 0.3 = often misses it.")]
    [Range(0f, 1f)] public float reactionChance = 0.5f;
    [Tooltip("Distance under which a player's attack counts as a threat.")]
    public float threatDistance = 2f;
    public float retreatDuration = 0.4f;
    public float retreatSpeedMultiplier = 1.5f;

    [Header("Timing (seconds)")]
    public float hitstunDuration = 0.4f;
    public float knockdownDuration = 1f;
    public float deathDelay = 1f;
}