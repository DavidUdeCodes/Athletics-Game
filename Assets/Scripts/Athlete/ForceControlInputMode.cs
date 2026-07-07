using UnityEngine;

public class ForceControlInputMode : ISprintInputMode
{
    [Header("Gravity Settings")]
    [SerializeField] [Tooltip("Base gravity magnitude pulling marker downward")]
    private float baseGravity = 1.5f;
    [SerializeField] [Tooltip("Gravity scaling factor based on athlete speed")]
    private float gravitySpeedScaling = 0.3f;

    [Header("Impulse Settings")]
    [SerializeField] [Tooltip("Upward impulse strength curve based on marker position (0=bottom, 1=top)")]
    private AnimationCurve impulseStrengthCurve = AnimationCurve.EaseInOut(0, 0.8f, 1, 0.2f);

    [Header("Target Zone Settings")]
    [SerializeField] [Tooltip("Base target zone size (0-1 range)")]
    private float baseTargetZoneSize = 0.2f;
    [SerializeField] [Tooltip("Rate at which target zone shrinks as speed increases")]
    private float targetShrinkingRate = 0.05f;
    [SerializeField] [Tooltip("Minimum target zone size")]
    private float minTargetZoneSize = 0.08f;
    [SerializeField] [Tooltip("Maximum target zone size")]
    private float maxTargetZoneSize = 0.3f;
    [SerializeField] [Tooltip("Center position of target zone (0.75 = upper area)")]
    private float baseTargetZoneCenter = 0.75f;

    [Header("Judgment Zone Sizes")]
    [SerializeField] [Tooltip("Perfect zone radius around target")]
    private float perfectZoneSize = 0.05f;
    [SerializeField] [Tooltip("Good zone radius around target")]
    private float goodZoneSize = 0.10f;
    [SerializeField] [Tooltip("Fair zone radius around target")]
    private float fairZoneSize = 0.15f;
    [SerializeField] [Tooltip("External zone beyond fair for additional fair judgement")]
    private float externalFairZoneSize = 0.05f;

    private float _markerPosition = 0.5f;
    private float _markerVelocity = 0f;
    private float _currentGravity = 0f;
    private float _currentTargetZoneSize = 0f;
    private float _targetZoneCenter = 0.75f;

    private TapQuality _lastQuality = TapQuality.Miss;
    private Athlete _athlete;

    public float MarkerPosition => _markerPosition;
    public float TargetZoneCenterPosition => _targetZoneCenter;
    public float TargetZoneSize => _currentTargetZoneSize;

    public override bool UsesDirectionalInput => false;

    public override void Initialize(Athlete athlete)
    {
        _athlete = athlete;
        Reset();
    }

    public override void Enable()
    {
        enabled = true;
    }

    public override void Disable()
    {
        enabled = false;
    }

    public override void OnNeutralTap()
    {
        float impulseStrength = impulseStrengthCurve.Evaluate(_markerPosition);
        _markerVelocity = impulseStrength;

        _lastQuality = EvaluateQuality();
    }

    public override TapQuality GetLastQuality() => _lastQuality;

    public override void Reset()
    {
        _markerPosition = 0.5f;
        _markerVelocity = 0f;
        _currentTargetZoneSize = baseTargetZoneSize;
        _targetZoneCenter = baseTargetZoneCenter;
        _lastQuality = TapQuality.Miss;
    }

    private void Update()
    {
        if (!enabled) return;

        UpdateGravity();
        UpdateMarkerPosition();
        UpdateTargetZone();
    }

    private void UpdateGravity()
    {
        float currentSpeed = _athlete != null ? _athlete.GetCurrentSpeed() : 0f;
        _currentGravity = baseGravity + (currentSpeed * gravitySpeedScaling);
    }

    private void UpdateMarkerPosition()
    {
        _markerVelocity -= _currentGravity * Time.deltaTime;
        _markerPosition += _markerVelocity * Time.deltaTime;

        _markerPosition = Mathf.Clamp01(_markerPosition);

        if (_markerPosition <= 0f)
        {
            _markerVelocity = Mathf.Max(_markerVelocity, 0f);
        }
        else if (_markerPosition >= 1f)
        {
            _markerVelocity = Mathf.Min(_markerVelocity, 0f);
        }
    }

    private void UpdateTargetZone()
    {
        float currentSpeed = _athlete != null ? _athlete.GetCurrentSpeed() : 0f;
        _currentTargetZoneSize = baseTargetZoneSize - (currentSpeed * targetShrinkingRate);
        _currentTargetZoneSize = Mathf.Clamp(_currentTargetZoneSize, minTargetZoneSize, maxTargetZoneSize);
    }

    private TapQuality EvaluateQuality()
    {
        float distanceFromCenter = Mathf.Abs(_markerPosition - _targetZoneCenter);

        float scaledPerfectZone = perfectZoneSize * (_currentTargetZoneSize / baseTargetZoneSize);
        float scaledGoodZone = goodZoneSize * (_currentTargetZoneSize / baseTargetZoneSize);
        float scaledFairZone = fairZoneSize * (_currentTargetZoneSize / baseTargetZoneSize);
        float scaledExternalFairZone = externalFairZoneSize * (_currentTargetZoneSize / baseTargetZoneSize);

        if (distanceFromCenter <= scaledPerfectZone)
            return TapQuality.Perfect;

        if (distanceFromCenter <= scaledGoodZone)
            return TapQuality.Good;

        if (distanceFromCenter <= scaledFairZone)
            return TapQuality.Fair;

        if (distanceFromCenter <= (scaledFairZone + scaledExternalFairZone))
            return TapQuality.Fair;

        return TapQuality.Miss;
    }
}
