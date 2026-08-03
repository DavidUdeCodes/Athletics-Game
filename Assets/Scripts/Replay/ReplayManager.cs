using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>Current state of the replay system.</summary>
public enum ReplayState { Idle, Playing, Paused }

/// <summary>
/// Singleton that owns replay playback.
///
/// Responsibilities:
///   - Store completed <see cref="ReplayData"/> from <see cref="ReplayRecorder"/>.
///   - Maintain an independent internal clock (unaffected by gameplay time).
///   - Expose events so UI, cameras, and other systems can react without tight coupling.
///   - Spawn and clean up <see cref="ReplayPlaybackController"/> components on athlete GameObjects.
///
/// RaceManager, ResultsScreen, and UI all communicate via the Instance singleton
/// and the events below.
/// </summary>
public class ReplayManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ReplayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when replay playback begins. Provides the replay data being played.</summary>
    public event Action<ReplayData> OnReplayStarted;

    /// <summary>Fired when the user pauses replay.</summary>
    public event Action OnReplayPaused;

    /// <summary>Fired when the user resumes a paused replay.</summary>
    public event Action OnReplayResumed;

    /// <summary>Fired when replay is stopped before completion (e.g. user exits).</summary>
    public event Action OnReplayStopped;

    /// <summary>Fired when the replay clock reaches the end of the recorded data.</summary>
    public event Action OnReplayFinished;

    /// <summary>Fired every frame during playback and on any seek. Provides current replay time.</summary>
    public event Action<float> OnReplayTimeChanged;

    // ── State ─────────────────────────────────────────────────────────────────

    private ReplayData _pendingReplay;
    private ReplayData _currentReplay;
    private ReplayState _replayState = ReplayState.Idle;
    private float _replayTime;
    private float _playbackSpeed = 1f;

    private readonly List<ReplayPlaybackController> _playbackControllers = new();

    // ── Public reads ──────────────────────────────────────────────────────────

    public ReplayState State => _replayState;
    public ReplayData CurrentReplay => _currentReplay;
    public float ReplayTime => _replayTime;
    public float PlaybackSpeed => _playbackSpeed;
    public bool IsReplaying => _replayState != ReplayState.Idle;
    public bool HasPendingReplay => _pendingReplay != null;

    // ── Data API (called by RaceManager after recording finishes) ─────────────

    /// <summary>
    /// Stores a sealed replay for later playback.
    /// ResultsScreen calls <see cref="StartReplay()"/> (no args) to play it.
    /// </summary>
    public void SetCompletedReplay(ReplayData data)
    {
        _pendingReplay = data;
    }

    // ── Playback control ──────────────────────────────────────────────────────

    /// <summary>Start replay using the most recently recorded session.</summary>
    public void StartReplay()
    {
        StartReplay(_pendingReplay);
    }

    /// <summary>Start replay using the provided data (used for ghost races, etc.).</summary>
    public void StartReplay(ReplayData data)
    {
        if (data == null || !data.IsSealed)
        {
            Debug.LogWarning("[ReplayManager] No valid sealed ReplayData to play.");
            return;
        }

        if (_replayState != ReplayState.Idle)
            CleanupPlaybackControllers();

        _currentReplay = data;
        _replayTime = 0f;
        _playbackSpeed = 1f;
        _replayState = ReplayState.Playing;

        SetupPlaybackControllers();

        OnReplayStarted?.Invoke(_currentReplay);
        OnReplayTimeChanged?.Invoke(_replayTime);
    }

    public void PauseReplay()
    {
        if (_replayState != ReplayState.Playing) return;
        _replayState = ReplayState.Paused;
        OnReplayPaused?.Invoke();
    }

    public void ResumeReplay()
    {
        if (_replayState != ReplayState.Paused) return;
        _replayState = ReplayState.Playing;
        OnReplayResumed?.Invoke();
    }

    public void TogglePlayPause()
    {
        if (_replayState == ReplayState.Playing)
            PauseReplay();
        else if (_replayState == ReplayState.Paused)
            ResumeReplay();
    }

    public void StopReplay()
    {
        if (_replayState == ReplayState.Idle) return;
        _replayState = ReplayState.Idle;
        CleanupPlaybackControllers();
        OnReplayStopped?.Invoke();
    }

    /// <summary>
    /// Jump the replay clock to the specified time and immediately apply it to all athletes.
    /// Works for both forward and backward scrubbing.
    /// </summary>
    public void SeekTo(float time)
    {
        if (_currentReplay == null) return;
        _replayTime = Mathf.Clamp(time, 0f, _currentReplay.TotalDuration);
        ApplyTimeToAllAthletes(_replayTime);
        OnReplayTimeChanged?.Invoke(_replayTime);
    }

    public void SeekToStart() => SeekTo(0f);
    public void SeekToEnd() => SeekTo(_currentReplay != null ? _currentReplay.TotalDuration : 0f);

    public void RestartReplay()
    {
        SeekToStart();
        if (_replayState == ReplayState.Paused)
            ResumeReplay();
    }

    /// <summary>Sets playback speed multiplier. Affects only the replay clock.</summary>
    public void SetPlaybackSpeed(float speed)
    {
        _playbackSpeed = speed;
    }

    // ── Internal clock ────────────────────────────────────────────────────────

    private void Update()
    {
        if (_replayState != ReplayState.Playing) return;

        _replayTime += Time.deltaTime * _playbackSpeed;

        if (_replayTime >= _currentReplay.TotalDuration)
        {
            _replayTime = _currentReplay.TotalDuration;
            ApplyTimeToAllAthletes(_replayTime);
            _replayState = ReplayState.Paused;
            OnReplayTimeChanged?.Invoke(_replayTime);
            OnReplayFinished?.Invoke();
            return;
        }

        ApplyTimeToAllAthletes(_replayTime);
        OnReplayTimeChanged?.Invoke(_replayTime);
    }

    // ── Playback controllers ──────────────────────────────────────────────────

    private void SetupPlaybackControllers()
    {
        Athlete[] sceneAthletes = FindObjectsByType<Athlete>();

        foreach (ReplayAthleteData athleteData in _currentReplay.Athletes)
        {
            Athlete match = FindAthleteByLane(sceneAthletes, athleteData.Lane);
            if (match == null)
            {
                Debug.LogWarning($"[ReplayManager] No athlete found in lane {athleteData.Lane} for replay.");
                continue;
            }

            ReplayPlaybackController controller = match.gameObject.AddComponent<ReplayPlaybackController>();
            controller.Initialize(athleteData, match);
            _playbackControllers.Add(controller);
        }
    }

    private void ApplyTimeToAllAthletes(float time)
    {
        foreach (ReplayPlaybackController controller in _playbackControllers)
        {
            if (controller != null)
                controller.ApplyTime(time);
        }
    }

    private void CleanupPlaybackControllers()
    {
        foreach (ReplayPlaybackController controller in _playbackControllers)
        {
            if (controller != null)
                Destroy(controller);
        }
        _playbackControllers.Clear();
    }

    private static Athlete FindAthleteByLane(Athlete[] athletes, int lane)
    {
        foreach (Athlete a in athletes)
        {
            if (a.AthleteLane == lane)
                return a;
        }
        return null;
    }
}
