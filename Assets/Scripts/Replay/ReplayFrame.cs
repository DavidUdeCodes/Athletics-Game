using UnityEngine;

/// <summary>
/// Snapshot of a single athlete's state at one point in time during a recorded race.
/// Designed to be lightweight and allocation-free (struct).
/// </summary>
[System.Serializable]
public struct ReplayFrame
{
    /// <summary>Elapsed replay time in seconds when this frame was captured.</summary>
    public float Timestamp;

    /// <summary>Distance travelled along the spline relative to the race start offset (metres).</summary>
    public float DistanceTravelled;

    /// <summary>
    /// Normalised 0–1 speed value that was being fed to the Animator blend tree.
    /// Captured directly from the source (momentum for player, NormalizedSpeed for AI).
    /// </summary>
    public float NormalizedSpeed;

    /// <summary>Absolute speed in metres per second at this frame.</summary>
    public float CurrentSpeed;

    /// <summary>Animation state machine state at this frame.</summary>
    public RaceStartState RaceState;

    /// <summary>Whether the athlete had crossed the finish line at this frame.</summary>
    public bool HasFinished;

    /// <summary>World-space position used for direct transform application during playback.</summary>
    public Vector3 WorldPosition;

    /// <summary>World-space rotation used for direct transform application during playback.</summary>
    public Quaternion WorldRotation;
}
