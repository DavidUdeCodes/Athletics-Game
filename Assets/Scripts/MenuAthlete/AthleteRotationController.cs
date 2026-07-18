using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Presentation-only. Rotates a pivot Transform in response to horizontal
// click/touch drag. Deliberately has no knowledge of RaceManager, sprint
// input modes, athlete movement/momentum, or any other gameplay system -
// it only ever touches the Transform it's told to rotate.
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

    [Tooltip("Maximum rotation speed in degrees/second, enforced both while dragging and during inertia.")]
    [SerializeField] private float maxAngularVelocity = 360f;

    [Header("Inertia")]
    [Tooltip("If enabled, the pivot keeps spinning briefly after release and smoothly damps to a stop.")]
    [SerializeField] private bool useInertia = true;

    [Tooltip("Higher values bring the spin to a stop more quickly.")]
    [SerializeField] private float inertiaDamping = 2.5f;

    [Tooltip("Angular velocity (degrees/second) below which inertia is considered stopped.")]
    [SerializeField] private float stopThreshold = 1f;

    private float _angularVelocity;
    private bool _isDragging;
    private bool _draggingWithTouch;
    private int _activeTouchId;
    private Vector2 _lastPointerPosition;

    private void Awake()
    {
        if (rotationPivot == null)
            rotationPivot = transform;
    }

    private void Update()
    {
        if (_isDragging)
            ContinueDrag();
        else if (!TryBeginDrag())
            ApplyInertia();
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
    }

    private void ContinueDrag()
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

        float dt = Time.deltaTime > 0f ? Time.deltaTime : Mathf.Epsilon;

        float rotationDelta = delta.x * dragSensitivity;
        if (invertHorizontal)
            rotationDelta = -rotationDelta;

        _angularVelocity = Mathf.Clamp(rotationDelta / dt, -maxAngularVelocity, maxAngularVelocity);

        float clampedRotation = Mathf.Clamp(rotationDelta, -maxAngularVelocity * dt, maxAngularVelocity * dt);
        Rotate(clampedRotation);
    }

    private void EndDrag()
    {
        _isDragging = false;
    }

    private void ApplyInertia()
    {
        if (!useInertia || Mathf.Abs(_angularVelocity) <= stopThreshold)
        {
            _angularVelocity = 0f;
            return;
        }

        Rotate(_angularVelocity * Time.deltaTime);

        float decay = 1f - Mathf.Exp(-inertiaDamping * Time.deltaTime);
        _angularVelocity = Mathf.Lerp(_angularVelocity, 0f, decay);
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