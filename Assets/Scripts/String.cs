using UnityEngine;

public class String : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.strings++;
            gameObject.SetActive(false);
        }
    }
}