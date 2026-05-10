using UnityEngine;
using UnityEngine.SceneManagement;

public class HeroKnight : MonoBehaviour
{
    static readonly string SlidePoolTag = "SlideDust";

    [Header("Movement")]
    [SerializeField] float m_speed = 4.0f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] float m_rollForce = 6.0f;
    [SerializeField] bool m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    [Header("Data")]
    [SerializeField] PlayerStats designerProfile;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float attackDamage = 25f;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    Animator m_animator;
    Rigidbody2D m_body2d;
    Sensor_HeroKnight m_groundSensor;
    Sensor_HeroKnight m_wallSensorR1;
    Sensor_HeroKnight m_wallSensorR2;
    Sensor_HeroKnight m_wallSensorL1;
    Sensor_HeroKnight m_wallSensorL2;

    float currentHealth;
    bool m_isWallSliding;
    bool m_grounded;
    bool wasGrounded;
    bool m_rolling;
    bool isDead;
    int m_facingDirection = 1;
    int m_currentAttack;
    float m_timeSinceAttack;
    float m_delayToIdle;
    float m_rollDuration = 8.0f / 14.0f;
    float m_rollCurrentTime;
    int airJumpsRemaining;

    void Start()
    {
        HydrateConfigurableStats();

        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();

        Transform t = transform;
        m_groundSensor = t.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = t.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = t.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = t.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = t.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();

        currentHealth = maxHealth;

        HydrateFromSaveIfMatchingScene();
        RecomputeJumpBudget();
        wasGrounded = m_grounded;
    }

    void HydrateConfigurableStats()
    {
        if (designerProfile != null)
        {
            ApplyDesignProfile(designerProfile);
            return;
        }

        PlayerStats fallback = Resources.Load<PlayerStats>("PlayerStats_Default");

        if (fallback != null)
            ApplyDesignProfile(fallback);
    }

    public void ApplyDesignProfile(PlayerStats profile)
    {
        if (profile == null)
            return;

        m_speed = profile.moveSpeed;
        m_jumpForce = profile.jumpForce;
        m_rollForce = profile.rollForce;
        maxHealth = Mathf.Max(1f, profile.maxHealth);
        attackDamage = profile.attackDamage;
        attackRange = profile.attackRange;
    }

    void HydrateFromSaveIfMatchingScene()
    {
        SaveSystem svc = SaveSystem.Instance;
        if (svc == null || !svc.HasSave)
            return;

        SaveSystem.GameData data = svc.GetData();
        if (data == null)
            return;

        int idx = SceneManager.GetActiveScene().buildIndex;

        bool sameSceneSlot = idx == data.currentLevel;
        bool hasHp = data.playerHealth > 0.5f;

        if (!sameSceneSlot || !hasHp)
            return;

        Vector3 pos = transform.position;
        transform.position = new Vector3(data.playerX, data.playerY, pos.z);

        float cap = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(data.playerHealth, 1f, cap);
        maxHealth = cap;
    }

    void OnApplicationQuit()
    {
        WriteQuickSaveSnapshot();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            WriteQuickSaveSnapshot();
    }

    public void WriteQuickSaveSnapshot()
    {
        SaveSystem svc = SaveSystem.Instance;
        if (svc == null || isDead)
            return;

        int idx = Mathf.Max(1, SceneManager.GetActiveScene().buildIndex);

        svc.SaveGame(transform, currentHealth, idx, svc.GetData().score, svc.GetData().hasDoubleJump);
    }

    void RecomputeJumpBudget()
    {
        bool perk = SaveSystem.Instance != null && SaveSystem.Instance.GetData().hasDoubleJump;
        airJumpsRemaining = perk ? 1 : 0;
    }

    void Update()
    {
        if (isDead)
            return;

        wasGrounded = m_grounded;
        m_timeSinceAttack += Time.deltaTime;

        if (m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        if (m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
            RecomputeJumpBudget();
        }

        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        if (!wasGrounded && m_grounded)
            RecomputeJumpBudget();

        float inputX = Input.GetAxis("Horizontal");

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (inputX > 0)
        {
            spriteRenderer.flipX = false;
            m_facingDirection = 1;
        }
        else if (inputX < 0)
        {
            spriteRenderer.flipX = true;
            m_facingDirection = -1;
        }

        if (!m_rolling)
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);

        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) ||
                          (m_wallSensorL1.State() && m_wallSensorL2.State());
        m_animator.SetBool("WallSlide", m_isWallSliding);

        if (Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        {
            m_currentAttack++;
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            m_animator.SetTrigger("Attack" + m_currentAttack);
            m_timeSinceAttack = 0.0f;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAttack(m_currentAttack);

            DealDamageToEnemies();
        }
        else if (Input.GetMouseButtonDown(1) && !m_rolling)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            m_animator.SetBool("IdleBlock", false);
        }
        else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        {
            m_rolling = true;
            m_rollCurrentTime = 0f;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPlayerRoll();
        }
        else if (Input.GetKeyDown("space") && !m_rolling)
        {
            if (TryJumpRoutine())
                return;
        }
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }
        else
        {
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }

    bool TryJumpRoutine()
    {
        if (m_grounded)
        {
            PerformJumpImpulse(false);
            return true;
        }

        if (airJumpsRemaining > 0 && !m_isWallSliding)
        {
            airJumpsRemaining--;
            PerformJumpImpulse(true);
            return true;
        }

        return false;
    }

    void PerformJumpImpulse(bool airborneStyle)
    {
        m_animator.SetTrigger("Jump");

        if (!airborneStyle)
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", false);
            m_groundSensor.Disable(0.2f);
        }

        Vector2 velocity = m_body2d.velocity;
        m_body2d.velocity = new Vector2(velocity.x, m_jumpForce);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerJump();

        if (!airborneStyle)
            RecomputeJumpBudget();
    }

    void DealDamageToEnemies()
    {
        Vector2 attackPoint = (Vector2)transform.position + Vector2.right * m_facingDirection * 0.8f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint, attackRange, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null)
                enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy != null)
                enemy.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        m_animator.SetTrigger("Hurt");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerHurt();

        if (currentHealth <= 0f)
            Die();

        WriteQuickSaveSnapshot();
    }

    void Die()
    {
        isDead = true;
        m_animator.SetBool("noBlood", m_noBlood);
        m_animator.SetTrigger("Death");
        m_body2d.velocity = Vector2.zero;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerDeath();

        DeathReplay.RestartCurrentScene();

        enabled = false;
    }

    public float GetHealthPercent() =>
        Mathf.Approximately(maxHealth, 0f) ? 0f : currentHealth / maxHealth;

    public float GetHealthCurrent() => currentHealth;

    public float GetHealthMax() => maxHealth;

    void OnDrawGizmosSelected()
    {
        Vector2 attackPoint = (Vector2)transform.position + Vector2.right * m_facingDirection * 0.8f;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(attackPoint, attackRange);
    }

    void AE_SlideDust()
    {
        Vector3 spawnPosition = m_facingDirection == 1
            ? m_wallSensorR2.transform.position
            : m_wallSensorL2.transform.position;

        Quaternion rot = transform.localRotation;

        ObjectPool pool = ObjectPool.Instance;

        if (pool != null)
        {
            GameObject dust = pool.SpawnFromPool(SlidePoolTag, spawnPosition, rot);

            if (dust != null)
            {
                dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
                return;
            }
        }

        if (m_slideDust != null)
        {
            GameObject spawned = Instantiate(m_slideDust, spawnPosition, rot);
            spawned.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
