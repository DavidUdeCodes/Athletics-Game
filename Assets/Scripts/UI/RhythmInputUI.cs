using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RhythmInputUI : MonoBehaviour, ISprintInputModeUI
{
    [Header("UI References")]
    [SerializeField] [Tooltip("The rhythm slider background container")]
    private RectTransform rhythmSlider;
    [SerializeField] [Tooltip("The animated marker showing current position")]
    private RectTransform marker;
    [SerializeField] [Tooltip("Perfect zone indicator on left side")]
    private RectTransform perfectZoneLeft;
    [SerializeField] [Tooltip("Perfect zone indicator on right side")]
    private RectTransform perfectZoneRight;
    [SerializeField] [Tooltip("Display for current speed")]
    private TMP_Text speedDisplay;
    [SerializeField] [Tooltip("Display for tap quality feedback")]
    private TMP_Text qualityDisplay;
    [SerializeField] [Tooltip("Background that changes color on tap feedback")]
    private Image qualityBackground;

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

    private RhythmInputMode _rhythmController;
    private SprintController _sprintController;
    private MomentumController _momentumController;
    private Color _defaultBackgroundColor;
    private CanvasGroup _qualityCanvasGroup;
    private CanvasGroup _canvasGroup;

    private void Awake()
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

    public void SetControllers(RhythmInputMode rhythmController, SprintController sprintController, MomentumController momentumController)
    {
        _rhythmController = rhythmController;
        _sprintController = sprintController;
        _momentumController = momentumController;
        UpdatePerfectZonePositions();
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

    private void UpdatePerfectZonePositions()
    {
        if (_rhythmController == null || rhythmSlider == null) return;

        float sliderWidth = rhythmSlider.rect.width;

        if (perfectZoneLeft != null)
        {
            float leftCenter = (_rhythmController.LeftZoneMin + _rhythmController.LeftZoneMax) * 0.5f;
            float leftSize = (_rhythmController.PerfectZoneSize) * sliderWidth;
            perfectZoneLeft.anchoredPosition = new Vector2((leftCenter - 0.5f) * sliderWidth, perfectZoneLeft.anchoredPosition.y);
            perfectZoneLeft.sizeDelta = new Vector2(leftSize, perfectZoneLeft.sizeDelta.y);
        }

        if (perfectZoneRight != null)
        {
            float rightCenter = (_rhythmController.RightZoneMin + _rhythmController.RightZoneMax) * 0.5f;
            float rightSize = (_rhythmController.PerfectZoneSize) * sliderWidth;
            perfectZoneRight.anchoredPosition = new Vector2((rightCenter - 0.5f) * sliderWidth, perfectZoneRight.anchoredPosition.y);
            perfectZoneRight.sizeDelta = new Vector2(rightSize, perfectZoneRight.sizeDelta.y);
        }
    }

    private void Update()
    {
        if (_rhythmController != null && marker != null)
        {
            float sliderWidth = rhythmSlider.rect.width;
            float markerXPos = (_rhythmController.MarkerPosition - 0.5f) * sliderWidth;
            marker.anchoredPosition = new Vector2(markerXPos, marker.anchoredPosition.y);
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
