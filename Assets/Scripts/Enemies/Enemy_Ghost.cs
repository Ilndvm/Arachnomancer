using System.Collections;
using UnityEngine;

namespace MiniGame.UsukiFrenzy
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy_Ghost : EnemyBase
    {
        [SerializeField] private float toggleInterval = 0.5f;

        [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float invisibleAlpha = 0.25f;

        [SerializeField] private bool smoothFade = true;
        [SerializeField] private float fadeDuration = 0.15f;

        Coroutine toggleCoroutine;

        protected override void OnEnable()
        {
            base.OnEnable();

            SetAlpha(visibleAlpha);
            SetInvulnerable(true);

            // start toggling
            if (toggleCoroutine != null) StopCoroutine(toggleCoroutine);
            toggleCoroutine = StartCoroutine(ToggleRoutine());
        }

        private void OnDisable()
        {
            // stop coroutine for pooling safety
            if (toggleCoroutine != null)
            {
                StopCoroutine(toggleCoroutine);
                toggleCoroutine = null;
            }

            SetInvulnerable(false);
            SetAlpha(visibleAlpha);
        }

        private IEnumerator ToggleRoutine()
        {
            bool currentlyVisible = true;

            while (true)
            {
                float target = currentlyVisible ? invisibleAlpha : visibleAlpha;

                if (smoothFade && fadeDuration > 0f)
                {
                    yield return StartCoroutine(FadeAlpha(spriteRenderer.color.a, target, fadeDuration));
                }
                else
                {
                    SetAlpha(target);
                }

                SetInvulnerable(currentlyVisible);

                // wait the configured interval, then flip state
                yield return new WaitForSeconds(Mathf.Max(0f, toggleInterval));
                currentlyVisible = !currentlyVisible;
            }
        }

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            if (spriteRenderer == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(from, to, t);
                SetAlpha(a);
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float a)
        {
            if (spriteRenderer == null) return;
            Color c = spriteRenderer.color;
            if (Mathf.Approximately(c.a, a)) return;
            c.a = Mathf.Clamp01(a);
            spriteRenderer.color = c;
        }
    }
}