using UnityEngine;

public class Enemy_FlyingSkull : EnemyBase
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
        var p = GameManager.Instance.GetProjectile(2);
        p.FollowPlayer(transform.position);
    }
}