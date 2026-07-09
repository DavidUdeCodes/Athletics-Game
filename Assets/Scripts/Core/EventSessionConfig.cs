public enum LaneSelectionMode
{
    Random,
    Fixed
}

public class EventSessionConfig
{
    public RaceDistance SelectedDistance { get; set; }
    public LaneSelectionMode LaneMode { get; set; }
    public int FixedLane { get; set; }
    public SprintInputMode InputMode { get; set; }

    public EventSessionConfig()
    {
        SelectedDistance = RaceDistance.Distance100m;
        LaneMode = LaneSelectionMode.Random;
        FixedLane = 1;
        InputMode = SprintInputMode.Rhythm;
    }

    public EventSessionConfig(RaceDistance distance, LaneSelectionMode laneMode, int fixedLane, SprintInputMode inputMode)
    {
        SelectedDistance = distance;
        LaneMode = laneMode;
        FixedLane = System.Math.Clamp(fixedLane, 1, 8);
        InputMode = inputMode;
    }

    public int GetPlayerLane(int defaultLane = 1)
    {
        if (LaneMode == LaneSelectionMode.Fixed)
            return FixedLane;

        return UnityEngine.Random.Range(1, 9);
    }

    public override string ToString()
    {
        string laneInfo = LaneMode == LaneSelectionMode.Fixed 
            ? $"Lane {FixedLane}" 
            : "Random Lane";
        
        return $"{SelectedDistance}m | {laneInfo} | {InputMode}";
    }
}
