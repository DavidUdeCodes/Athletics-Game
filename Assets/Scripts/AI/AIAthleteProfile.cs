using UnityEngine;

/// <summary>
/// Defines a single AI runner's identity.
/// Performance (target finish time) is now driven by RaceDifficultyConfig assigned to
/// RaceManager — no per-athlete finish time is needed here.
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
}
