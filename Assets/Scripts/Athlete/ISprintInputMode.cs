using UnityEngine;
using System;

public enum TapDirection { Left, Right }

public abstract class ISprintInputMode : MonoBehaviour
{
    public event Action OnFalseStartDetected;

    public abstract void Initialize(Athlete athlete);
    public abstract void Enable();
    public abstract void Disable();
    public abstract TapQuality GetLastQuality();
    public abstract void Reset();
    
    public abstract bool UsesDirectionalInput { get; }
    
    public virtual void OnDirectionalTap(TapDirection direction) { }
    public virtual void OnNeutralTap() { }
    
    public virtual void EnterGetSetState() { }
    public virtual void ExitGetSetState() { }
    public virtual void EnterRunningState() { }
    
    protected void RaiseFalseStartDetected()
    {
        OnFalseStartDetected?.Invoke();
    }
}
