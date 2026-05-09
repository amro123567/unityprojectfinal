using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Data")]
    public EnemyData data;

    [Header("References")]
    public Animator animator;

    private Transform player;
    private HeroKnight playerScript;
    private float currentHealth;
    private Vector3 startPosition;
    private float patrolTarget;
    private float patrolTimer;
    private float attackCooldown = 0f;
    private bool movingRight = true;
    private bool isDead = false;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool defaultFacingRight;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    void Start()
    {
        currentHealth = data.maxHealth;
        startPosition = transform.position;
        SetPatrolTarget();

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Detect original facing direction from Y rotation
        defaultFacingRight = (transform.eulerAngles.y < 90f || transform.eulerAngles.y > 270f);

        // Reset Y rotation so flip logic works cleanly
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z);

        if (animator == null)
            animator = GetComponent<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerScript = p.GetComponent<HeroKnight>();
        }

        // Ignore physical collision with player (no pushing)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Collider2D enemyCol = GetComponent<Collider2D>();
            Collider2D playerCol = playerObj.GetComponent<Collider2D>();
            if (enemyCol != null && playerCol != null)
                Physics2D.IgnoreCollision(enemyCol, playerCol);
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        attackCooldown -= Time.deltaTime;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer <= data.attackRange)
            currentState = State.Attack;
        else if (distToPlayer <= data.detectionRange)
            currentState = State.Chase;
        else
            currentState = State.Patrol;

        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase:  Chase();  break;
            case State.Attack: AttackPlayer(); break;
        }
    }

    // BEHAVIOR 1: Patrol
    void Patrol()
    {
        SetAnim("Walking", true);

        float dir = patrolTarget - transform.position.x;
        if (rb != null)
            rb.velocity = new Vector2(Mathf.Sign(dir) * data.moveSpeed * 0.5f, rb.velocity.y);

        FlipToward(patrolTarget);

        if (Mathf.Abs(transform.position.x - patrolTarget) < 0.15f)
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            SetAnim("Walking", false);
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f)
            {
                movingRight = !movingRight;
                SetPatrolTarget();
            }
        }
    }

    // BEHAVIOR 2: Chase
    void Chase()
    {
        SetAnim("Walking", true);
        FlipToward(player.position.x);

        float dir = player.position.x - transform.position.x;
        if (rb != null)
            rb.velocity = new Vector2(Mathf.Sign(dir) * data.moveSpeed, rb.velocity.y);
    }

    // BEHAVIOR 3: Attack
    void AttackPlayer()
    {
        if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
        SetAnim("Walking", false);
        FlipToward(player.position.x);

        if (attackCooldown <= 0f)
        {
            TriggerAnim("Attack");
            attackCooldown = 1.5f;

            // 🔊 Enemy attack sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEnemyAttack();

            if (playerScript != null)
                playerScript.TakeDamage(data.damage);
            else
                Debug.LogWarning(gameObject.name + ": playerScript null — is HeroKnight tagged 'Player'?");
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        Debug.Log(gameObject.name + " HP: " + currentHealth);
        TriggerAnim("Hit");

        // 🔊 Enemy hurt sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyHurt();

        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        isDead = true;
        TriggerAnim("Die");

        // 🔊 Enemy death sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDeath();

        if (rb != null) rb.velocity = Vector2.zero;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        this.enabled = false;
        Destroy(gameObject, 2f);
    }

    void FlipToward(float targetX)
    {
        if (sr == null) return;
        float dir = targetX - transform.position.x;
        if (dir > 0.05f)       sr.flipX = !defaultFacingRight;
        else if (dir < -0.05f) sr.flipX = defaultFacingRight;
    }

    void SetAnim(string param, bool value)
    {
        if (animator != null) animator.SetBool(param, value);
    }

    void TriggerAnim(string param)
    {
        if (animator != null) animator.SetTrigger(param);
    }

    void SetPatrolTarget()
    {
        patrolTimer = data.patrolWaitTime;
        patrolTarget = startPosition.x + (movingRight ? data.patrolDistance : -data.patrolDistance);
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
