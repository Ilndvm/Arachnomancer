using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected int maxHealth = 20;
    [SerializeField] protected int contactDamage = 1;
    [SerializeField] protected int damage = 5;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float attackRange = 1f; // stop moving when inside this range
    [SerializeField] protected float attackCooldown = 1.0f;
    [Range(0f, 100f)]
    [SerializeField] protected float dropChance = 50.0f;

    [SerializeField] protected float invulnAfterHitSeconds = 0.2f;

    protected float flipThreshold = 0.1f;

    protected int currentHealth;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected SpiderController player;

    [HideInInspector] public float lastAttackTime = 0f;
    bool isInvulnerable = false;

    protected float baseMoveSpeed;
    Coroutine slowCoroutine = null;
    Coroutine poisonCoroutine = null;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // store the unmodified speed so we can restore after slows
        baseMoveSpeed = moveSpeed;
    }

    protected virtual void Start()
    {
        player = GameManager.Instance.player;
    }

    protected virtual void Init()
    {
        currentHealth = maxHealth;
        moveSpeed = baseMoveSpeed;

        // ensure runtime effect state is clean if Init is called directly
        ResetStatusEffects();
    }

    protected virtual void OnEnable()
    {
        Init();
    }

    protected void ResetStatusEffects()
    {
        // stop slow
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            slowCoroutine = null;
        }

        // stop poison
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            poisonCoroutine = null;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (player == null) return;

        MoveToPlayer();
    }

    protected virtual void MoveToRange()
    {
        Vector2 currentPos = rb.position;
        Vector2 targetPos = player.transform.position;
        Vector2 diff = targetPos - currentPos;
        float sqrDist = diff.sqrMagnitude;

        // If inside attackRange, don't try to chase — attack instead.
        if (sqrDist <= attackRange * attackRange)
        {
            // Face player if horizontal offset is significant
            float dx = diff.x;
            if (Mathf.Abs(dx) > flipThreshold)
                SetFlip(dx < 0);

            // attack logic
            Attack();
            return;
        }

        // Not in attack range: move toward player
        Vector2 dir = diff.normalized;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // Flip sprite only when horizontal component is significant to avoid jitter
        if (Mathf.Abs(dir.x) > flipThreshold)
        {
            SetFlip(dir.x < 0);
        }
    }

    protected virtual void MoveToPlayer()
    {
        Vector2 currentPos = rb.position;
        Vector2 targetPos = player.transform.position;
        Vector2 diff = targetPos - currentPos;

        // move toward player
        Vector2 dir = diff.normalized;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // Flip sprite only when horizontal component is significant to avoid jitter
        if (Mathf.Abs(dir.x) > flipThreshold)
        {
            SetFlip(dir.x < 0);
        }
    }

    public virtual void SetPosition(Vector2 pos)
    {
        transform.position = pos;
    }

    protected void SetFlip(bool flip)
    {
        if (spriteRenderer != null && spriteRenderer.flipX != flip)
            spriteRenderer.flipX = flip;
    }
    protected void SetInvulnerable(bool v)
    {
        isInvulnerable = v;
    }

    protected virtual void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isInvulnerable) return;

        FloatingText t = GameManager.Instance.GetFloatingText();
        if (t != null) t.Init(transform.position, "-" + amount);

        currentHealth -= amount;
        StartCoroutine(TemporaryInvuln(invulnAfterHitSeconds));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator TemporaryInvuln(float seconds)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(seconds);
        isInvulnerable = false;
    }

    public void ApplySlowness(float factor, float duration)
    {
        factor = Mathf.Clamp(factor, 0.01f, 1f);

        // cancel existing slow if running
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            slowCoroutine = null;
        }

        slowCoroutine = StartCoroutine(SlowRoutine(factor, duration));
    }

    private IEnumerator SlowRoutine(float factor, float duration)
    {
        // set slowed speed based on baseMoveSpeed so stacking/overrides don't compound
        moveSpeed = baseMoveSpeed * factor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // restore base speed
        moveSpeed = baseMoveSpeed;
        slowCoroutine = null;
    }

    public void ApplyPoison(int damagePerSecond, float duration)
    {
        // cancel existing poison if running
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            poisonCoroutine = null;
        }

        poisonCoroutine = StartCoroutine(PoisonRoutine(damagePerSecond, duration));
    }

    private IEnumerator PoisonRoutine(int damagePerSecond, float duration)
    {
        float elapsed = 0f;

        // immediate tick every 1s (not fractional). If duration < 1s there will be at most one tick if duration >=1.
        while (elapsed < duration)
        {
            // wait one second (or remaining time if less than 1s)
            float wait = Mathf.Min(1f, duration - elapsed);
            yield return new WaitForSeconds(wait);

            // apply damage (ignore invulnerability)
            ApplyDamageDirect(damagePerSecond);

            elapsed += wait;

            // if died, break
            if (currentHealth <= 0) break;
        }

        poisonCoroutine = null;
    }

    // Applies damage bypassing temporary invulnerability and still shows floating text.
    protected void ApplyDamageDirect(int amount)
    {
        if (amount <= 0) return;

        FloatingText t = GameManager.Instance.GetFloatingText();
        if (t != null) t.Init(transform.position, "-" + amount);

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        gameObject.SetActive(false);

        float random = Random.Range(1f, 100f);
        if (random <= dropChance + UpgradeManager.Instance.GetLuckBonus())
        {
            var s = GameManager.Instance.GetString();
            s.transform.position = transform.position;
        }

        var d = GameManager.Instance.GetDeathParticle();
        d.transform.position = transform.position;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        SpiderController player = other.GetComponent<SpiderController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
        }
    }
}