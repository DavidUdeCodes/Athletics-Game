using UnityEngine;

public enum TapDirection { Left, Right }

public abstract class ISprintInputMode : MonoBehaviour
{
    public abstract void Initialize(Athlete athlete);
    public abstract void Enable();
    public abstract void Disable();
    public abstract TapQuality GetLastQuality();
    public abstract void Reset();
    
    public abstract bool UsesDirectionalInput { get; }
    
    public virtual void OnDirectionalTap(TapDirection direction) { }
    public virtual void OnNeutralTap() { }
}
