using UnityEngine;

public class Enemy_Goblin : EnemyBase
{
    [SerializeField] private GameObject mine;
    protected override void MoveToPlayer()
    {
        Vector2 currentPos = rb.position;
        Vector2 targetPos = player.transform.position;
        Vector2 diff = targetPos - currentPos;

        Attack();

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

    protected override void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        // spawn mine
        var p = GameManager.Instance.GetProjectile(0);
        p.Init(damage);
    }
}