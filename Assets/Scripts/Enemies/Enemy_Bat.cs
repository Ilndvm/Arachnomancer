using System.Collections; 
using UnityEngine;

namespace MiniGame.UsukiFrenzy
{
    public class Enemy_Bat : EnemyBase
    {
        [SerializeField] private float lungeOutTime = 0.18f;
        [SerializeField] private float pauseAtTarget = 0.02f;

        // runtime
        private bool isLunging = false;
        private float lastLungeTime = -999f;
        private Vector2 targetPos;


        protected override void FixedUpdate()
        {
            if (player == null) return;

            MoveToRange(); 
        }

        protected override void Attack()
        {
            base.Attack();

            if (isLunging) return;

            if (Time.time - lastLungeTime < attackCooldown) return;

            lastLungeTime = Time.time;

            targetPos = player.transform.position;

            animator.SetTrigger("Attack");
            StartCoroutine(LungeRoutine());
        }

        private IEnumerator LungeRoutine()
        {
            if (rb == null || player == null)
                yield break;

            isLunging = true;

            // cache positions at the start of the attack
            Vector2 startPos = rb.position;

            // cache player position at the moment of attack
            Vector2 cachedPlayerPos = player.transform.position;

            // --- Move OUT to target ---
            float elapsed = 0f;
            while (elapsed < lungeOutTime)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, lungeOutTime));
                // eased interpolation (smooth out)
                float ease = EaseOutQuad(t);
                Vector2 next = Vector2.LerpUnclamped(startPos, targetPos, ease);
                rb.MovePosition(next);
                yield return new WaitForFixedUpdate();
            }

            // ensure exact target at end
            rb.MovePosition(targetPos);

            // small optional pause so the lunge is visible
            if (pauseAtTarget > 0f)
                yield return new WaitForSeconds(pauseAtTarget);

            // --- Move BACK to original start position ---
            elapsed = 0f;
            while (elapsed < lungeOutTime)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, lungeOutTime));
                float ease = EaseInQuad(t);
                Vector2 next = Vector2.LerpUnclamped(targetPos, startPos, ease);
                rb.MovePosition(next);
                yield return new WaitForFixedUpdate();
            }

            // ensure original position is restored
            rb.MovePosition(startPos);

            isLunging = false;
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInQuad(float t) => t * t;
    }
}
