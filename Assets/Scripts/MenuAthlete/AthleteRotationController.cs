using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class AthleteRotationController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Pivot transform to rotate. Rotate a parent pivot rather than the mesh directly so the model underneath can be swapped freely. Defaults to this transform if unassigned.")]
    [SerializeField] private Transform rotationPivot;

    [Header("Drag Sensitivity")]
    [Tooltip("Degrees rotated per pixel of horizontal pointer movement.")]
    [SerializeField] private float dragSensitivity = 0.25f;

    [Tooltip("Invert the horizontal drag direction.")]
    [SerializeField] private bool invertHorizontal = false;

    [Tooltip("Maximum rotation speed in degrees/second while actively dragging.")]
    [SerializeField] private float maxAngularVelocity = 360f;

    [Header("Inertia")]
    [Tooltip("If enabled, the pivot keeps spinning briefly after release and smoothly damps to a stop.")]
    [SerializeField] private bool useInertia = true;

    [Tooltip("Higher values bring the spin to a stop more quickly.")]
    [SerializeField] private float inertiaDamping = 2.5f;

    [Tooltip("Angular velocity (degrees/second) below which inertia is considered stopped.")]
    [SerializeField] private float stopThreshold = 1f;

    [Header("Flick Feel")]
    [Tooltip("How far back (in seconds) to look when measuring release speed. Smaller = snappier but more sensitive to noise, larger = smoother but less responsive to a sudden flick at the very end of the drag.")]
    [SerializeField] private float velocitySampleWindow = 0.12f;

    [Tooltip("Multiplier applied to the measured release velocity. >1 makes flicks feel punchier than the raw drag speed suggested.")]
    [SerializeField] private float flickBoost = 1.15f;

    [Tooltip("Absolute cap on release velocity, applied after the flick boost. Set >= Max Angular Velocity to let flicks exceed normal drag speed.")]
    [SerializeField] private float maxFlickAngularVelocity = 540f;

    [Header("Idle Life (optional)")]
    [Tooltip("If enabled, the character gently keeps spinning on its own after being at rest for a while, so the menu doesn't feel static.")]
    [SerializeField] private bool useIdleSpin = false;

    [Tooltip("Seconds of no interaction and no residual inertia before idle spin kicks in.")]
    [SerializeField] private float idleSpinDelay = 4f;

    [Tooltip("Idle spin speed in degrees/second.")]
    [SerializeField] private float idleSpinSpeed = 12f;

    [Tooltip("How quickly idle spin ramps in/out, in degrees/second^2-ish terms (higher = snappier ramp).")]
    [SerializeField] private float idleSpinRampSpeed = 8f;

    private float _angularVelocity;
    private float _idleSpinCurrent;
    private float _timeSinceLastInteraction;

    private bool _isDragging;
    private bool _draggingWithTouch;
    private int _activeTouchId;
    private Vector2 _lastPointerPosition;

    // Rolling buffer of recent (timestamp, rotationDeltaApplied) samples, used to
    // measure release velocity over a short window instead of a single noisy frame.
    private readonly List<(float time, float delta)> _velocitySamples = new List<(float, float)>();

    private void Awake()
    {
        if (rotationPivot == null)
            rotationPivot = transform;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : Mathf.Epsilon;

        if (_isDragging)
        {
            ContinueDrag(dt);
        }
        else if (!TryBeginDrag())
        {
            ApplyInertia(dt);
            ApplyIdleSpin(dt);
        }
    }

    private bool TryBeginDrag()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            if (IsPointerOverUI(-1)) return false;

            BeginDrag(pos, isTouch: false, touchId: -1);
            return true;
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                Vector2 pos = touch.position.ReadValue();
                int touchId = touch.touchId.ReadValue();
                if (IsPointerOverUI(touchId)) return false;

                BeginDrag(pos, isTouch: true, touchId: touchId);
                return true;
            }
        }

        return false;
    }

    private void BeginDrag(Vector2 startPosition, bool isTouch, int touchId)
    {
        _isDragging = true;
        _draggingWithTouch = isTouch;
        _activeTouchId = touchId;
        _lastPointerPosition = startPosition;
        _angularVelocity = 0f;
        _idleSpinCurrent = 0f;
        _timeSinceLastInteraction = 0f;

        _velocitySamples.Clear();
        _velocitySamples.Add((Time.unscaledTime, 0f));
    }

    private void ContinueDrag(float dt)
    {
        Vector2 currentPosition;
        bool stillDown;

        if (_draggingWithTouch)
        {
            if (Touchscreen.current == null)
            {
                EndDrag();
                return;
            }

            var touch = Touchscreen.current.primaryTouch;
            stillDown = touch.press.isPressed && touch.touchId.ReadValue() == _activeTouchId;
            currentPosition = touch.position.ReadValue();
        }
        else
        {
            stillDown = Mouse.current != null && Mouse.current.leftButton.isPressed;
            currentPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : _lastPointerPosition;
        }

        if (!stillDown)
        {
            EndDrag();
            return;
        }

        Vector2 delta = currentPosition - _lastPointerPosition;
        _lastPointerPosition = currentPosition;

        float rotationDelta = delta.x * dragSensitivity;
        if (invertHorizontal)
            rotationDelta = -rotationDelta;

        // Follow the pointer 1:1 while dragging (clamped so an enormous single-frame
        // jump - e.g. a frame hitch - can't snap the model instantly).
        float clampedRotation = Mathf.Clamp(rotationDelta, -maxAngularVelocity * dt, maxAngularVelocity * dt);
        Rotate(clampedRotation);

        RecordVelocitySample(clampedRotation);
    }

    private void EndDrag()
    {
        _isDragging = false;
        _timeSinceLastInteraction = 0f;
        _angularVelocity = ComputeReleaseVelocity();
    }

    /// <summary>
    /// Adds the most recent applied-rotation sample and trims samples older than
    /// velocitySampleWindow, keeping the buffer small and recent.
    /// </summary>
    private void RecordVelocitySample(float appliedDelta)
    {
        float now = Time.unscaledTime;
        _velocitySamples.Add((now, appliedDelta));

        while (_velocitySamples.Count > 1 && now - _velocitySamples[0].time > velocitySampleWindow)
            _velocitySamples.RemoveAt(0);
    }

    /// <summary>
    /// Measures how fast the pivot was actually spinning over the last
    /// velocitySampleWindow seconds (not just the final frame), then applies the
    /// flick boost and caps. This is what makes a real flick feel snappy while
    /// a slow, careful drag-then-stop doesn't launch into an unwanted spin.
    /// </summary>
    private float ComputeReleaseVelocity()
    {
        if (_velocitySamples.Count < 2)
            return 0f;

        float totalDelta = 0f;
        for (int i = 1; i < _velocitySamples.Count; i++)
            totalDelta += _velocitySamples[i].delta;

        float timeSpan = _velocitySamples[^1].time - _velocitySamples[0].time;
        if (timeSpan <= Mathf.Epsilon)
            return 0f;

        float releaseVelocity = (totalDelta / timeSpan) * flickBoost;
        return Mathf.Clamp(releaseVelocity, -maxFlickAngularVelocity, maxFlickAngularVelocity);
    }

    private void ApplyInertia(float dt)
    {
        if (!useInertia || Mathf.Abs(_angularVelocity) <= stopThreshold)
        {
            _angularVelocity = 0f;
            _timeSinceLastInteraction += dt;
            return;
        }

        Rotate(_angularVelocity * dt);

        float decay = 1f - Mathf.Exp(-inertiaDamping * dt);
        _angularVelocity = Mathf.Lerp(_angularVelocity, 0f, decay);

        _timeSinceLastInteraction = 0f;
    }

    /// <summary>
    /// Gentle ambient spin that ramps in once the character has been sitting
    /// still (no drag, no residual inertia) for idleSpinDelay seconds, and ramps
    /// back out the moment the user grabs it again. Purely cosmetic "keep the
    /// menu feeling alive" flourish - safe to leave disabled.
    /// </summary>
    private void ApplyIdleSpin(float dt)
    {
        if (!useIdleSpin)
            return;

        bool shouldIdleSpin = _timeSinceLastInteraction >= idleSpinDelay;
        float target = shouldIdleSpin ? idleSpinSpeed : 0f;

        _idleSpinCurrent = Mathf.MoveTowards(_idleSpinCurrent, target, idleSpinRampSpeed * dt);

        if (Mathf.Abs(_idleSpinCurrent) > 0.001f)
            Rotate(_idleSpinCurrent * dt);
    }

    private void Rotate(float degrees)
    {
        rotationPivot.Rotate(Vector3.up, degrees, Space.World);
    }

    private bool IsPointerOverUI(int touchId)
    {
        if (EventSystem.current == null) return false;

        return touchId >= 0
            ? EventSystem.current.IsPointerOverGameObject(touchId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}