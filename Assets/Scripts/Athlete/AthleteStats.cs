using UnityEngine;

[CreateAssetMenu(fileName = "NewAthleteStats", menuName = "Athletics/Athlete Stats")]
public class AthleteStats : ScriptableObject
{
    [Header("Event Group")]
    public EventGroup eventGroup;

    [Header("Core Stats (1-100)")]
    [Range(1, 100)] public float acceleration = 50f;
    [Range(1, 100)] public float topSpeed = 50f;
    [Range(1, 100)] public float stamina = 50f;

    [Header("Stat Points")]
    public int totalStatPoints = 10;
    public int spentStatPoints = 0;
    public int AvailableStatPoints => totalStatPoints - spentStatPoints;

    // Converts raw stat (1-100) into a usable game multiplier
    public float GetAccelerationMultiplier() => Mathf.Lerp(0.5f, 1.5f, acceleration / 100f);
    public float GetTopSpeedMultiplier()     => Mathf.Lerp(0.5f, 1.5f, topSpeed / 100f);
    public float GetStaminaMultiplier()      => Mathf.Lerp(0.5f, 1.5f, stamina / 100f);

    public bool SpendStatPoint(StatType stat, int amount = 1)
    {
        if (AvailableStatPoints < amount) return false;

        switch (stat)
        {
            case StatType.Acceleration: acceleration = Mathf.Min(100, acceleration + amount); break;
            case StatType.TopSpeed:     topSpeed     = Mathf.Min(100, topSpeed + amount);     break;
            case StatType.Stamina:      stamina      = Mathf.Min(100, stamina + amount);      break;
        }

        spentStatPoints += amount;
        return true;
    }
}

public enum EventGroup { Sprinter, MiddleDistance }
public enum StatType   { Acceleration, TopSpeed, Stamina }