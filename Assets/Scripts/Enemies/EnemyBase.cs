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


    [SerializeField] protected float invulnAfterHitSeconds = 0.2f;

    protected float flipThreshold = 0.1f;

    protected int currentHealth;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected SpiderController player;

    [HideInInspector] public float lastAttackTime = 0f;
    bool isInvulnerable = false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    protected virtual void Start()
    {
        player = FindFirstObjectByType<SpiderController>();
    }

    protected virtual void Init()
    {
        currentHealth = maxHealth;
    }

    protected virtual void OnEnable()
    {
        Init();
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

    protected void SetFlip(bool flip)
    {
        if (spriteRenderer != null && spriteRenderer.flipX != flip)
            spriteRenderer.flipX = flip;
    }

    protected virtual void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isInvulnerable) return;

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

    protected virtual void Die()
    {
        gameObject.SetActive(false);
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