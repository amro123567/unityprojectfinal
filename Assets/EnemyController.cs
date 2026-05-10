using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Data")]
    public EnemyData data;

    [Header("References")]
    public Animator animator;

    Transform player;
    HeroKnight playerScript;
    float currentHealth;
    Vector3 startPosition;
    float patrolTarget;
    float patrolTimer;
    float attackCooldown = 0f;
    bool movingRight = true;
    bool isDead = false;
    Rigidbody2D rb;
    SpriteRenderer sr;
    bool defaultFacingRight;

    enum State { Patrol, Chase, Attack }
    State currentState = State.Patrol;

    float walkingTriggerThrottle;

    bool TriggerExists(string parameterName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == parameterName && p.type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    bool TryFireTrigger(string parameterName)
    {
        if (animator == null || !TriggerExists(parameterName))
            return false;

        animator.SetTrigger(parameterName);
        return true;
    }

    void PulseWalkingAnimator(bool wantsMove)
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name != "Walking")
                continue;

            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool("Walking", wantsMove);
                    return;
                case AnimatorControllerParameterType.Trigger:
                    if (wantsMove)
                    {
                        walkingTriggerThrottle -= Time.deltaTime;
                        if (walkingTriggerThrottle <= 0f)
                        {
                            animator.SetTrigger("Walking");
                            walkingTriggerThrottle = 0.45f;
                        }
                    }
                    else
                    {
                        walkingTriggerThrottle = 0f;
                        animator.ResetTrigger("Walking");
                    }

                    return;
            }
        }
    }

    void FireMeleeAttackAnimator()
    {
        if (!TryFireTrigger("Attack"))
            TryFireTrigger("Attacking");
    }

    void FireHitAnimator()
    {
        if (!TryFireTrigger("Hit"))
            TryFireTrigger("Hurt");
    }

    void FireDeathAnimator()
    {
        if (!TryFireTrigger("Die"))
            TryFireTrigger("Death");
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogError($"{name}: assign EnemyData on EnemyController.", this);
            enabled = false;
            return;
        }

        currentHealth = data.maxHealth;
        startPosition = transform.position;
        SetPatrolTarget();

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        defaultFacingRight = (transform.eulerAngles.y < 90f || transform.eulerAngles.y > 270f);

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z);

        if (animator == null)
            animator = GetComponent<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerScript = p.GetComponent<HeroKnight>();
        }

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
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Attack:
                AttackPlayer();
                break;
        }
    }

    void Patrol()
    {
        PulseWalkingAnimator(true);

        float dir = patrolTarget - transform.position.x;
        if (rb != null)
            rb.velocity = new Vector2(Mathf.Sign(dir) * data.moveSpeed * 0.5f, rb.velocity.y);

        FlipToward(patrolTarget);

        if (Mathf.Abs(transform.position.x - patrolTarget) < 0.15f)
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            PulseWalkingAnimator(false);
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f)
            {
                movingRight = !movingRight;
                SetPatrolTarget();
            }
        }
    }

    void Chase()
    {
        PulseWalkingAnimator(true);
        FlipToward(player.position.x);

        float dir = player.position.x - transform.position.x;
        if (rb != null)
            rb.velocity = new Vector2(Mathf.Sign(dir) * data.moveSpeed, rb.velocity.y);
    }

    void AttackPlayer()
    {
        if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
        PulseWalkingAnimator(false);
        FlipToward(player.position.x);

        if (attackCooldown <= 0f)
        {
            FireMeleeAttackAnimator();
            attackCooldown = 1.5f;

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
        FireHitAnimator();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyHurt();

        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        isDead = true;
        FireDeathAnimator();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDeath();

        if (rb != null) rb.velocity = Vector2.zero;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        enabled = false;
        Destroy(gameObject, 2f);
    }

    void FlipToward(float targetX)
    {
        if (sr == null) return;
        float dir = targetX - transform.position.x;
        if (dir > 0.05f) sr.flipX = !defaultFacingRight;
        else if (dir < -0.05f) sr.flipX = defaultFacingRight;
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
