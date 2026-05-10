using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats", order = 0)]
public class PlayerStats : ScriptableObject
{
    public float moveSpeed = 4f;
    public float jumpForce = 7.5f;
    public float rollForce = 6f;
    public float maxHealth = 100f;
    public float attackDamage = 25f;
    public float attackRange = 1.5f;
}
