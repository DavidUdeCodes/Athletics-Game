using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Drives a single athlete's transform and animation from pre-recorded <see cref="ReplayFrame"/> data.
/// Added to an athlete GameObject by <see cref="ReplayManager"/> at replay start and destroyed when replay ends.
///
/// Responsibilities:
///   - Disable all gameplay components so no simulation runs during replay.
///   - Interpolate between neighbouring frames for smooth playback at any speed.
///   - Apply world-space position/rotation and Animator parameters each replay tick.
///   - Re-enable disabled components when destroyed (defensive cleanup).
/// </summary>
public class ReplayPlaybackController : MonoBehaviour
{
    private ReplayAthleteData _data;

    // Components this controller drives during replay.
    private AthleteAnimationController _animationController;

    // Components disabled during replay to prevent gameplay systems from interfering.
    private Athlete _athlete;
    private AthleteMovement _athleteMovement;
    private SprintController _sprintController;
    private MomentumController _momentumController;
    private AISprinterController _aiController;
    private AthleteInput _athleteInput;

    // ── Setup ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="ReplayManager"/> immediately after AddComponent.
    /// Caches references, disables gameplay components, and applies the first frame.
    /// </summary>
    public void Initialize(ReplayAthleteData data, Athlete athlete)
    {
        _data = data;
        _athlete = athlete;

        _animationController = athlete.GetComponent<AthleteAnimationController>();
        _athleteMovement     = athlete.GetComponent<AthleteMovement>();
        _sprintController    = athlete.GetComponent<SprintController>();
        _momentumController  = athlete.GetComponent<MomentumController>();
        _aiController        = athlete.GetComponent<AISprinterController>();
        _athleteInput        = athlete.GetComponent<AthleteInput>();

        DisableGameplayComponents();
        ApplyTime(0f);
    }

    private void OnDestroy()
    {
        RestoreGameplayComponents();
    }

    // ── Playback API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Sample the recording at <paramref name="timestamp"/> and apply the interpolated
    /// state to the athlete's transform and Animator. Safe to call every frame or on seek.
    /// </summary>
    public void ApplyTime(float timestamp)
    {
        if (_data == null || _data.Frames.Count == 0) return;

        IReadOnlyList<ReplayFrame> frames = _data.Frames;

        if (frames.Count == 1)
        {
            ApplyFrame(frames[0]);
            return;
        }

        int nextIndex = FindNextFrameIndex(frames, timestamp);

        if (nextIndex <= 0)
        {
            ApplyFrame(frames[0]);
            return;
        }

        if (nextIndex >= frames.Count)
        {
            ApplyFrame(frames[frames.Count - 1]);
            return;
        }

        ReplayFrame prev = frames[nextIndex - 1];
        ReplayFrame next = frames[nextIndex];

        float span = next.Timestamp - prev.Timestamp;
        float t    = span > 0f ? (timestamp - prev.Timestamp) / span : 1f;

        ApplyFrame(Interpolate(prev, next, t));
    }

    // ── Frame sampling ────────────────────────────────────────────────────────

    /// <summary>
    /// Binary search: returns the index of the first frame with Timestamp > <paramref name="timestamp"/>.
    /// </summary>
    private static int FindNextFrameIndex(IReadOnlyList<ReplayFrame> frames, float timestamp)
    {
        int lo = 0;
        int hi = frames.Count;

        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (frames[mid].Timestamp <= timestamp)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo;
    }

    private static ReplayFrame Interpolate(in ReplayFrame a, in ReplayFrame b, float t)
    {
        return new ReplayFrame
        {
            Timestamp         = Mathf.Lerp(a.Timestamp, b.Timestamp, t),
            DistanceTravelled = Mathf.Lerp(a.DistanceTravelled, b.DistanceTravelled, t),
            NormalizedSpeed   = Mathf.Lerp(a.NormalizedSpeed, b.NormalizedSpeed, t),
            CurrentSpeed      = Mathf.Lerp(a.CurrentSpeed, b.CurrentSpeed, t),
            // Discrete values: snap at 50% so transitions happen at the correct moment.
            RaceState         = t >= 0.5f ? b.RaceState : a.RaceState,
            HasFinished       = t >= 0.5f ? b.HasFinished : a.HasFinished,
            WorldPosition     = Vector3.Lerp(a.WorldPosition, b.WorldPosition, t),
            WorldRotation     = Quaternion.Slerp(a.WorldRotation, b.WorldRotation, t)
        };
    }

    private void ApplyFrame(in ReplayFrame frame)
    {
        transform.position = frame.WorldPosition;
        transform.rotation = frame.WorldRotation;

        if (_animationController != null)
        {
            _animationController.SetRaceState(frame.RaceState);
            _animationController.SetNormalizedSpeed(frame.NormalizedSpeed);
        }
    }

    // ── Component management ──────────────────────────────────────────────────

    private void DisableGameplayComponents()
    {
        if (_athleteMovement != null) _athleteMovement.enabled = false;
        if (_sprintController != null) _sprintController.enabled = false;
        if (_momentumController != null) _momentumController.enabled = false;
        if (_aiController != null) _aiController.enabled = false;
        if (_athleteInput != null) _athleteInput.SetEnabled(false);
    }

    private void RestoreGameplayComponents()
    {
        if (_athleteMovement != null) _athleteMovement.enabled = true;
        if (_sprintController != null) _sprintController.enabled = true;
        if (_momentumController != null) _momentumController.enabled = true;
        if (_aiController != null) _aiController.enabled = true;
    }
}
