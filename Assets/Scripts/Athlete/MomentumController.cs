using UnityEngine;

public class MomentumController : MonoBehaviour
{
    [Header("Momentum Gains")]
    [SerializeField] [Tooltip("Momentum gained from a Perfect tap")]
    private float perfectMomentumGain = 0.3f;
    [SerializeField] [Tooltip("Momentum gained from a Good tap")]
    private float goodMomentumGain = 0.2f;
    [SerializeField] [Tooltip("Momentum gained from a Fair tap")]
    private float fairMomentumGain = 0.1f;
    [SerializeField] [Tooltip("Momentum penalty for a Miss")]
    private float missMomentumPenalty = -0.15f;

    [Space]
    [Header("Momentum Decay")]
    [SerializeField] [Tooltip("Rate at which momentum decays over time per second")]
    private float baseDecayRate = 0.5f;

    [Space]
    [Header("Momentum to Speed Conversion")]
    [SerializeField] [Tooltip("Curve mapping momentum (0-1) to speed multiplier")]
    private AnimationCurve momentumToSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _currentMomentum = 0f;
    private float _maxSpeed = 10f;
    private float _topSpeedMultiplier = 1f;

    public float CurrentMomentum => _currentMomentum;
    public float CurrentSpeed => GetSpeedFromMomentum();

    public void Initialize(float maxSpeed, float topSpeedMultiplier)
    {
        _maxSpeed = maxSpeed;
        _topSpeedMultiplier = topSpeedMultiplier;
        _currentMomentum = 0f;
    }

    public void ApplyQuality(TapQuality quality)
    {
        switch (quality)
        {
            case TapQuality.Perfect:
                _currentMomentum += perfectMomentumGain;
                break;
            case TapQuality.Good:
                _currentMomentum += goodMomentumGain;
                break;
            case TapQuality.Fair:
                _currentMomentum += fairMomentumGain;
                break;
            case TapQuality.Miss:
                _currentMomentum += missMomentumPenalty;
                break;
        }

        _currentMomentum = Mathf.Clamp01(_currentMomentum);
    }

    public void ApplyStartingBonus(float bonus)
    {
        _currentMomentum += bonus;
        _currentMomentum = Mathf.Clamp01(_currentMomentum);
    }

    public void Tick(float deltaTime)
    {
        _currentMomentum -= baseDecayRate * deltaTime;
        _currentMomentum = Mathf.Clamp01(_currentMomentum);
    }

    public float GetSpeedFromMomentum()
    {
        float curveValue = momentumToSpeedCurve.Evaluate(_currentMomentum);
        return _maxSpeed * curveValue * _topSpeedMultiplier;
    }

    public void Reset()
    {
        _currentMomentum = 0f;
    }
}
