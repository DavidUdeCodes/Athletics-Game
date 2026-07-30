using UnityEngine;

/// <summary>
/// Defines a single AI runner's identity and performance target.
/// Adjust targetFinishTime to control difficulty — no other code changes needed.
/// Add fields here as customisation and athlete progression are introduced later.
/// </summary>
[CreateAssetMenu(fileName = "AIAthleteProfile", menuName = "Athletics/AI Athlete Profile")]
public class AIAthleteProfile : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in race results")]
    public string athleteName = "CPU Athlete";
    [Tooltip("Nationality shown in race results")]
    public string nationality = "Unknown";

    [Header("Performance")]
    [Tooltip("Target finish time in seconds. Lower = faster. Examples: 10.82 (elite), 11.05, 11.43 (easy).")]
    [Min(5f)]
    public float targetFinishTime = 10.82f;
}
