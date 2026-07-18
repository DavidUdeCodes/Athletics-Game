using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFadeController : MonoBehaviour
{
    [Header("Fade Configuration")]
    [SerializeField] [Tooltip("Duration of fade out animation in seconds")]
    private float fadeOutDuration = 0.5f;
    
    [SerializeField] [Tooltip("Duration of fade in animation in seconds")]
    private float fadeInDuration = 0.5f;
    
    [SerializeField] [Tooltip("Curve for fade animation")]
    private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [SerializeField] [Tooltip("Image for fade overlay")]
    private Image fadeImage;
    
    [SerializeField] [Tooltip("CanvasGroup controlling fade visibility")]
    private CanvasGroup fadeCanvasGroup;
    
    private Coroutine _activeFadeCoroutine;

    private void OnEnable()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning($"{gameObject.name}: Fade Image not assigned to ScreenFadeController");
        }

        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning($"{gameObject.name}: Fade CanvasGroup not assigned to ScreenFadeController");
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }
    }

    public Coroutine FadeOut()
    {
        if (_activeFadeCoroutine != null)
        {
            StopCoroutine(_activeFadeCoroutine);
        }

        _activeFadeCoroutine = StartCoroutine(FadeOutCoroutine());
        return _activeFadeCoroutine;
    }

    public Coroutine FadeIn()
    {
        if (_activeFadeCoroutine != null)
        {
            StopCoroutine(_activeFadeCoroutine);
        }

        _activeFadeCoroutine = StartCoroutine(FadeInCoroutine());
        return _activeFadeCoroutine;
    }

    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            float alpha = fadeCurve.Evaluate(normalizedTime);

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = alpha;
            }

            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }

        _activeFadeCoroutine = null;
    }

    private IEnumerator FadeInCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeInDuration);
            float alpha = 1f - fadeCurve.Evaluate(normalizedTime);

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = alpha;
            }

            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }

        _activeFadeCoroutine = null;
    }
}
