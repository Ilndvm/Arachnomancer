using UnityEngine;

public class Enemy_Skeleton : EnemyBase
{
    protected override void FixedUpdate()
    {
        if (player == null) return;

        MoveToRange();
    }
    protected override void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        // spawn projectile
        var p = GameManager.Instance.GetProjectile(1);
        p.MoveToPlayer(transform.position);
        animator.SetTrigger("Attack");
    }
}