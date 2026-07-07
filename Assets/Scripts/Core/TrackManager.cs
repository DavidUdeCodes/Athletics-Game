using UnityEngine;
using UnityEngine.Splines;

public class TrackManager : MonoBehaviour
{
    [Header("Straightaway Splines (110m start line)")]
    [SerializeField] private SplineContainer[] straightawayLanes = new SplineContainer[8];
    
    [Header("200m Splines")]
    [SerializeField] private SplineContainer[] ovals200mLanes = new SplineContainer[8];
    
    [Header("400m Splines")]
    [SerializeField] private SplineContainer[] ovals400mLanes = new SplineContainer[8];
    
    private TrackConfiguration _trackConfig;
    
    private void Awake()
    {
        InitializeTrackConfiguration();
    }
    
    private void InitializeTrackConfiguration()
    {
        _trackConfig = new TrackConfiguration(straightawayLanes, ovals200mLanes, ovals400mLanes);
        
        if (!_trackConfig.IsValid)
            Debug.LogError("TrackConfiguration is not properly initialized - verify all 24 splines are assigned");
    }
    
    public TrackConfiguration GetTrackForRace(RaceDistance distance)
    {
        if (!_trackConfig.IsValid)
        {
            Debug.LogError("TrackConfiguration is invalid");
            return null;
        }
        return _trackConfig;
    }
}
