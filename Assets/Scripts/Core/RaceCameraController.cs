using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Controls which athlete the Cinemachine camera tracks.
/// Subscribes to <see cref="ReplayManager"/> events so future camera modes
/// (broadcast, chase, finish-line, free-cam) can be layered on without
/// modifying replay or gameplay systems.
/// </summary>
public class RaceCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private void Start()
    {
        if (ReplayManager.Instance != null)
            SubscribeToReplayEvents(ReplayManager.Instance);
    }

    private void OnDestroy()
    {
        if (ReplayManager.Instance != null)
            UnsubscribeFromReplayEvents(ReplayManager.Instance);
    }

    public void FollowAthlete(Transform athleteTransform)
    {
        cinemachineCamera.Target.TrackingTarget = athleteTransform;
        cinemachineCamera.Target.LookAtTarget = athleteTransform;
    }

    // ── Replay event hooks ────────────────────────────────────────────────────
    // The camera already tracks the player's transform set during SpawnPlayerAthlete,
    // so no functional change is needed for the default replay mode.
    // These hooks are the extension points for future replay camera modes.

    private void SubscribeToReplayEvents(ReplayManager mgr)
    {
        mgr.OnReplayStarted += OnReplayStarted;
        mgr.OnReplayStopped += OnReplayStopped;
        mgr.OnReplayFinished += OnReplayStopped;
    }

    private void UnsubscribeFromReplayEvents(ReplayManager mgr)
    {
        mgr.OnReplayStarted -= OnReplayStarted;
        mgr.OnReplayStopped -= OnReplayStopped;
        mgr.OnReplayFinished -= OnReplayStopped;
    }

    private void OnReplayStarted(ReplayData data)
    {
        // Default replay mode: stay on the player athlete's transform (already set).
        // Future: switch to a broadcast or cinematic camera based on data.Distance, etc.
    }

    private void OnReplayStopped()
    {
        // Future: restore a gameplay camera mode after replay ends.
    }
}
