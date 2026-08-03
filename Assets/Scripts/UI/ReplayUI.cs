using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Video-player-style replay controls.
/// Subscribes to <see cref="ReplayManager"/> events so it never polls and stays
/// decoupled from playback internals.
///
/// Shows automatically when replay starts, hides when replay ends or is stopped.
/// All UI updates are driven by events; no per-frame polling is needed outside Update
/// for the timeline slider sync during active playback.
///
/// Wire all serialised fields in the Inspector. ReplayManager is located via
/// <see cref="ReplayManager.Instance"/> if not assigned.
/// </summary>
public class ReplayUI : MonoBehaviour
{
    // ── Inspector references ──────────────────────────────────────────────────

    [Header("Transport Buttons")]
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button skipToStartButton;
    [SerializeField] private Button skipToEndButton;
    [SerializeField] private Button closeButton;

    [Header("Play / Pause Icons")]
    [SerializeField] private Image playPauseIcon;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;

    [Header("Speed Control")]
    [SerializeField] private Button speedButton;
    [Tooltip("Speeds the button cycles through, in order. Wraps back to the start after the last entry.")]
    [SerializeField] private float[] speedOptions = { 0.25f, 0.5f, 1f, 1.5f, 2f };
    [Tooltip("Index into speedOptions that playback starts at (1 = 1x with the default array above).")]
    [SerializeField] private int defaultSpeedIndex = 2;

    [Header("Timeline")]
    [SerializeField] private Slider timelineSlider;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI currentTimeLabel;
    [SerializeField] private TextMeshProUGUI totalTimeLabel;
    [SerializeField] private TextMeshProUGUI speedLabel;

    [Header("References")]
    [SerializeField] private ReplayManager replayManager;

    // ── Internal state ────────────────────────────────────────────────────────

    private CanvasGroup _canvasGroup;
    private bool _suppressSliderCallback;
    private int _speedIndex;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (replayManager == null)
            replayManager = ReplayManager.Instance;

        if (replayManager == null)
        {
            Debug.LogError("[ReplayUI] ReplayManager not found. Assign it in the Inspector or ensure the singleton is in the scene.");
            return;
        }

        _speedIndex = ClampSpeedIndex(defaultSpeedIndex);

