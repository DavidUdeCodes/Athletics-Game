using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Records athlete state every frame during an active race and packages it into
/// a sealed <see cref="ReplayData"/> when recording stops.
///
/// Responsibilities:
///   - Capture per-frame snapshots for every race participant.
///   - Remain completely passive; never modify athlete or race state.
///   - Hand the finished data to <see cref="ReplayManager"/> via RaceManager.
///
/// Attach to the same GameObject as RaceManager and assign via the inspector,
/// or call <see cref="Initialize"/> before recording starts.
/// </summary>
public class ReplayRecorder : MonoBehaviour
{
    private RaceManager _raceManager;
    private bool _isRecording;
    private float _recordingTime;

    private Athlete[] _trackedAthletes;
    private List<ReplayAthleteData> _athleteData;

    private ReplayData _completedData;

    // ── Public state ──────────────────────────────────────────────────────────

    public bool IsRecording => _isRecording;

    /// <summary>
    /// The sealed replay produced by the most recent recording session.
    /// Null until <see cref="StopRecording"/> is called.
    /// </summary>
    public ReplayData CompletedData => _completedData;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void Initialize(RaceManager raceManager)
    {
        _raceManager = raceManager;
    }

    // ── Recording control ─────────────────────────────────────────────────────

    /// <summary>
    /// Begin recording all athletes currently in the scene.
    /// Called by RaceManager when the gun fires (race officially starts).
    /// </summary>
    public void StartRecording()
    {
        if (_raceManager == null)
        {
            Debug.LogError("[ReplayRecorder] RaceManager not set. Call Initialize() first.");
            return;
        }

        _trackedAthletes = Object.FindObjectsByType<Athlete>();
        _athleteData = new List<ReplayAthleteData>(_trackedAthletes.Length);

        foreach (Athlete athlete in _trackedAthletes)
        {
            _athleteData.Add(new ReplayAthleteData(
                athlete.athleteName,
                athlete.nationality,
                athlete.isPlayer,
                athlete.AthleteLane
            ));
        }

        _recordingTime = 0f;
        _completedData = null;
        _isRecording = true;

        Debug.Log($"[ReplayRecorder] Started recording {_trackedAthletes.Length} athletes.");
    }

    /// <summary>
    /// Stop recording, seal the data, and return the finished <see cref="ReplayData"/>.
    /// Called by RaceManager once all athletes have come to rest.
    /// </summary>
    public ReplayData StopRecording(List<RaceResult> results)
    {
        if (!_isRecording)
            return _completedData;

        _isRecording = false;

        ApplyFinishResults(results);

        RaceDistance distance = (RaceDistance)Mathf.RoundToInt(_raceManager.RaceDistanceInMeters);
        _completedData = new ReplayData(distance, _athleteData, results ?? new List<RaceResult>());
        _completedData.Seal(_recordingTime);

        Debug.Log($"[ReplayRecorder] Stopped. Duration: {_recordingTime:F2}s, " +
                  $"Frames per athlete: {(_athleteData.Count > 0 ? _athleteData[0].Frames.Count : 0)}");

        return _completedData;
    }

    // ── Per-frame capture ─────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isRecording) return;

        _recordingTime += Time.deltaTime;

        for (int i = 0; i < _trackedAthletes.Length; i++)
        {
            Athlete athlete = _trackedAthletes[i];
            if (athlete == null) continue;

            _athleteData[i].AddFrame(new ReplayFrame
            {
                Timestamp        = _recordingTime,
                DistanceTravelled = athlete.CurrentDistance,
                NormalizedSpeed  = athlete.AnimationNormalizedSpeed,
                CurrentSpeed     = athlete.CurrentSpeed,
                RaceState        = athlete.CurrentAnimationState,
                HasFinished      = athlete.HasFinishedRace,
                WorldPosition    = athlete.transform.position,
                WorldRotation    = athlete.transform.rotation
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyFinishResults(List<RaceResult> results)
    {
        if (results == null) return;

        foreach (RaceResult result in results)
        {
            if (result.AthleteReference == null) continue;

            int lane = result.AthleteReference.AthleteLane;

            for (int i = 0; i < _trackedAthletes.Length; i++)
            {
                if (_trackedAthletes[i] != null && _trackedAthletes[i].AthleteLane == lane)
                {
                    _athleteData[i].SetFinishResult(result.Placement, result.FinishTime);
                    break;
                }
            }
        }
    }
}
