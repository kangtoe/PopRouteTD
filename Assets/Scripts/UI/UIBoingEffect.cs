using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIBoingEffect : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float amplitude = 0.12f;
    [SerializeField] private float frequency = 2.5f;

    private Vector3 originalScale;
    private Coroutine boingCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (boingCoroutine != null)
            StopCoroutine(boingCoroutine);
        transform.localScale = originalScale;
        boingCoroutine = StartCoroutine(BoingRoutine());
    }

    private IEnumerator BoingRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float decay = 1f - t;
            float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f);
            float s = amplitude * decay * wave;
            transform.localScale = new Vector3(
                originalScale.x * (1f - s * 0.5f),
                originalScale.y * (1f + s),
                originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
        boingCoroutine = null;
    }
}
