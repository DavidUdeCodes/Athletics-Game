using System.Collections.Generic;

/// <summary>
/// All recorded frames for a single athlete across an entire race session.
/// Mutable only during recording; sealed once <see cref="ReplayData.Seal"/> is called.
/// Designed to also serve ghost/personal-best use cases without structural changes.
/// </summary>
public sealed class ReplayAthleteData
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public string AthleteName { get; }
    public string Nationality { get; }
    public bool IsPlayer { get; }
    public int Lane { get; }

    // ── Results ───────────────────────────────────────────────────────────────

    public int FinishPosition { get; private set; }
    public float FinishTime { get; private set; }

    // ── Recorded frames ───────────────────────────────────────────────────────

    private readonly List<ReplayFrame> _frames;

    /// <summary>Read-only view of all recorded frames, in chronological order.</summary>
    public IReadOnlyList<ReplayFrame> Frames => _frames;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <param name="initialCapacity">
    /// Pre-allocate list capacity to avoid resizing during recording.
    /// Default 3600 covers 60s at 60fps with no reallocation.
    /// </param>
    public ReplayAthleteData(string name, string nationality, bool isPlayer, int lane,
                             int initialCapacity = 3600)
    {
        AthleteName = name;
        Nationality = nationality;
        IsPlayer = isPlayer;
        Lane = lane;
        _frames = new List<ReplayFrame>(initialCapacity);
    }

    // ── Recording-only API (called by ReplayRecorder) ─────────────────────────

    internal void AddFrame(ReplayFrame frame)
    {
        _frames.Add(frame);
    }

    internal void SetFinishResult(int position, float time)
    {
        FinishPosition = position;
        FinishTime = time;
    }
}
