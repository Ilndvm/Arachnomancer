using System.Collections;
using UnityEngine;

public class Blood : MonoBehaviour
{
    [SerializeField] private float magnetPullSpeed = 8f;    // units/sec while being pulled

    bool isBeingPulled = false;

    private void OnEnable()
    {
        // Reset runtime state for pooling
        isBeingPulled = false;
    }

    private void Update()
    {
        // If magnet pulled, move toward player's current position each frame
        if (isBeingPulled)
        {
            Vector2 cur = transform.position;
            Vector2 target = GameManager.Instance.player.transform.position;
            float step = magnetPullSpeed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(cur, target, step);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.blood++;

            GameManager.Instance.UIManager.UpdateBloodText();

            gameObject.SetActive(false);
            return;
        }

        if (collision.CompareTag("Magnet"))
        {
            // start being pulled toward the player (no coroutine, no collider disabling)
            isBeingPulled = true;
        }
    }
}