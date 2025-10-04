using UnityEngine;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(Animator))]
public class Enemy_SlimeKing : EnemyBase
{
    protected bool isMoving = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        isMoving = false;
    }

    public void StartMovingEvent()
    {
        isMoving = true;
    }

    public void StopMovingEvent()
    {
        isMoving = false;
    }

    protected override void MoveToPlayer()
    {
        Vector2 currentPos = rb.position;
        Vector2 targetPos = player.transform.position;
        Vector2 diff = targetPos - currentPos;
        float sqrDist = diff.sqrMagnitude;

        if (!isMoving)
        {
            // still update facing a little if the horizontal offset is large to keep sprite orientation sensible
            float dx = diff.x;
            if (Mathf.Abs(dx) > flipThreshold)
                SetFlip(dx < 0);

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
    protected override void Die()
    {
        base.Die();
        // spawn two smaller slimes
        EnemyBase enemy1 = GameManager.Instance.GetEnemy(5);
        enemy1.SetPosition(new Vector2(transform.position.x + 1f, transform.position.y));

        EnemyBase enemy2 = GameManager.Instance.GetEnemy(5);
        enemy2.SetPosition(new Vector2(transform.position.x - 1f, transform.position.y));

        EnemyBase enemy3 = GameManager.Instance.GetEnemy(5);
        enemy3.SetPosition(new Vector2(transform.position.x, transform.position.y + 1f));
    }
}