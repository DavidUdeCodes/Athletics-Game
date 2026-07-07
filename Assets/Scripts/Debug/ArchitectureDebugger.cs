using UnityEngine;
using UnityEngine.Splines;

public class ArchitectureDebugger : MonoBehaviour
{
    [SerializeField] private bool showDebugInfo = true;
    
    private RaceManager _raceManager;
    private TrackManager _trackManager;
    
    private void Start()
    {
        _raceManager = FindAnyObjectByType<RaceManager>();
        _trackManager = FindAnyObjectByType<TrackManager>();
    }
    
    [ContextMenu("Debug: Validate Race Configuration")]
    public void DebugRaceConfiguration()
    {
        if (_raceManager == null)
        {
            Debug.LogError("RaceManager not found");
            return;
        }
        
        RaceConfiguration config = _raceManager.CurrentRaceConfig;
        
        Debug.Log("=== RACE CONFIGURATION DEBUG ===");
        Debug.Log($"Race Distance: {config.RaceDistance}m");
        Debug.Log($"Player Lane: {_raceManager.PlayerLane}");
        Debug.Log($"Track Valid: {config.TrackConfig.IsValid}");
    }
    
    [ContextMenu("Debug: Show All Lane Splines")]
    public void DebugAllLaneSplines()
    {
        if (_raceManager == null)
        {
            Debug.LogError("RaceManager not found");
            return;
        }
        
        RaceConfiguration config = _raceManager.CurrentRaceConfig;
        
        if (!config.IsValid)
        {
            Debug.LogError("Race configuration invalid");
            return;
        }
        
        Debug.Log("=== ALL LANE SPLINES ===");
        
        for (int lane = 1; lane <= 8; lane++)
        {
            SplineContainer spline = config.GetSplineForAthlete(lane);
            float length = spline != null ? spline.CalculateLength() : 0f;
            
            Debug.Log($"Lane {lane}: {(spline != null ? spline.name : "NULL")}, length={length:F2}m");
        }
    }
    
    [ContextMenu("Debug: Show Athlete Start Positions")]
    public void DebugAthleteStartPositions()
    {
        if (_raceManager == null)
        {
            Debug.LogError("RaceManager not found");
            return;
        }
        
        RaceConfiguration config = _raceManager.CurrentRaceConfig;
        
        if (!config.IsValid)
        {
            Debug.LogError("Race configuration invalid");
            return;
        }
        
        Debug.Log("=== ATHLETE START POSITIONS ===");
        
        for (int lane = 1; lane <= 8; lane++)
        {
            float startDist = config.GetStartDistanceForAthlete(lane);
            Vector3 startPos = config.GetStartPositionForAthlete(lane);
            
            Debug.Log($"Lane {lane}: start={startDist:F2}m, pos={startPos}");
        }
    }
    
    [ContextMenu("Debug: Show Spline Configuration")]
    public void DebugSplineConfiguration()
    {
        if (_trackManager == null)
        {
            Debug.LogError("TrackManager not found");
            return;
        }
        
        Debug.Log("=== SPLINE CONFIGURATION ===");
        
        TrackConfiguration track = _trackManager.GetTrackForRace(RaceDistance.Distance100m);
        
        if (track != null)
        {
            Debug.Log($"Configuration Valid: {track.IsValid}");
            
            for (int distance = 0; distance < 3; distance++)
            {
                RaceDistance d = (RaceDistance)(100 + distance * 100);
                Debug.Log($"\n{d}m Race:");
                
                for (int lane = 1; lane <= 8; lane++)
                {
                    SplineContainer spline = track.GetSpline(d, lane);
                    float length = spline != null ? spline.CalculateLength() : 0f;
                    Debug.Log($"  Lane {lane}: {length:F2}m");
                }
            }
        }
    }
    
    [ContextMenu("Debug: Validate Athlete Positions")]
    public void DebugAthletePositions()
    {
        if (_raceManager == null)
        {
            Debug.LogError("RaceManager not found");
            return;
        }
        
        Athlete[] athletes = FindObjectsByType<Athlete>();
        
        Debug.Log($"=== {athletes.Length} ATHLETES IN SCENE ===");
        
        RaceConfiguration config = _raceManager.CurrentRaceConfig;
        
        if (!config.IsValid)
        {
            Debug.LogWarning("Race configuration invalid - cannot validate");
            return;
        }
        
        foreach (Athlete athlete in athletes)
        {
            float distance = athlete.CurrentDistance;
            float finishDist = config.GetFinishDistance();
            float progress = finishDist > 0 ? (distance / finishDist) * 100f : 0f;
            
            Debug.Log($"{athlete.athleteName}: {distance:F1}m/{finishDist}m ({progress:F1}%)");
        }
    }
    
