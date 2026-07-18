using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenEffectsController : MonoBehaviour
{
    [Header("Screen Flash Configuration")]
    [SerializeField] [Tooltip("Color for GO flash effect")]
    private Color goFlashColor = Color.green;
    
    [SerializeField] [Tooltip("Color for False Start flash effect")]
    private Color falseStartFlashColor = Color.red;
    
    [SerializeField] [Tooltip("Duration of the flash effect in seconds")]
    private float flashDuration = 0.5f;
    
    [SerializeField] [Tooltip("Curve for fade animation")]
    private AnimationCurve flashFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [SerializeField] private Image flashImage;
    [SerializeField] private CanvasGroup flashCanvasGroup;
    private Coroutine activeFlashCoroutine;
    
    
    public void PlayGoFlash()
    {
        PlayFlash(goFlashColor);
    }
    
    public void PlayFalseStartFlash()
    {
        PlayFlash(falseStartFlashColor);
    }
    
    public void PlayFlash(Color color)
    {
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
        }
        
        activeFlashCoroutine = StartCoroutine(FlashCoroutine(color));
    }
    
    private IEnumerator FlashCoroutine(Color color)
    {
        flashImage.color = color;
        flashCanvasGroup.alpha = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / flashDuration);
            float alpha = flashFadeCurve.Evaluate(normalizedTime);
            
            Color currentColor = color;
            currentColor.a = alpha;
            flashImage.color = currentColor;
            
            yield return null;
        }
        
        flashImage.color = Color.clear;
        activeFlashCoroutine = null;
    }
    
    public void Stop()
    {
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
            activeFlashCoroutine = null;
        }
        
        if (flashImage != null)
        {
            flashImage.color = Color.clear;
        }
    }
}
