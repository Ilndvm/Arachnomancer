using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float floatDistance = 0.6f; // world units up
    [SerializeField] private float duration = 0.9f;
    [SerializeField] private bool fadeOut = true;

    private TMP_Text tmpText;

    // runtime
    private Coroutine floatCoroutine;
    private Vector3 startPos;
    private Color startColor;
    private string cachedText = "";

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        startColor = tmpText.color;
    }

    private void OnEnable()
    {
        StartFloatingFromCurrentState();
    }

    private void OnDisable()
    {
        // stop coroutine to be pool-safe
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }

        // restore color just in case
        if (tmpText != null)
            tmpText.color = startColor;
    }

    public void Init(Vector2 position, string text)
    {
        startPos = new Vector3(position.x, position.y, transform.position.z);
        cachedText = text ?? "";

        // place at start position immediately (important when pooling)
        transform.position = startPos;

        if (tmpText != null)
        {
            tmpText.text = cachedText;
            tmpText.color = startColor;
        }

        // If object is already active, start floating immediately
        if (gameObject.activeInHierarchy)
            StartFloatingFromCurrentState();
    }

    private void StartFloatingFromCurrentState()
    {
        // ensure position is set (if Init wasn't called explicitly)
        if (startPos == Vector3.zero)
            startPos = transform.position;
        else
            transform.position = startPos;

        // reset color
        tmpText.color = startColor;

        // restart coroutine
        if (floatCoroutine != null)
            StopCoroutine(floatCoroutine);

        floatCoroutine = StartCoroutine(FloatRoutine());
    }

    private IEnumerator FloatRoutine()
    {
        Vector3 from = transform.position;
        Vector3 to = from + Vector3.up * floatDistance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // ease out motion for nicer feel
            float ease = 1f - Mathf.Pow(1f - t, 2f);
            transform.position = Vector3.Lerp(from, to, ease);

            if (fadeOut)
            {
                float a = 1f - t;
                Color c = tmpText.color;
                c.a = startColor.a * a;
                tmpText.color = c;
            }

            yield return null;
        }

        // final state
        transform.position = to;
        if (fadeOut)
        {
            Color c = startColor;
            c.a = 0f;
            tmpText.color = c;
        }

        floatCoroutine = null;

        // deactivate for pooling
        gameObject.SetActive(false);
    }
}