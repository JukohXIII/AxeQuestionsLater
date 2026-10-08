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
    [Tooltip("Horizontal distance at which the enemy starts attacking.")]
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
    [Tooltip("Chance per decision to pick a completely random action.")]
    [Range(0f, 1f)] public float decisionNoise = 0.3f;
    [Tooltip("How much the enemy favors melee attacks when in range.")]
    public float aggression = 0.7f;
    [Tooltip("Weight of the dash attack option when in dash range.")]
    public float dashAggression = 0.6f;
    [Tooltip("Chance to stand still instead of acting, per decision.")]
    public float hesitation = 0.15f;

    [Header("Melee attack")]
    public float meleeWindup = 0.25f;
    public float meleeActiveDuration = 0.2f;

    [Header("Dash attack")]
    [Tooltip("Long windup = the telegraph the player reacts to.")]
    public float dashWindup = 0.9f;
    public float dashSpeed = 14f;
    public float dashDuration = 0.35f;
    [Tooltip("Max horizontal distance at which it may start a dash.")]
    public float dashMaxDistance = 7f;
    [Tooltip("Minimum seconds between the START of two dashes (includes the windup, dash and recovery).")]
    public float dashCooldown = 5f;

    [Header("Timing (seconds)")]
    public float recoveryDuration = 0.6f;
    [Tooltip("Longer than the melee recovery: the dash is the risky move.")]
    public float dashRecoveryDuration = 1f;
    public float hitstunDuration = 0.4f;
    public float knockdownDuration = 5f;
    public float deathDelay = 1f;
}