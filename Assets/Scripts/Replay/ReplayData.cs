using System.Collections.Generic;

/// <summary>
/// Immutable container for an entire recorded race session.
/// Writable only via <see cref="ReplayRecorder"/>; becomes immutable after <see cref="Seal"/> is called.
/// Designed so that ghost races and personal-best ghosts can later reuse this type without changes.
/// </summary>
public sealed class ReplayData
{
    // ── Session metadata ──────────────────────────────────────────────────────

    /// <summary>Race distance this replay was recorded for.</summary>
    public RaceDistance Distance { get; }

    /// <summary>Total recorded duration in seconds. Set when the session is sealed.</summary>
    public float TotalDuration { get; private set; }

    /// <summary>
    /// True once recording has finished and the data is safe to hand to the playback system.
    /// No new frames can be added after sealing.
    /// </summary>
    public bool IsSealed { get; private set; }

    // ── Per-athlete data ──────────────────────────────────────────────────────

    private readonly List<ReplayAthleteData> _athletes;

    /// <summary>Ordered list of per-athlete recorded data, one entry per race participant.</summary>
    public IReadOnlyList<ReplayAthleteData> Athletes => _athletes;

    // ── Race results ──────────────────────────────────────────────────────────

    private readonly List<RaceResult> _results;

    /// <summary>Final race results in finish order.</summary>
    public IReadOnlyList<RaceResult> Results => _results;

    // ── Construction ──────────────────────────────────────────────────────────

    public ReplayData(RaceDistance distance,
                      List<ReplayAthleteData> athletes,
                      List<RaceResult> results)
    {
        Distance = distance;
        _athletes = athletes ?? new List<ReplayAthleteData>();
        _results = results ?? new List<RaceResult>();
    }

    // ── Sealing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks the data as complete and immutable. Must be called before handing the
    /// replay to <see cref="ReplayManager"/>.
    /// </summary>
    internal void Seal(float totalDuration)
    {
        if (IsSealed) return;
        TotalDuration = totalDuration;
        IsSealed = true;
    }
}
