using UnityEngine;
using UnityEngine.InputSystem;

public class SpiderController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    [Header("Health")]
    [SerializeField] public float maxHP = 100f;
    [SerializeField] private float regenRate = 1f;      // HP per second while regenerating
    [SerializeField] private float regenDelay = 5f;     // seconds after last damage before regen starts
    public float currentHP;
    private float lastDamageTime = -999f;

    [Header("Auto-shoot")]
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float scanRange = 3f;      // how far we can detect enemies
    [SerializeField] private float fireRate = 1f;       // shots per second
    [SerializeField] private float scanInterval = 0.15f;
    [SerializeField] public LayerMask enemyLayer = ~0; // which layers to consider when scanning (default = everything)

    [Header("Shield")]
    [SerializeField] private float shieldCooldown = 10f;  // how long between activations
    // runtime timers
    private bool isShieldActive = false;
    private float shieldCooldownTimer = 0f;

    [SerializeField] private CircleCollider2D magnet;
    [SerializeField] private GameObject shield;
    private float initialMagnetRadius;
    // runtime
    private Transform currentTarget;
    private Transform secondTarget;
    private bool isShooting = false;

    // timers
    private float scanTimer = 0f;
    private float lastShotTime = -999f;
    private float fireCooldown => 1f / Mathf.Max(0.0001f, fireRate * UpgradeManager.Instance.GetFireRateMultiplier());

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHP = maxHP;
        GameManager.Instance.UIManager.UpdateHPSlider();
        initialMagnetRadius = magnet.radius;
    }

    void OnEnable()
    {
        // reset timers so shooting/scan don't fire immediately in weird states
        scanTimer = 0f;
        lastShotTime = Time.time - fireCooldown;
        isShooting = false;
    }

    void OnDisable()
    {
        isShooting = false;
        currentTarget = null;
    }

    void FixedUpdate()
    {
        // Move using physics-friendly velocity assignment
        rb.linearVelocity = moveInput * moveSpeed * UpgradeManager.Instance.GetSpeedMultiplier();
    }

    void Update()
    {
        // scanning handled on a timer
        scanTimer += Time.deltaTime;
        if (scanTimer >= Mathf.Max(0.01f, scanInterval))
        {
            scanTimer = 0f;
            ScanForTarget();
        }

        // shooting handled every frame but gated by time checks
        if (isShooting && currentTarget != null)
        {
            HandleShooting();
        }

        // passive regen
        HandleRegen();
        HandleShield();
    }

    #region Input / Movement
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;
        animator.SetBool("IsMoving", isMoving);

        if (context.canceled)
        {
            // store last input direction for idle facing
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }
    #endregion
    public void HandleShield()
    {
        // safety checks
        if (!UpgradeManager.Instance.HasShield)
        {
            if (isShieldActive)
                DeactivateShield();

            // keep cooldown reset so it won't activate if upgrade is off
            shieldCooldownTimer = shieldCooldown;
            return;
        }

        if (isShieldActive) return;

        // If shield is not active, count down cooldown and activate when it reaches zero.
        if (shieldCooldownTimer > 0f)
        {
            shieldCooldownTimer -= Time.deltaTime;
            if (shieldCooldownTimer <= 0f)
            {
                ActivateShield();
            }
        }
        else
        {
            ActivateShield();
        }
    }

    private void ActivateShield()
    {
        if (shield == null) return;
        shield.SetActive(true);
        isShieldActive = true;

        AudioManager.Instance.PlaySound(AudioManager.Sound.ShieldActivate);

        // TODO: play shield spawn VFX / SFX
    }

    public void DeactivateShield()
    {
        if (shield == null) return;
        shield.SetActive(false);
        isShieldActive = false;

        // Reset cooldown so next activation waits full cooldown
        shieldCooldownTimer = shieldCooldown;

        // TODO: play shield end VFX / SFX
    }
    #region Health / Regen
    public void TakeDamage(float damage)
    {
        if (damage <= 0f) return;

        if (isShieldActive)
        {
            DeactivateShield();
            return;
        }

        currentHP -= damage;
        lastDamageTime = Time.time;
        GameManager.Instance.UIManager.UpdateHPSlider();

        // TODO: play hurt VFX/SFX/animation
        // animator.SetTrigger("Hurt");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private void HandleRegen()
    {
        if (!UpgradeManager.Instance.HasRegeneration) return;

        if (currentHP >= maxHP) return;

        if (Time.time - lastDamageTime >= regenDelay)
        {
            // regenerate
            currentHP += regenRate * Time.deltaTime;
            if (currentHP > maxHP) currentHP = maxHP;

            GameManager.Instance.UIManager.UpdateHPSlider();
        }
    }
    public void Heal(float amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        GameManager.Instance.UIManager.UpdateHPSlider();
    }

    public void UpdateMaxHP()
    {
        maxHP = maxHP + UpgradeManager.Instance.GetBonusHP();
        GameManager.Instance.UIManager.UpdateHPSlider();
    }
    public void UpdateMagnet()
    {
        magnet.radius = initialMagnetRadius + UpgradeManager.Instance.GetMagnetBonus();
    }
    private void Die()
    {
        // TODO: play death animation / drop loot / notify manager
        // animator.SetTrigger("Die");

        // For now, deactivate the player object (or implement respawn)
        gameObject.SetActive(false);
    }
    #endregion

    #region Auto-Target & Shooting (no coroutines)
    private void ScanForTarget()
    {
        // find nearest enemy collider inside scanRange
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRange, enemyLayer);

        // track two nearest
        Transform best1 = null;
        Transform best2 = null;
        float best1Sqr = float.MaxValue;
        float best2Sqr = float.MaxValue;

        Vector2 me = transform.position;

        foreach (var c in hits)
        {
            if (c == null) continue;
            var enemyComp = c.GetComponent<EnemyBase>();
            if (enemyComp == null) continue;

            float sqr = ((Vector2)c.transform.position - me).sqrMagnitude;

            if (sqr < best1Sqr)
            {
                // shift best1 -> best2, set new best1
                best2Sqr = best1Sqr;
                best2 = best1;

                best1Sqr = sqr;
                best1 = c.transform;
            }
            else if (sqr < best2Sqr)
            {
                // this is the new second best
                best2Sqr = sqr;
                best2 = c.transform;
            }
        }

        // assign targets (may be null if fewer than required enemies)
        currentTarget = best1;
        secondTarget = best2;

        if (currentTarget != null)
        {
            isShooting = true;
            // optionally allow immediate shot:
            // lastShotTime = Time.time - fireCooldown;
        }
        else
        {
            isShooting = false;
        }

    }

    private void HandleShooting()
    {
        if (currentTarget == null)
        {
            isShooting = false;
            return;
        }

        // target may have been destroyed; double-check
        if (currentTarget.gameObject.activeInHierarchy == false)
        {
            currentTarget = null;
            isShooting = false;
            return;
        }

        // check range again (defensive)
        float dist = Vector2.Distance(transform.position, currentTarget.position);
        if (dist > scanRange)
        {
            currentTarget = null;
            isShooting = false;
            return;
        }

        // time to fire?
        if (Time.time - lastShotTime >= fireCooldown)
        {
            lastShotTime = Time.time;

            // animator.SetTrigger("Shoot");

            var p = GameManager.Instance.GetPlayerProjectile();
            p.Init(projectileDamage + UpgradeManager.Instance.GetDamageBonus());
            p.MoveToTarget(transform.position, currentTarget.position);

            if (secondTarget != null && UpgradeManager.Instance.HasTwoTargets)
            {
                p = GameManager.Instance.GetPlayerProjectile();
                p.Init(projectileDamage + UpgradeManager.Instance.GetDamageBonus());
                p.MoveToTarget(transform.position, secondTarget.position);
            }

        }
    }
    #endregion
}