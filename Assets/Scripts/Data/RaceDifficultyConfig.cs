using UnityEngine;

/// <summary>
/// Defines realistic finish-time ranges for each race distance at a given difficulty.
/// Assign one instance to RaceManager. Add a new EventTimingRange entry here whenever
/// a new track event is introduced — no other code changes are required.
/// </summary>
[CreateAssetMenu(fileName = "RaceDifficultyConfig", menuName = "Athletics/Race Difficulty Config")]
public class RaceDifficultyConfig : ScriptableObject
{
    [System.Serializable]
    public struct EventTimingRange
    {
        [Tooltip("Race distance this range applies to.")]
        public RaceDistance distance;

        [Tooltip("Target finish time (seconds) at maximum difficulty — the fastest AI runners will aim for this.")]
        [Min(1f)]
        public float eliteTime;

        [Tooltip("Target finish time (seconds) at minimum difficulty — the slowest AI runners will aim for this.")]
        [Min(1f)]
        public float easyTime;

        [Tooltip("Maximum per-athlete time spread (seconds) added on top of the difficulty-derived base time " +
                 "so runners within a race are spread apart rather than all targeting identical times.")]
        [Min(0f)]
        public float athleteSpread;
    }

    [SerializeField]
    [Tooltip("One entry per race distance. Difficulty 0 = easiest (slowest AI), 1 = hardest (fastest AI).")]
    private EventTimingRange[] _eventRanges = new EventTimingRange[]
    {
        new EventTimingRange { distance = RaceDistance.Distance100m, eliteTime =  9.85f, easyTime = 12.5f, athleteSpread = 0.40f },
        new EventTimingRange { distance = RaceDistance.Distance200m, eliteTime = 19.90f, easyTime = 26.0f, athleteSpread = 0.60f },
        new EventTimingRange { distance = RaceDistance.Distance400m, eliteTime = 43.50f, easyTime = 58.0f, athleteSpread = 1.20f },
    };

    /// <summary>
    /// Returns a target finish time for a single AI athlete.
    /// Each call applies a random spread so athletes in the same race target different times.
    /// </summary>
    /// <param name="distance">The race distance for this event.</param>
    /// <param name="difficulty">0 = easiest (slowest AI), 1 = hardest (fastest AI).</param>
    public float GetTargetTime(RaceDistance distance, float difficulty)
    {
        float clampedDifficulty = Mathf.Clamp01(difficulty);

        foreach (EventTimingRange range in _eventRanges)
        {
            if (range.distance != distance)
                continue;

            float baseTime = Mathf.Lerp(range.easyTime, range.eliteTime, clampedDifficulty);
            float spread   = Random.Range(-range.athleteSpread, range.athleteSpread);

            // Never let the generated time go below the elite ceiling.
            return Mathf.Max(range.eliteTime, baseTime + spread);
        }

        Debug.LogWarning($"[RaceDifficultyConfig] No timing range configured for {distance}. Returning 12 s fallback.");
        return 12f;
    }
}
