using UnityEngine;

/// <summary>
/// Drives an AI athlete's movement speed based on a target finish time and a
/// configurable pacing curve. Attach this to an AI athlete prefab alongside
/// AthleteMovement and SplineMovement. SprintController / MomentumController
/// are not required and should not be present on the AI prefab.
///
/// Speed is computed so that — accounting for the pacing curve shape — the
/// athlete finishes in approximately targetFinishTime seconds. A small random
/// variation is applied each race to prevent all AI runners looking identical.
/// </summary>
public class AISprinterController : MonoBehaviour
{
    [Header("AI Timing")]
    [SerializeField]
    [Tooltip("Target finish time in seconds. Set at runtime by RaceManager via SetTargetFinishTime.")]
    private float _targetFinishTime = 10.82f;

    [Header("Pacing")]
    [SerializeField]
    [Tooltip("Maps normalised race progress (0 = start, 1 = finish) to a speed multiplier. " +
             "The curve integral is automatically accounted for, so its average need not equal 1 — " +
             "just shape it to taste. Default: slow acceleration, full speed by ~15%, slight kick at finish.")]
    private AnimationCurve _pacingCurve = new AnimationCurve(
        new Keyframe(0f,    0.82f, 0f,   2.0f),
        new Keyframe(0.15f, 1.00f, 0.5f, 0f),
        new Keyframe(0.88f, 1.00f, 0f,   0.3f),
        new Keyframe(1f,    1.02f, 0.5f, 0f)
    );

    [SerializeField]
    [Range(0f, 0.05f)]
    [Tooltip("Maximum fractional variation applied to the target finish time at the start of each race " +
             "(e.g. 0.025 = ±2.5%). Keeps repeat races feeling slightly different.")]
    private float _raceVariation = 0.025f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private float _raceDistance;
    private float _effectiveTargetTime;
    private float _adjustedAvgSpeed;
    private float _peakSpeed;
    private bool  _isSprinting;

    private SplineMovement _splineMovement;

    // ── Public reads ──────────────────────────────────────────────────────────
    /// <summary>Speed in m/s this frame (0 when not sprinting).</summary>
    public float CurrentSpeed   { get; private set; }

    /// <summary>0-1 normalised speed for driving the Animator blend tree.</summary>
    public float NormalizedSpeed { get; private set; }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _splineMovement = GetComponent<SplineMovement>();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set the target finish time for this runner. Called by RaceManager at spawn time
    /// based on the current difficulty and RaceDifficultyConfig.
    /// </summary>
    public void SetTargetFinishTime(float time)
    {
        _targetFinishTime = Mathf.Max(1f, time);
    }

    /// <summary>
    /// Must be called before StartSprinting. Provides the race distance so the
    /// controller can compute the correct average speed.
    /// </summary>
    public void Initialize(float raceDistance)
    {
        _raceDistance = raceDistance;
    }

    /// <summary>
    /// Begin producing speed each Update. Call once per race, after Initialize.
    /// Applies a random per-race variation to the target time.
    /// </summary>
    public void StartSprinting()
    {
        if (_targetFinishTime <= 0f)
        {
            Debug.LogWarning($"[AISprinterController] Target finish time is not set on {gameObject.name}. " +
                             "Call SetTargetFinishTime() before StartSprinting().");
            return;
        }

        float variation        = Random.Range(-_raceVariation, _raceVariation);
        _effectiveTargetTime   = _targetFinishTime * (1f + variation);

        float curveAvg         = ComputeCurveAverage(_pacingCurve);
        _adjustedAvgSpeed      = curveAvg > 0f
                                     ? (_raceDistance / _effectiveTargetTime) / curveAvg
                                     : 0f;

        // Pre-compute the peak speed (used to normalise the animation blend value).
        // Use the curve value at 50% progress as an approximation of "full pace".
        _peakSpeed = _adjustedAvgSpeed * _pacingCurve.Evaluate(0.5f);

        _isSprinting   = true;
        CurrentSpeed   = 0f;
        NormalizedSpeed = 0f;
    }

    /// <summary>Stop producing speed. Called on race finish or early termination.</summary>
    public void StopSprinting()
    {
        _isSprinting    = false;
        CurrentSpeed    = 0f;
        NormalizedSpeed = 0f;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isSprinting || _raceDistance <= 0f || _splineMovement == null)
            return;

        float progress    = Mathf.Clamp01(_splineMovement.DistanceTravelled / _raceDistance);
        float multiplier  = _pacingCurve.Evaluate(progress);
        CurrentSpeed      = _adjustedAvgSpeed * multiplier;

        NormalizedSpeed = _peakSpeed > 0f
                              ? Mathf.Clamp01(CurrentSpeed / _peakSpeed)
                              : 0f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Numerically integrates the curve to find its average value over [0,1].
    /// Used to scale avgSpeed so the pacing shape does not affect total race time.
    /// </summary>
    private static float ComputeCurveAverage(AnimationCurve curve, int samples = 64)
    {
        if (curve == null || curve.keys.Length == 0)
            return 1f;

        float sum = 0f;
        for (int i = 0; i < samples; i++)
            sum += curve.Evaluate(i / (float)(samples - 1));

        return sum / samples;
    }
}
