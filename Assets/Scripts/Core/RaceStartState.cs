using System;

public enum RaceStartState
{
    Idle,
    OnYourMarks,
    GetSet,
    Go,
    Running,
    FalseStart,
    Finished
}

public enum ReactionQuality
{
    PerfectStart,
    GreatStart,
    GoodStart,
    SlowStart,
    VerySlowStart
}

public class RaceStartEvents
{
    public static event Action<RaceStartState> OnRaceStateChanged;
    public static event Action OnFalseStart;
    public static event Action<float> OnReactionTimed;
    public static event Action<ReactionQuality, float> OnReactionQualityDetermined;

    public static void RaiseRaceStateChanged(RaceStartState newState)
    {
        OnRaceStateChanged?.Invoke(newState);
    }

    public static void RaiseFalseStart()
    {
        OnFalseStart?.Invoke();
    }

    public static void RaiseReactionTimed(float reactionTime)
    {
        OnReactionTimed?.Invoke(reactionTime);
    }

    public static void RaiseReactionQualityDetermined(ReactionQuality quality, float reactionTime)
    {
        OnReactionQualityDetermined?.Invoke(quality, reactionTime);
    }

    public static void ClearAllListeners()
    {
        OnRaceStateChanged = null;
        OnFalseStart = null;
        OnReactionTimed = null;
        OnReactionQualityDetermined = null;
    }
}
