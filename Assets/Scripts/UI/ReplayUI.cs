using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Video-player-style replay controls.
/// Subscribes to <see cref="ReplayManager"/> events so it never polls and stays
/// decoupled from playback internals.
///
/// Shows automatically when replay starts, hides when replay ends or is stopped.
///
/// Scrubbing behaviour (issue fix):
///   - While the user is dragging the timeline slider, the current-time label shows
///     a live preview of the target timestamp.
///   - Athletes and camera are NOT moved during the drag.
///   - The actual seek only fires when the pointer is released.
///   - Playback state (playing/paused) is preserved across drags.
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
    [Tooltip("Index into speedOptions that playback starts at (2 = 1x with the default array above).")]
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

    /// <summary>Suppresses <see cref="OnTimelineSliderChanged"/> while we set the slider value in code.</summary>
    private bool _suppressSliderCallback;

    /// <summary>True while the user's pointer is held down on the timeline slider.</summary>
    private bool _isDragging;

    /// <summary>Whether replay was playing when the drag began; used to restore state after seek.</summary>
    private bool _wasPlayingBeforeDrag;

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
        replayManager.OnReplayStarted     += HandleReplayStarted;
        replayManager.OnReplayPaused      += HandleReplayPaused;
        replayManager.OnReplayResumed     += HandleReplayResumed;
        replayManager.OnReplayStopped     += HandleReplayStopped;
        replayManager.OnReplayFinished    += HandleReplayFinished;
        replayManager.OnReplayTimeChanged += HandleReplayTimeChanged;
    }

    private void UnsubscribeFromReplayEvents()
    {
        if (replayManager == null) return;
        replayManager.OnReplayStarted     -= HandleReplayStarted;
        replayManager.OnReplayPaused      -= HandleReplayPaused;
        replayManager.OnReplayResumed     -= HandleReplayResumed;
        replayManager.OnReplayStopped     -= HandleReplayStopped;
        replayManager.OnReplayFinished    -= HandleReplayFinished;
        replayManager.OnReplayTimeChanged -= HandleReplayTimeChanged;
    }

    // ── Button + slider wiring ────────────────────────────────────────────────

    private void SetupButtonListeners()
    {
        if (playPauseButton   != null) playPauseButton.onClick.AddListener(OnPlayPausePressed);
        if (restartButton     != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (skipToStartButton != null) skipToStartButton.onClick.AddListener(OnSkipToStartPressed);
        if (skipToEndButton   != null) skipToEndButton.onClick.AddListener(OnSkipToEndPressed);
        if (closeButton       != null) closeButton.onClick.AddListener(OnClosePressed);
        if (speedButton       != null) speedButton.onClick.AddListener(OnSpeedButtonPressed);

        SetupSliderListeners();
    }

    private void RemoveButtonListeners()
    {
        if (playPauseButton   != null) playPauseButton.onClick.RemoveListener(OnPlayPausePressed);
        if (restartButton     != null) restartButton.onClick.RemoveListener(OnRestartPressed);
        if (skipToStartButton != null) skipToStartButton.onClick.RemoveListener(OnSkipToStartPressed);
        if (skipToEndButton   != null) skipToEndButton.onClick.RemoveListener(OnSkipToEndPressed);
        if (closeButton       != null) closeButton.onClick.RemoveListener(OnClosePressed);
        if (speedButton       != null) speedButton.onClick.RemoveListener(OnSpeedButtonPressed);

        RemoveSliderListeners();
    }

    // ── Slider drag wiring ────────────────────────────────────────────────────

    private void SetupSliderListeners()
    {
        if (timelineSlider == null) return;

        timelineSlider.onValueChanged.AddListener(OnTimelineSliderChanged);

        // Use EventTrigger to detect pointer-down (drag start) and pointer-up
        // (drag end / seek commit) without polling Input each frame.
        EventTrigger trigger = timelineSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = timelineSlider.gameObject.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(_ => OnSliderPointerDown());
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(_ => OnSliderPointerUp());
        trigger.triggers.Add(pointerUp);
    }

    private void RemoveSliderListeners()
    {
        if (timelineSlider == null) return;

        timelineSlider.onValueChanged.RemoveListener(OnTimelineSliderChanged);

        var trigger = timelineSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
            trigger.triggers.Clear();
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnPlayPausePressed()   => replayManager.TogglePlayPause();
    private void OnRestartPressed()     => replayManager.RestartReplay();
    private void OnClosePressed()       => replayManager.StopReplay();

    private void OnSkipToStartPressed()
    {
        replayManager.SeekToStart();
        if (replayManager.State == ReplayState.Paused)
            replayManager.ResumeReplay();
    }

    private void OnSkipToEndPressed() => replayManager.SeekToEnd();

    private void OnSpeedButtonPressed()
    {
        if (speedOptions == null || speedOptions.Length == 0) return;

        _speedIndex = (_speedIndex + 1) % speedOptions.Length;
        float speed = speedOptions[_speedIndex];

        replayManager.SetPlaybackSpeed(speed);
        UpdateSpeedLabel(speed);
    }

    // ── Slider drag handlers ──────────────────────────────────────────────────

    private void OnSliderPointerDown()
    {
        _isDragging = true;
        _wasPlayingBeforeDrag = replayManager.State == ReplayState.Playing;
        // Do not pause replay here — we just suppress seeks while dragging.
        // Athletes freeze naturally because ReplayManager.Update only advances the
        // clock during Playing state, and we are not seeking.
    }

    private void OnSliderPointerUp()
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (replayManager.CurrentReplay == null) return;

        // Commit the seek to the position the slider was released at.
        float seekTime = timelineSlider.value * replayManager.CurrentReplay.TotalDuration;
        replayManager.SeekTo(seekTime);

        // Restore playback if it was running before the drag started.
        if (_wasPlayingBeforeDrag && replayManager.State == ReplayState.Paused)
            replayManager.ResumeReplay();
    }

    private void OnTimelineSliderChanged(float value)
    {
        // Ignore updates we triggered in code.
        if (_suppressSliderCallback) return;
        if (replayManager.CurrentReplay == null) return;

        if (_isDragging)
        {
            // Preview only: update the time label so the user can see where they
            // will land, but do not seek (no camera movement, no athlete movement).
            float previewTime = value * replayManager.CurrentReplay.TotalDuration;
            UpdateCurrentTimeLabel(previewTime);
            return;
        }

        // Non-drag change (e.g. keyboard stepping the slider): seek immediately.
        float seekTime = value * replayManager.CurrentReplay.TotalDuration;
        replayManager.SeekTo(seekTime);
    }

    // ── ReplayManager event handlers ──────────────────────────────────────────

    private void HandleReplayStarted(ReplayData data)
    {
        _suppressSliderCallback = true;
        if (timelineSlider != null) timelineSlider.value = 0f;
        _suppressSliderCallback = false;

        _isDragging = false;

        // Reset to configured default speed each time a replay starts.
        _speedIndex = ClampSpeedIndex(defaultSpeedIndex);
        replayManager.SetPlaybackSpeed(speedOptions[_speedIndex]);

        UpdateTotalTimeLabel(data.TotalDuration);
        UpdateCurrentTimeLabel(0f);
        UpdateSpeedLabel(speedOptions[_speedIndex]);
        UpdatePlayPauseIcon(isPlaying: true);
        Show();
    }

    private void HandleReplayPaused()  => UpdatePlayPauseIcon(isPlaying: false);
    private void HandleReplayResumed() => UpdatePlayPauseIcon(isPlaying: true);
    private void HandleReplayStopped() => Hide();
    private void HandleReplayFinished() => UpdatePlayPauseIcon(isPlaying: false);

    private void HandleReplayTimeChanged(float time)
    {
        // While dragging, the preview label is controlled by OnTimelineSliderChanged;
        // don't let replay clock updates overwrite it.
        if (_isDragging) return;

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

        // If the button itself carries the label (no separate TMP text assigned),
        // reflect the speed on the button too.
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
