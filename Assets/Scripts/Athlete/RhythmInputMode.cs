using UnityEngine;

public class RhythmInputMode : ISprintInputMode
{
    [Header("Rhythm Settings")]
    [SerializeField] [Tooltip("Base rhythm speed when no input detected")]
    private float baseRhythmSpeed = 1f;
    [SerializeField] [Tooltip("How much athlete speed affects rhythm speed")]
    private float rhythmSpeedMultiplier = 0.5f;
    [SerializeField] [Tooltip("Minimum rhythm speed limit")]
    private float minRhythmSpeed = 0.5f;
    [SerializeField] [Tooltip("Maximum rhythm speed limit")]
    private float maxRhythmSpeed = 2.5f;

    [Space]
    [Header("Tap Zone Configuration")]
    [SerializeField] [Tooltip("Left zone minimum position (0 = leftmost)")]
    private float leftZoneMin = 0.0f;
    [SerializeField] [Tooltip("Left zone maximum position")]
    private float leftZoneMax = 0.2f;
    [SerializeField] [Tooltip("Right zone minimum position")]
    private float rightZoneMin = 0.8f;
    [SerializeField] [Tooltip("Right zone maximum position (1 = rightmost)")]
    private float rightZoneMax = 1.0f;

    [Space]
    [Header("Zone Judgment Sizes")]
    [SerializeField] [Tooltip("Perfect zone radius around tap point")]
    private float perfectZoneSize = 0.05f;
    [SerializeField] [Tooltip("Good zone radius around tap point")]
    private float goodZoneSize = 0.10f;
    [SerializeField] [Tooltip("Fair zone radius around tap point")]
    private float fairZoneSize = 0.15f;

    private float _markerPosition = 0f;
    private float _markerDirection = 1f;
    private float _currentRhythmSpeed = 1f;
    private bool _isAnimating = false;
    private bool _isWaitingForFirstInput = false;
    private bool _firstInputReceived = false;
    private TapQuality _lastQuality = TapQuality.Miss;
    private TapDirection _lastTapDirection = TapDirection.Left;
    private Athlete _athlete;
    private bool _isInGetSetState = false;
    private bool _inputAllowedForReaction = false;

    public float MarkerPosition => _markerPosition;
    public float CurrentRhythmSpeed => _currentRhythmSpeed;
    public bool IsAnimating => _isAnimating;

    public float LeftZoneMin => leftZoneMin;
    public float LeftZoneMax => leftZoneMax;
    public float RightZoneMin => rightZoneMin;
    public float RightZoneMax => rightZoneMax;

    public float PerfectZoneSize => perfectZoneSize;
    public float GoodZoneSize => goodZoneSize;
    public float FairZoneSize => fairZoneSize;

    public override bool UsesDirectionalInput => true;

    public override void Initialize(Athlete athlete)
    {
        _athlete = athlete;
        PrepareForFirstInput();
    }

    public override void Enable()
    {
        enabled = true;
    }

    public override void Disable()
    {
        enabled = false;
        StopRhythm();
    }

    public override TapQuality GetLastQuality() => _lastQuality;

    public override void Reset()
    {
        _lastQuality = TapQuality.Miss;
        _lastTapDirection = TapDirection.Left;
        _firstInputReceived = false;
        _isInGetSetState = false;
        _inputAllowedForReaction = false;
        PrepareForFirstInput();
    }

    public override void EnterGetSetState()
    {
        _isInGetSetState = true;
        _firstInputReceived = false;
        _inputAllowedForReaction = false;
    }

    public override void ExitGetSetState()
    {
        _isInGetSetState = false;
        _inputAllowedForReaction = true;
    }

    public override void EnterRunningState()
    {
        _isInGetSetState = false;
        _inputAllowedForReaction = true;
        _firstInputReceived = false;
        PrepareForFirstInput();
    }

    public void PrepareForFirstInput()
    {
        _isAnimating = false;
        _isWaitingForFirstInput = true;
        _markerPosition = 0f;
        _markerDirection = 1f;
        _currentRhythmSpeed = baseRhythmSpeed;
    }

    public void StartRhythm()
    {
        _isAnimating = true;
        _isWaitingForFirstInput = false;
    }

    public void StopRhythm()
    {
        _isAnimating = false;
    }

    public override void OnDirectionalTap(TapDirection direction)
    {
        if (_isInGetSetState)
        {
            RaiseFalseStartDetected();
            return;
        }

        if (!_inputAllowedForReaction)
        {
            return;
        }

        if (direction == _lastTapDirection)
        {
            _lastQuality = TapQuality.Miss;
            _lastTapDirection = direction;
            return;
        }

        _lastQuality = EvaluateTap(direction);
        _lastTapDirection = direction;
        
        if (!_firstInputReceived && direction == TapDirection.Left)
        {
            _firstInputReceived = true;
            StartRhythm();
        }
    }

    public void UpdateRhythmSpeed(float playerSpeed)
    {
        if (!_isWaitingForFirstInput && !_isAnimating) return;

        _currentRhythmSpeed = Mathf.Clamp(
            baseRhythmSpeed + (playerSpeed * rhythmSpeedMultiplier),
            minRhythmSpeed,
            maxRhythmSpeed
        );
    }

    public TapQuality EvaluateTap(TapDirection direction)
    {
        float zoneCenter = (direction == TapDirection.Left) ? 0f : 1f;
        float distanceFromCenter = Mathf.Abs(_markerPosition - zoneCenter);

        if (distanceFromCenter <= perfectZoneSize)
            return TapQuality.Perfect;

        if (distanceFromCenter <= goodZoneSize)
            return TapQuality.Good;

        if (distanceFromCenter <= fairZoneSize)
            return TapQuality.Fair;

        return TapQuality.Miss;
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _markerPosition += _markerDirection * _currentRhythmSpeed * Time.deltaTime;

        if (_markerPosition >= 1f)
        {
            _markerPosition = 1f;
            _markerDirection = -1f;
        }
        else if (_markerPosition <= 0f)
        {
            _markerPosition = 0f;
            _markerDirection = 1f;
        }
    }
}
