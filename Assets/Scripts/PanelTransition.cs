using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class PanelTransition : MonoBehaviour
{
    [SerializeField] private float Duration = 0.25f;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Coroutine activeTransition;
    private float canvasWidth;

    private void EnsureInit()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    private float GetCanvasWidth()
    {
        EnsureInit();
        var canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null) return canvasRect.rect.width;
        }
        return Screen.width;
    }

    public void ShowInstant()
    {
        gameObject.SetActive(true);
        EnsureInit();
        rect.anchoredPosition = Vector2.zero;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HideInstant()
    {
        gameObject.SetActive(true);
        EnsureInit();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
            activeTransition = null;
        }
        gameObject.SetActive(false);
    }

    public void SlideIn(Direction from)
    {
        gameObject.SetActive(true);
        EnsureInit();
        if (activeTransition != null) StopCoroutine(activeTransition);

        canvasWidth = GetCanvasWidth();

        float startX = from == Direction.Right ? canvasWidth : -canvasWidth;
        rect.anchoredPosition = new Vector2(startX, rect.anchoredPosition.y);
        canvasGroup.alpha = 0f;

        activeTransition = StartCoroutine(RunSlideIn());
    }

    public void SlideOut(Direction to, bool deactivate = true)
    {
        if (!gameObject.activeInHierarchy)
        {
            EnsureInit();
            canvasGroup.alpha = 0f;
            return;
        }
        EnsureInit();
        if (activeTransition != null) StopCoroutine(activeTransition);

        canvasWidth = GetCanvasWidth();
        activeTransition = StartCoroutine(RunSlideOut(to, deactivate));
    }

    public void FadeIn()
    {
        gameObject.SetActive(true);
        EnsureInit();
        if (activeTransition != null) StopCoroutine(activeTransition);

        canvasGroup.alpha = 0f;
        rect.anchoredPosition = Vector2.zero;

        activeTransition = StartCoroutine(RunFadeIn());
    }

    public void FadeOut(bool deactivate = true)
    {
        if (!gameObject.activeInHierarchy)
        {
            EnsureInit();
            canvasGroup.alpha = 0f;
            return;
        }
        EnsureInit();
        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(RunFadeOut(deactivate));
    }

    private IEnumerator RunSlideIn()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float startX = rect.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / Duration));

            rect.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0f, t), rect.anchoredPosition.y);
            canvasGroup.alpha = t;

            yield return null;
        }

        rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        activeTransition = null;
    }

    private IEnumerator RunSlideOut(Direction to, bool deactivate)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float targetX = to == Direction.Right ? canvasWidth : -canvasWidth;
        float startX = rect.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / Duration));

            rect.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), rect.anchoredPosition.y);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (deactivate) gameObject.SetActive(false);
        activeTransition = null;
    }

    private IEnumerator RunFadeIn()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / Duration));
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        activeTransition = null;
    }

    private IEnumerator RunFadeOut(bool deactivate)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / Duration));
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (deactivate) gameObject.SetActive(false);
        activeTransition = null;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public enum Direction
    {
        Left,
        Right
    }
}
