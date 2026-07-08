using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System;

public class AthleteInput : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] [Tooltip("InputSystem actions configuration asset")]
    private InputActionAsset inputActionAsset;

    public event Action<TapQuality> OnTap;

    private InputAction _tapLeftAction;
    private InputAction _tapRightAction;
    private InputAction _tapAction;

    private ISprintInputMode _currentInputMode;
    private bool _enabled = false;
    private bool _inputAllowed = false;

    private void Awake()
    {
        var map = inputActionAsset.FindActionMap("Athlete", throwIfNotFound: true);

        _tapLeftAction = map.FindAction("TapLeft", throwIfNotFound: true);
        _tapRightAction = map.FindAction("TapRight", throwIfNotFound: true);
        _tapAction = map.FindAction("Tap", throwIfNotFound: true);
    }

    private void OnEnable()
    {
        _tapLeftAction.performed += OnTapLeftPerformed;
        _tapRightAction.performed += OnTapRightPerformed;
        _tapAction.performed += OnTapPerformed;
    }

    private void OnDisable()
    {
        _tapLeftAction.performed -= OnTapLeftPerformed;
        _tapRightAction.performed -= OnTapRightPerformed;
        _tapAction.performed -= OnTapPerformed;
    }

    public void SetEnabled(bool value)
    {
        _enabled = value;
    }

    public void AllowInput(bool value)
    {
        _inputAllowed = value;
    }

    public void SetInputMode(ISprintInputMode inputMode)
    {
        _currentInputMode = inputMode;
        UpdateActionStates();
    }

    public ISprintInputMode GetCurrentMode() => _currentInputMode;

    private void Update()
    {
        if (!_enabled) return;
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        foreach (var touch in touchscreen.touches)
        {
            if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began) continue;

            if (_currentInputMode == null) continue;

            if (_currentInputMode.UsesDirectionalInput)
            {
                float x = touch.position.ReadValue().x;
                TapDirection direction = x < Screen.width * 0.5f ? TapDirection.Left : TapDirection.Right;
                ProcessDirectionalTap(direction);
            }
            else
            {
                ProcessNeutralTap();
            }
        }
    }

    private void OnTapLeftPerformed(InputAction.CallbackContext context)
    {
        if (!_enabled || _currentInputMode == null) return;
        ProcessDirectionalTap(TapDirection.Left);
    }

    private void OnTapRightPerformed(InputAction.CallbackContext context)
    {
        if (!_enabled || _currentInputMode == null) return;
        ProcessDirectionalTap(TapDirection.Right);
    }

    private void OnTapPerformed(InputAction.CallbackContext context)
    {
        if (!_enabled || _currentInputMode == null) return;
        ProcessNeutralTap();
    }

    private void ProcessDirectionalTap(TapDirection direction)
    {
        if (_currentInputMode == null) return;

        _currentInputMode.OnDirectionalTap(direction);
        TapQuality quality = _currentInputMode.GetLastQuality();
        
        if (_inputAllowed)
        {
            OnTap?.Invoke(quality);
        }
    }

    private void ProcessNeutralTap()
    {
        if (_currentInputMode == null) return;

        _currentInputMode.OnNeutralTap();
        TapQuality quality = _currentInputMode.GetLastQuality();
        
        if (_inputAllowed)
        {
            OnTap?.Invoke(quality);
        }
    }

    private void UpdateActionStates()
    {
        if (_currentInputMode == null) return;

        if (_currentInputMode.UsesDirectionalInput)
        {
            _tapLeftAction.Enable();
            _tapRightAction.Enable();
            _tapAction.Disable();
        }
        else
        {
            _tapLeftAction.Disable();
            _tapRightAction.Disable();
            _tapAction.Enable();
        }
    }
}