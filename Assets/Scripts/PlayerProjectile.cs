using System.Collections;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float speed = 6f;
    [SerializeField, Range(0f, 360f)] private float ricochetAngleSpread = 180f; // spread for random direction

    private bool ricocheted = false;

    private Coroutine lifeCoroutine;

    // Movement runtime
    private Rigidbody2D rb;
    private Coroutine moveCoroutine;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void Init(int damage)
    {
        this.damage = damage;
        ricocheted = false;

    }

    private void OnEnable()
    {
        // Start self-destruct timer
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        lifeCoroutine = StartCoroutine(LifeTimer());

        // Ensure any previous movement is stopped when enabled
        StopMovement();

        // TODO: play spawn VFX / SFX here
    }

    private void OnDisable()
    {
        // Stop running coroutines when disabled / returned to pool
        if (lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
            lifeCoroutine = null;
        }

        // Stop movement coroutine as well
        StopMovement();
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, lifetime));
        gameObject.SetActive(false);
    }

    public void MoveToTarget(Vector2 spawnPos, Vector2 target)
    {
        //StopMovement();

        // place projectile at desired spawn location (important when pooling)
        rb.position = spawnPos;

        moveCoroutine = StartCoroutine(MoveStraightToRoutine(target));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to find the player component
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // Damage the player
            enemy.TakeDamage(damage);

            if (UpgradeManager.Instance.HasLifeSteal)
            {
                GameManager.Instance.player.Heal(damage * UpgradeManager.Instance.lifeStealPercent);
            }
            if (UpgradeManager.Instance.HasSlowness)
            {
                enemy.ApplySlowness(UpgradeManager.Instance.slowness, UpgradeManager.Instance.slownessTime);
            }
            if (UpgradeManager.Instance.HasPoison)
            {
                enemy.ApplyPoison(UpgradeManager.Instance.poison, UpgradeManager.Instance.poisonTime);
            }
            if (UpgradeManager.Instance.HasRicochet && !ricocheted)
            {
                // mark ricochet used
                ricocheted = true;

                // choose a random direction within the spread
                float halfSpread = ricochetAngleSpread * 0.5f;
                float angleDeg = Random.Range(-halfSpread, halfSpread);
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // random unit vector in 2D (rotate by random angle)
                Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;

                // compute a distant target along that direction
                Vector2 newTarget = (rb != null ? rb.position : (Vector2)transform.position) + dir * 5;

                // restart movement toward newTarget
                StopMovement();
                moveCoroutine = StartCoroutine(MoveStraightToRoutine(newTarget));

                // do not deactivate — allow the ricochet to continue and try to hit a second enemy
                return;
            }
            if (UpgradeManager.Instance.HasExplosion)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, UpgradeManager.Instance.explosionRadius, GameManager.Instance.player.enemyLayer);

                foreach (var c in hits)
                {
                    c.TryGetComponent<EnemyBase>(out var e);

                    // Apply damage to each enemy
                    // TODO: spawn explosion VFX / SFX here

                    e.TakeDamage(damage);
                }
            }

            // Die (deactivate) immediately after applying damage
            gameObject.SetActive(false);
        }
    }

    private void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    private IEnumerator MoveStraightToRoutine(Vector2 targetPos)
    {
        // Move until we reach the cached target position or the projectile is disabled.
        while (true)
        {
            Vector2 cur = rb != null ? rb.position : (Vector2)transform.position;

            Vector2 next = Vector2.MoveTowards(cur, targetPos, speed * Time.fixedDeltaTime);

            if (rb != null)
            {
                rb.MovePosition(next);
                yield return new WaitForFixedUpdate();
            }
            else
            {
                transform.position = next;
                yield return null;
            }
        }
    }
}