    [ContextMenu("Debug: Check Coordinate Spaces")]
    public void DebugCoordinateSpaces()
    {
        if (_raceManager == null)
        {
            Debug.LogError("RaceManager not found");
            return;
        }
        
        RaceConfiguration config = _raceManager.CurrentRaceConfig;
        
        if (!config.IsValid)
        {
            Debug.LogError("Race configuration invalid");
            return;
        }
        
        Debug.Log("=== COORDINATE SPACE CHECK ===");
        
        for (int lane = 1; lane <= 3; lane++)
        {
            SplineContainer spline = config.GetSplineForAthlete(lane);
            if (spline == null) continue;
            
            Debug.Log($"\nLane {lane}:");
            Debug.Log($"  Spline Position: {spline.transform.position}");
            Debug.Log($"  Spline Rotation: {spline.transform.rotation.eulerAngles}");
            Debug.Log($"  Spline Scale: {spline.transform.lossyScale}");
            
            spline.Spline.Evaluate(0f, out Unity.Mathematics.float3 pos, out _, out _);
            Debug.Log($"  Spline Start (local): {pos}");
            
            Vector3 worldPos = spline.transform.TransformPoint((Vector3)pos);
            Debug.Log($"  Spline Start (world): {worldPos}");
        }
    }
    
    [ContextMenu("Debug: Finish Detection Status")]
    public void DebugFinishDetection()
    {
        if (_raceManager == null)
        {
            Debug.LogError("RaceManager not found");
            return;
        }
        
        Athlete[] athletes = FindObjectsByType<Athlete>();
        
        Debug.Log("=== FINISH DETECTION STATUS ===");
        Debug.Log($"Race Active: {_raceManager.IsRaceActive}");
        Debug.Log($"Race Finished: {_raceManager.IsRaceFinished}");
        Debug.Log($"Finish Distance: {_raceManager.CurrentRaceConfig.GetFinishDistance()}m");
        Debug.Log("");
        
        foreach (Athlete athlete in athletes)
        {
            bool finished = _raceManager.HasAthleteFinished(athlete);
            bool atRest = _raceManager.IsAthleteAtRest(athlete);
            int order = _raceManager.GetAthleteFinishOrder(athlete);
            
            if (finished && atRest)
            {
                Debug.Log($"{athlete.athleteName}: FINISHED & AT REST (Position: {order}, Time: {athlete.RaceTime:F2}s)");
            }
            else if (finished)
            {
                Debug.Log($"{athlete.athleteName}: FINISHED, DECELERATING (Position: {order}, Current: {athlete.CurrentDistance:F1}m)");
            }
            else
            {
                Debug.Log($"{athlete.athleteName}: RACING ({athlete.CurrentDistance:F1}m, Speed: {athlete.CurrentSpeed:F2}m/s)");
            }
        }
    }
    
    [ContextMenu("Debug: Full Architecture Audit")]
    public void DebugFullAudit()
    {
        Debug.Log("\n\n===========================");
        Debug.Log("FULL ARCHITECTURE AUDIT");
        Debug.Log("===========================\n");
        
        DebugSplineConfiguration();
        Debug.Log("");
        
        DebugRaceConfiguration();
        Debug.Log("");
        
        DebugAllLaneSplines();
        Debug.Log("");
        
        DebugAthleteStartPositions();
        Debug.Log("");
        
        DebugCoordinateSpaces();
        Debug.Log("");
        
        DebugAthletePositions();
        Debug.Log("");
        
        DebugFinishDetection();
        
        Debug.Log("\n===========================");
        Debug.Log("END AUDIT");
        Debug.Log("===========================\n");
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Architecture Debug", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Space(5);
        
        if (_raceManager != null && _raceManager.CurrentRaceConfig.IsValid)
        {
            RaceConfiguration config = _raceManager.CurrentRaceConfig;
            
            GUILayout.Label($"Race: {config.RaceDistance}m");
            GUILayout.Space(10);
            
            Athlete[] athletes = FindObjectsByType<Athlete>();
            GUILayout.Label($"Athletes: {athletes.Length}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            float finishDist = config.GetFinishDistance();
            foreach (Athlete athlete in athletes)
            {
                float progress = finishDist > 0
                    ? (athlete.CurrentDistance / finishDist) * 100f
                    : 0f;
                GUILayout.Label($"  {athlete.athleteName}: {progress:F1}%");
            }
        }
        else
        {
            GUILayout.Label("No valid race configuration", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.red } });
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Full Audit", GUILayout.Height(30)))
        {
            DebugFullAudit();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
