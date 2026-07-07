using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ForceControlInputUI : MonoBehaviour, ISprintInputModeUI
{
    [Header("UI References")]
    [SerializeField] [Tooltip("The meter background container")]
    private RectTransform meterBackground;
    [SerializeField] [Tooltip("The animated marker showing current height")]
    private RectTransform marker;
    [SerializeField] [Tooltip("The target zone indicator")]
    private RectTransform targetZone;
    [SerializeField] [Tooltip("Display for current speed")]
    private TMP_Text speedDisplay;
    [SerializeField] [Tooltip("Background that changes color on tap feedback")]
    private Image qualityBackground;
    [SerializeField] [Tooltip("Display for tap quality feedback")]
    private TMP_Text qualityDisplay;

    [Space]
    [Header("Momentum Display")]
    [SerializeField] [Tooltip("Fill bar showing current momentum")]
    private Image momentumBar;
    [SerializeField] [Tooltip("Text display for momentum value")]
    private TMP_Text momentumDisplay;

    [Space]
    [Header("Color Feedback")]
    [SerializeField] [Tooltip("Color for Perfect tap")]
    private Color perfectColor = Color.green;
    [SerializeField] [Tooltip("Color for Good tap")]
    private Color goodColor = Color.blue;
    [SerializeField] [Tooltip("Color for Fair tap")]
    private Color fairColor = Color.yellow;
    [SerializeField] [Tooltip("Color for Miss")]
    private Color missColor = Color.red;

    [Space]
    [Header("Feedback Duration")]
    [SerializeField] [Tooltip("How long feedback animation lasts in seconds")]
    private float feedbackDuration = 0.5f;

    private ForceControlInputMode _forceControlInputMode;
    private SprintController _sprintController;
    private MomentumController _momentumController;
    private Color _defaultBackgroundColor;
    private CanvasGroup _qualityCanvasGroup;
    private CanvasGroup _canvasGroup;

    private void Start()
    {
        if (qualityBackground != null)
            _defaultBackgroundColor = qualityBackground.color;

        _qualityCanvasGroup = qualityDisplay?.GetComponent<CanvasGroup>();
        if (_qualityCanvasGroup == null && qualityDisplay != null)
            _qualityCanvasGroup = qualityDisplay.gameObject.AddComponent<CanvasGroup>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetControllers(ForceControlInputMode forceControlInputMode, SprintController sprintController, MomentumController momentumController)
    {
        _forceControlInputMode = forceControlInputMode;
        _sprintController = sprintController;
        _momentumController = momentumController;
    }

    public void Show()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    public void Hide()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowQualityFeedback(TapQuality quality)
    {
        ShowTimingFeedback(quality);
    }

    public void UpdateMomentum(float momentum)
    {
        if (momentumBar != null)
        {
            momentumBar.fillAmount = momentum;
        }

        if (momentumDisplay != null)
        {
            momentumDisplay.text = $"Momentum: {momentum:F2}";
        }
    }

    public void UpdateSpeed(float currentSpeed, float maxSpeed)
    {
        if (speedDisplay != null)
        {
            speedDisplay.text = $"Speed: {currentSpeed:F1} / {maxSpeed:F1}";
        }
    }

    private void Update()
    {
        if (_forceControlInputMode == null || meterBackground == null) return;

        float meterHeight = meterBackground.rect.height;

        if (marker != null)
        {
            float markerYPos = (_forceControlInputMode.MarkerPosition - 0.5f) * meterHeight;
            marker.anchoredPosition = new Vector2(marker.anchoredPosition.x, markerYPos);
        }

        if (targetZone != null)
        {
            float targetZoneHeight = _forceControlInputMode.TargetZoneSize * meterHeight;
            float targetZoneCenterY = (_forceControlInputMode.TargetZoneCenterPosition - 0.5f) * meterHeight;
            targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, targetZoneCenterY);
            targetZone.sizeDelta = new Vector2(targetZone.sizeDelta.x, targetZoneHeight);
        }

        if (_sprintController != null && speedDisplay != null)
        {
            speedDisplay.text = $"Speed: {_sprintController.CurrentSpeed:F1} / {_sprintController.MaxSpeed:F1}";
        }

        if (_momentumController != null && momentumBar != null)
        {
            momentumBar.fillAmount = _momentumController.CurrentMomentum;
        }

        if (_momentumController != null && momentumDisplay != null)
        {
            momentumDisplay.text = $"Momentum: {_momentumController.CurrentMomentum:F2}";
        }
    }

    public void ShowTimingFeedback(TapQuality quality)
    {
        if (qualityBackground == null) return;

        Color feedbackColor = quality switch
        {
            TapQuality.Perfect => perfectColor,
            TapQuality.Good => goodColor,
            TapQuality.Fair => fairColor,
            TapQuality.Miss => missColor,
            _ => missColor
        };

        if (qualityDisplay != null)
        {
            qualityDisplay.text = quality.ToString();
        }

        StopAllCoroutines();
        StartCoroutine(AnimateFeedback(feedbackColor));
    }

    private IEnumerator AnimateFeedback(Color targetColor)
    {
        float elapsed = 0f;
        Color startColor = qualityBackground.color;

        while (elapsed < feedbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / feedbackDuration;
            qualityBackground.color = Color.Lerp(targetColor, startColor, t);

            if (_qualityCanvasGroup != null)
                _qualityCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        qualityBackground.color = startColor;
        if (_qualityCanvasGroup != null)
            _qualityCanvasGroup.alpha = 0f;
    }
}
