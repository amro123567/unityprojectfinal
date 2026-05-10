using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Stats")]
    public string enemyName = "Enemy";
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    public float damage = 10f;

    [Header("Detection")]
    public float detectionRange = 5f;
    public float attackRange = 1.5f;

    [Header("Patrol")]
    public float patrolDistance = 4f;
    public float patrolWaitTime = 1f;
}
