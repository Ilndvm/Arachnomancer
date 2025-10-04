using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class Mine : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 5f;

    private Coroutine lifeCoroutine;

    private void OnEnable()
    {
        // Start self-destruct timer
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        lifeCoroutine = StartCoroutine(LifeTimer());

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
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, lifetime));
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to find the player component
        SpiderController player = other.GetComponent<SpiderController>();
        if (player != null)
        {
            // Damage the player
            player.TakeDamage(damage);

            // TODO: spawn explosion VFX / SFX here

            // Die (deactivate) immediately after applying damage
            gameObject.SetActive(false);
        }
    }
}