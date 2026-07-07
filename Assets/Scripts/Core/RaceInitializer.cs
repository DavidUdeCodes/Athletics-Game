using UnityEngine;
using UnityEngine.Splines;

public static class RaceInitializer
{
    public static void InitializeAthleteForRace(
        Athlete athlete,
        RaceConfiguration raceConfig,
        int athleteLane)
    {
        if (!raceConfig.IsValid)
        {
            Debug.LogError($"Cannot initialize athlete {athlete.name}: Invalid race configuration");
            return;
        }

        AthleteMovement movement = athlete.GetComponent<AthleteMovement>();
        if (movement == null)
        {
            Debug.LogError($"Cannot initialize athlete {athlete.name}: No AthleteMovement component");
            return;
        }

        SplineContainer targetSpline = raceConfig.GetSplineForAthlete(athleteLane);
        if (targetSpline == null)
        {
            Debug.LogError($"Cannot initialize athlete {athlete.name}: No spline for {raceConfig.RaceDistance}m race, lane {athleteLane}");
            return;
        }

        float startDistance = raceConfig.GetStartDistanceForAthlete(athleteLane);
        ApplyStartState(athlete, movement, targetSpline, startDistance, raceConfig, athleteLane);
    }

    private static void ApplyStartState(
        Athlete athlete,
        AthleteMovement movement,
        SplineContainer spline,
        float startDistance,
        RaceConfiguration raceConfig,
        int athleteLane)
    {
        movement.SetSpline(spline);
        movement.SetShouldLoop(raceConfig.RaceDistance == RaceDistance.Distance400m);
        movement.PositionAtDistance(startDistance);

        Vector3 startWorldPos = raceConfig.GetStartPositionForAthlete(athleteLane);
        athlete.transform.position = startWorldPos;

        LogInitialization(raceConfig, athleteLane, startDistance, athlete.name, spline);
    }

    private static void LogInitialization(
        RaceConfiguration raceConfig,
        int athleteLane,
        float startDistance,
        string athleteName,
        SplineContainer spline)
    {
        float splineLength = spline.CalculateLength();
        
        string log = $"[RACE INIT] {athleteName} - {raceConfig.RaceDistance}m Race, Lane {athleteLane}\n";
        log += $"  Spline Length: {splineLength:F2}m\n";
        log += $"  Start Distance: {startDistance:F2}m";
        
        Debug.Log(log);
    }
}

