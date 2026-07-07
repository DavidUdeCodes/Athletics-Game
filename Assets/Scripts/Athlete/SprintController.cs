using UnityEngine;

public enum TapQuality { Perfect, Good, Fair, Miss }

public class SprintController : MonoBehaviour
{
    [Header("Speed Configuration")]
    [SerializeField] [Tooltip("Base maximum speed in units/second")]
    private float baseTopSpeed = 10f;

    [Space]
    [Header("Stat Multipliers")]
    private float _topSpeedMult = 1f;

    private float _maxSpeed = 0f;
    private bool _isActive = false;

    private MomentumController _momentumController;

    public float CurrentSpeed => _momentumController != null ? _momentumController.CurrentSpeed : 0f;
    public float MaxSpeed => _maxSpeed;
    public bool IsAtMaxSpeed => CurrentSpeed >= (_maxSpeed * 0.99f);

    private void Update()
    {
        if (!_isActive || _momentumController == null) return;

        _momentumController.Tick(Time.deltaTime);
    }

    public void SetStatMultipliers(float topSpeedMult, float accelerationMult)
    {
        _topSpeedMult = topSpeedMult;
        _maxSpeed = baseTopSpeed * _topSpeedMult;
    }

    public void SetMomentumController(MomentumController momentumController)
    {
        _momentumController = momentumController;
        if (_momentumController != null)
        {
            _momentumController.Initialize(_maxSpeed, _topSpeedMult);
        }
    }

    public void StartSprinting()
    {
        _isActive = true;
        if (_momentumController != null)
        {
            _momentumController.Reset();
        }
    }

    public void StopSprinting()
    {
        _isActive = false;
    }
}
