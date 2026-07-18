using UnityEngine;
using TMPro;
using System.Collections;

public class RaceStartUIManager : MonoBehaviour
{
    [Header("Start Message Display")]
    [SerializeField] [Tooltip("Text display for start sequence messages")]
    private TextMeshProUGUI startMessageDisplay;

    [Space]
    [Header("Reaction Feedback Display")]
    [SerializeField] [Tooltip("Text display for reaction time feedback")]
    private TextMeshProUGUI reactionFeedbackDisplay;

    [Space]
    [Header("Display Settings")]
    [SerializeField] [Tooltip("Duration to show start messages (seconds)")]
    private float startMessageDuration = 0.8f;
    [SerializeField] [Tooltip("Duration to show reaction feedback (seconds)")]
    private float reactionFeedbackDuration = 2.0f;

    [Space]
    [Header("Colors")]
    [SerializeField] [Tooltip("Color for start messages")]
    private Color startMessageColor = Color.white;
    [SerializeField] [Tooltip("Color for Perfect Start")]
    private Color perfectStartColor = Color.green;
    [SerializeField] [Tooltip("Color for Great Start")]
    private Color greatStartColor = Color.cyan;
    [SerializeField] [Tooltip("Color for Good Start")]
    private Color goodStartColor = Color.yellow;
    [SerializeField] [Tooltip("Color for Slow Start")]
    private Color slowStartColor = new Color(1f, 0.5f, 0f);
    [SerializeField] [Tooltip("Color for Very Slow Start")]
    private Color verySlowStartColor = Color.red;

    private CanvasGroup _startMessageCanvasGroup;
    private CanvasGroup _reactionFeedbackCanvasGroup;

    private void Start()
    {
        InitializeCanvasGroups();
        SubscribeToEvents();

        DisplayStartMessage("On Your Marks");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeCanvasGroups()
    {
        if (startMessageDisplay != null)
        {
            _startMessageCanvasGroup = startMessageDisplay.GetComponent<CanvasGroup>();
            if (_startMessageCanvasGroup == null)
            {
                _startMessageCanvasGroup = startMessageDisplay.gameObject.AddComponent<CanvasGroup>();
            }
            _startMessageCanvasGroup.alpha = 1f;
        }

        if (reactionFeedbackDisplay != null)
        {
            _reactionFeedbackCanvasGroup = reactionFeedbackDisplay.GetComponent<CanvasGroup>();
            if (_reactionFeedbackCanvasGroup == null)
            {
                _reactionFeedbackCanvasGroup = reactionFeedbackDisplay.gameObject.AddComponent<CanvasGroup>();
            }
            _reactionFeedbackCanvasGroup.alpha = 0f;
        }
    }

    private void SubscribeToEvents()
    {
        RaceStartEvents.OnRaceStateChanged += HandleRaceStateChanged;
        RaceStartEvents.OnReactionQualityDetermined += HandleReactionQualityDetermined;
        RaceStartEvents.OnFalseStart += HandleFalseStart;
    }

    private void UnsubscribeFromEvents()
    {
        RaceStartEvents.OnRaceStateChanged -= HandleRaceStateChanged;
        RaceStartEvents.OnReactionQualityDetermined -= HandleReactionQualityDetermined;
        RaceStartEvents.OnFalseStart -= HandleFalseStart;
    }

    private void HandleRaceStateChanged(RaceStartState newState)
    {
        switch (newState)
        {
            case RaceStartState.GetSet:
                DisplayStartMessage("Get Set");
                break;
            case RaceStartState.Go:
                DisplayStartMessage("GO");
                break;
        }
    }

    private void HandleReactionQualityDetermined(ReactionQuality quality, float reactionTime)
    {
        DisplayReactionFeedback(quality, reactionTime);
    }

    private void HandleFalseStart()
    {
        DisplayStartMessage("False Start", Color.red);
    }

    private void DisplayStartMessage(string message, Color? color = null)
    {
        if (startMessageDisplay == null) return;

        startMessageDisplay.text = message;
        startMessageDisplay.color = color ?? startMessageColor;

        StopAllCoroutines();
        StartCoroutine(FadeMessageIn(startMessageDisplay, _startMessageCanvasGroup, startMessageDuration));
    }

    private void DisplayReactionFeedback(ReactionQuality quality, float reactionTime)
    {
        if (reactionFeedbackDisplay == null) return;

        string qualityText = quality.ToString().CamelCaseToWords();
        string feedbackText = $"{qualityText}\n{reactionTime:F3}s";

        reactionFeedbackDisplay.text = feedbackText;
        reactionFeedbackDisplay.color = GetReactionQualityColor(quality);

        StopCoroutine(FadeMessageIn(reactionFeedbackDisplay, _reactionFeedbackCanvasGroup, reactionFeedbackDuration));
        StartCoroutine(FadeMessageIn(reactionFeedbackDisplay, _reactionFeedbackCanvasGroup, reactionFeedbackDuration));
    }

    private Color GetReactionQualityColor(ReactionQuality quality)
    {
        return quality switch
        {
            ReactionQuality.PerfectStart => perfectStartColor,
            ReactionQuality.GreatStart => greatStartColor,
            ReactionQuality.GoodStart => goodStartColor,
            ReactionQuality.SlowStart => slowStartColor,
            ReactionQuality.VerySlowStart => verySlowStartColor,
            _ => Color.white
        };
    }

    private IEnumerator FadeMessageIn(TextMeshProUGUI textDisplay, CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration * 0.5f);

        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}

public static class StringExtensions
{
    public static string CamelCaseToWords(this string camelCase)
    {
        string result = "";
        for (int i = 0; i < camelCase.Length; i++)
        {
            if (char.IsUpper(camelCase[i]) && i > 0)
            {
                result += " ";
            }
            result += camelCase[i];
        }
        return result;
    }
}