        SetupButtonListeners();
        SubscribeToReplayEvents();
        Hide();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
        UnsubscribeFromReplayEvents();
    }

    // ── Event subscriptions ───────────────────────────────────────────────────

    private void SubscribeToReplayEvents()
    {
        replayManager.OnReplayStarted  += HandleReplayStarted;
        replayManager.OnReplayPaused   += HandleReplayPaused;
        replayManager.OnReplayResumed  += HandleReplayResumed;
        replayManager.OnReplayStopped  += HandleReplayStopped;
        replayManager.OnReplayFinished += HandleReplayFinished;
        replayManager.OnReplayTimeChanged += HandleReplayTimeChanged;
    }

    private void UnsubscribeFromReplayEvents()
    {
        if (replayManager == null) return;
        replayManager.OnReplayStarted  -= HandleReplayStarted;
        replayManager.OnReplayPaused   -= HandleReplayPaused;
        replayManager.OnReplayResumed  -= HandleReplayResumed;
        replayManager.OnReplayStopped  -= HandleReplayStopped;
        replayManager.OnReplayFinished -= HandleReplayFinished;
        replayManager.OnReplayTimeChanged -= HandleReplayTimeChanged;
    }

    // ── Button wiring ─────────────────────────────────────────────────────────

    private void SetupButtonListeners()
    {
        if (playPauseButton  != null) playPauseButton.onClick.AddListener(OnPlayPausePressed);
        if (restartButton    != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (skipToStartButton != null) skipToStartButton.onClick.AddListener(OnSkipToStartPressed);
        if (skipToEndButton  != null) skipToEndButton.onClick.AddListener(OnSkipToEndPressed);
        if (closeButton      != null) closeButton.onClick.AddListener(OnClosePressed);

        if (speedButton != null) speedButton.onClick.AddListener(OnSpeedButtonPressed);

        if (timelineSlider != null)
        {
            timelineSlider.onValueChanged.AddListener(OnTimelineSliderChanged);
        }
    }

    private void RemoveButtonListeners()
    {
        if (playPauseButton  != null) playPauseButton.onClick.RemoveListener(OnPlayPausePressed);
        if (restartButton    != null) restartButton.onClick.RemoveListener(OnRestartPressed);
        if (skipToStartButton != null) skipToStartButton.onClick.RemoveListener(OnSkipToStartPressed);
        if (skipToEndButton  != null) skipToEndButton.onClick.RemoveListener(OnSkipToEndPressed);
        if (closeButton      != null) closeButton.onClick.RemoveListener(OnClosePressed);

        if (speedButton != null) speedButton.onClick.RemoveListener(OnSpeedButtonPressed);

        if (timelineSlider != null)
            timelineSlider.onValueChanged.RemoveListener(OnTimelineSliderChanged);
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnPlayPausePressed()
    {
        replayManager.TogglePlayPause();
    }

    private void OnRestartPressed()
    {
        replayManager.RestartReplay();
    }

    private void OnSkipToStartPressed()
    {
        replayManager.SeekToStart();
        if (replayManager.State == ReplayState.Paused)
            replayManager.ResumeReplay();
    }

    private void OnSkipToEndPressed()
    {
        replayManager.SeekToEnd();
    }

    private void OnClosePressed()
    {
        replayManager.StopReplay();
    }

    private void OnSpeedButtonPressed()
    {
        if (speedOptions == null || speedOptions.Length == 0) return;

        _speedIndex = (_speedIndex + 1) % speedOptions.Length;
        float speed = speedOptions[_speedIndex];

        replayManager.SetPlaybackSpeed(speed);
        UpdateSpeedLabel(speed);
    }

    private void OnTimelineSliderChanged(float value)
    {
        // Ignore programmatic updates to avoid feedback loops.
        if (_suppressSliderCallback) return;

        if (replayManager.CurrentReplay == null) return;

        float seekTime = value * replayManager.CurrentReplay.TotalDuration;
        replayManager.SeekTo(seekTime);
    }

    // ── ReplayManager event handlers ──────────────────────────────────────────

    private void HandleReplayStarted(ReplayData data)
    {
        if (timelineSlider != null)
        {
            _suppressSliderCallback = true;
            timelineSlider.value = 0f;
            _suppressSliderCallback = false;
        }

        // Reset to the configured default speed each time a replay starts.
        _speedIndex = ClampSpeedIndex(defaultSpeedIndex);
        replayManager.SetPlaybackSpeed(speedOptions[_speedIndex]);

        UpdateTotalTimeLabel(data.TotalDuration);
        UpdateCurrentTimeLabel(0f);
        UpdateSpeedLabel(speedOptions[_speedIndex]);
        UpdatePlayPauseIcon(isPlaying: true);
        Show();
    }

    private void HandleReplayPaused()
    {
        UpdatePlayPauseIcon(isPlaying: false);
    }

    private void HandleReplayResumed()
    {
        UpdatePlayPauseIcon(isPlaying: true);
    }

    private void HandleReplayStopped()
    {
        Hide();
    }

    private void HandleReplayFinished()
    {
        UpdatePlayPauseIcon(isPlaying: false);
    }

    private void HandleReplayTimeChanged(float time)
    {
        UpdateCurrentTimeLabel(time);
        UpdateSliderPosition(time);
    }

    // ── Display helpers ───────────────────────────────────────────────────────

    private void UpdatePlayPauseIcon(bool isPlaying)
    {
        if (playPauseIcon == null) return;
        playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
    }

    private void UpdateCurrentTimeLabel(float time)
    {
        if (currentTimeLabel != null)
            currentTimeLabel.text = FormatTime(time);
    }

    private void UpdateTotalTimeLabel(float duration)
    {
        if (totalTimeLabel != null)
            totalTimeLabel.text = FormatTime(duration);
    }

    private void UpdateSpeedLabel(float speed)
    {
        string text = $"{speed:0.##}x";

        if (speedLabel != null)
            speedLabel.text = text;

        // If the button itself carries the label (e.g. no separate TMP text assigned
        // elsewhere), reflect the speed on the button too.
        if (speedButton != null)
        {
            var buttonLabel = speedButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null && buttonLabel != speedLabel)
                buttonLabel.text = text;
        }
    }

    private void UpdateSliderPosition(float time)
    {
        if (timelineSlider == null || replayManager.CurrentReplay == null) return;
        if (replayManager.CurrentReplay.TotalDuration <= 0f) return;

        _suppressSliderCallback = true;
        timelineSlider.value = time / replayManager.CurrentReplay.TotalDuration;
        _suppressSliderCallback = false;
    }

    private int ClampSpeedIndex(int index)
    {
        if (speedOptions == null || speedOptions.Length == 0) return 0;
        return Mathf.Clamp(index, 0, speedOptions.Length - 1);
    }

    // ── Visibility ────────────────────────────────────────────────────────────

    private void Show()
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    // ── Time formatting ───────────────────────────────────────────────────────

    private static string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60f);
        float secs = seconds % 60f;
        return mins > 0
            ? string.Format("{0:D2}:{1:00.00}", mins, secs)
            : string.Format("{0:00.00}", secs);
    }
}