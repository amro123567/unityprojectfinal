using UnityEngine;
using System.Collections;

public class HeroKnight : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float m_speed = 4.0f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] float m_rollForce = 6.0f;
    [SerializeField] bool m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float attackDamage = 25f;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    // Private references
    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private Sensor_HeroKnight m_groundSensor;
    private Sensor_HeroKnight m_wallSensorR1;
    private Sensor_HeroKnight m_wallSensorR2;
    private Sensor_HeroKnight m_wallSensorL1;
    private Sensor_HeroKnight m_wallSensorL2;

    // Private state
    private float currentHealth;
    private bool m_isWallSliding = false;
    private bool m_grounded = false;
    private bool m_rolling = false;
    private bool isDead = false;
    private int m_facingDirection = 1;
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 8.0f / 14.0f;
    private float m_rollCurrentTime;

    void Start()
    {
        currentHealth = maxHealth;
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor  = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1  = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2  = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1  = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2  = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
    }

    void Update()
    {
        if (isDead) return;

        m_timeSinceAttack += Time.deltaTime;

        if (m_rolling) m_rollCurrentTime += Time.deltaTime;
        if (m_rollCurrentTime > m_rollDuration) m_rolling = false;

        // Ground check
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        float inputX = Input.GetAxis("Horizontal");

        if (inputX > 0)      { GetComponent<SpriteRenderer>().flipX = false; m_facingDirection = 1; }
        else if (inputX < 0) { GetComponent<SpriteRenderer>().flipX = true;  m_facingDirection = -1; }

        if (!m_rolling)
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);

        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) ||
                          (m_wallSensorL1.State() && m_wallSensorL2.State());
        m_animator.SetBool("WallSlide", m_isWallSliding);

        // Death
        if (Input.GetKeyDown("e") && !m_rolling)
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
        }

        // Attack
        else if (Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        {
            m_currentAttack++;
            if (m_currentAttack > 3) m_currentAttack = 1;
            if (m_timeSinceAttack > 1.0f) m_currentAttack = 1;

            m_animator.SetTrigger("Attack" + m_currentAttack);
            m_timeSinceAttack = 0.0f;

            // 🔊 Play attack sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAttack(m_currentAttack);

            DealDamageToEnemies();
        }

        // Block
        else if (Input.GetMouseButtonDown(1) && !m_rolling)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
        }
        else if (Input.GetMouseButtonUp(1))
            m_animator.SetBool("IdleBlock", false);

        // Roll
        else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        {
            m_rolling = true;
            m_rollCurrentTime = 0f;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);

            // 🔊 Play roll sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlayerRoll();
        }

        // Jump
        else if (Input.GetKeyDown("space") && m_grounded && !m_rolling)
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);

            // 🔊 Play jump sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlayerJump();
        }

        // Run
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }

        // Idle
        else
        {
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }

    // ── Combat ───────────────────────────────────────

    void DealDamageToEnemies()
    {
        Vector2 attackPoint = (Vector2)transform.position + Vector2.right * m_facingDirection * 0.8f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint, attackRange, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        m_animator.SetTrigger("Hurt");
        Debug.Log("Player HP: " + currentHealth);

        // 🔊 Play hurt sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerHurt();

        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        isDead = true;
        m_animator.SetBool("noBlood", m_noBlood);
        m_animator.SetTrigger("Death");
        m_body2d.velocity = Vector2.zero;

        // 🔊 Play death sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerDeath();

        this.enabled = false;
    }

    public float GetHealthPercent() => currentHealth / maxHealth;

    void OnDrawGizmosSelected()
    {
        Vector2 attackPoint = (Vector2)transform.position + Vector2.right * m_facingDirection * 0.8f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint, attackRange);
    }

    void AE_SlideDust()
    {
        Vector3 spawnPosition;
        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation);
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
