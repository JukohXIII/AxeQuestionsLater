using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Enemies/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float jumpVelocity = 5f;

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

    [Header("Timing (seconds)")]
    [Tooltip("Placeholder until the attack is designed")]
    public float attackDuration = 0.8f;
    public float recoveryDuration = 0.6f;
    public float hitstunDuration = 0.4f;
    public float deathDelay = 1f;
    public float knockdownDuration = 5f;
}