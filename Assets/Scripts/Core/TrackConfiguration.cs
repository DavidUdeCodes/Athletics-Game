using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class TrackConfiguration
{
    private SplineContainer[] _straightawaySplines = new SplineContainer[8];
    private SplineContainer[] _200mSplines = new SplineContainer[8];
    private SplineContainer[] _400mSplines = new SplineContainer[8];

    public TrackConfiguration(
        SplineContainer[] straightawayLanes,
        SplineContainer[] ovals200mLanes,
        SplineContainer[] ovals400mLanes)
    {
        ValidateAndAssignSplines(straightawayLanes, _straightawaySplines, "Straightaway");
        ValidateAndAssignSplines(ovals200mLanes, _200mSplines, "200m");
        ValidateAndAssignSplines(ovals400mLanes, _400mSplines, "400m");
    }

    private void ValidateAndAssignSplines(SplineContainer[] source, SplineContainer[] target, string group)
    {
        if (source == null || source.Length < 8)
        {
            Debug.LogError($"TrackConfiguration: {group} group requires exactly 8 splines");
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            target[i] = source[i];
            if (target[i] == null)
            {
                Debug.LogError($"TrackConfiguration: {group} Lane {i + 1} spline is null");
            }
        }
    }

    public SplineContainer GetSpline(RaceDistance distance, int lane)
    {
        lane = Mathf.Clamp(lane, 1, 8);
        int laneIndex = lane - 1;

        return distance switch
        {
            RaceDistance.Distance100m => _straightawaySplines[laneIndex],
            RaceDistance.Distance200m => _200mSplines[laneIndex],
            RaceDistance.Distance400m => _400mSplines[laneIndex],
            _ => null
        };
    }

    public float GetSplineLength(RaceDistance distance, int lane)
    {
        SplineContainer spline = GetSpline(distance, lane);
        return spline != null ? spline.CalculateLength() : 0f;
    }

    public bool IsValid
    {
        get
        {
            for (int i = 0; i < 8; i++)
            {
                if (_straightawaySplines[i] == null || _200mSplines[i] == null || _400mSplines[i] == null)
                    return false;
            }
            return true;
        }
    }
}

public class RaceConfiguration
{
    public RaceDistance RaceDistance { get; private set; }
    public int PlayerLane { get; set; }
    public TrackConfiguration TrackConfig { get; private set; }

    public RaceConfiguration(RaceDistance raceDistance, int playerLane, TrackConfiguration trackConfig)
    {
        RaceDistance = raceDistance;
        PlayerLane = playerLane;
        TrackConfig = trackConfig;
    }

    public float GetFinishDistance() => (float)RaceDistance;

    public SplineContainer GetSplineForAthlete(int athleteLane)
    {
        return TrackConfig.GetSpline(RaceDistance, athleteLane);
    }

    public float GetStartDistanceForAthlete(int athleteLane)
    {
        if (RaceDistance == RaceDistance.Distance100m)
        {
            SplineContainer spline = GetSplineForAthlete(athleteLane);
            if (spline == null) return 0f;
            
            float splineLength = spline.CalculateLength();
            return splineLength > 0f ? 10f : 0f;
        }

        return 0f;
    }

    public Vector3 GetStartPositionForAthlete(int athleteLane)
    {
        SplineContainer spline = GetSplineForAthlete(athleteLane);
        if (spline == null)
            return Vector3.zero;

        float startDistance = GetStartDistanceForAthlete(athleteLane);
        float splineLength = spline.CalculateLength();
        
        if (splineLength <= 0f)
            return Vector3.zero;

        float normalizedStart = startDistance / splineLength;

        spline.Spline.Evaluate(
            normalizedStart,
            out float3 pos,
            out float3 tangent,
            out float3 up);

        return spline.transform.TransformPoint((Vector3)pos);
    }

    public bool IsValid => TrackConfig.IsValid && RaceDistance > 0;
}


public struct AthleteStartState
{
    public float StartDistanceMeters { get; set; }

    public AthleteStartState(float startDistance)
    {
        StartDistanceMeters = startDistance;
    }
}
