using UnityEngine;

// Add new emotes here as content grows - no new Animator triggers needed per-emote.
public enum EmoteType
{
    Celebrate
}

public class AthleteAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static class AnimParamIDs
    {
        public static readonly int RaceState = Animator.StringToHash("RaceState");
        public static readonly int NormalizedSpeed = Animator.StringToHash("NormalizedSpeed");
        public static readonly int FinishDip = Animator.StringToHash("FinishDip");
        public static readonly int Emote = Animator.StringToHash("Emote");
        public static readonly int EmoteID = Animator.StringToHash("EmoteID");
        public static readonly int FlagHold = Animator.StringToHash("FlagHold");
    }

    // Single source of truth. Other systems (Athlete, UI, etc.) should read
    // these instead of caching their own copy of "what animation state are we in".
    public RaceStartState CurrentRaceState { get; private set; } = RaceStartState.Idle;
    public bool IsHoldingFlag { get; private set; }

    // Fired by Animation Events placed on the actual clips (FinishDip, Emote).
    // Lets gameplay code react to when a one-shot animation *actually* finishes
    // instead of assuming it completes instantly.
    public event System.Action OnFinishDipComplete;
    public event System.Action OnEmoteComplete;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogError($"No Animator found on {gameObject.name}. AthleteAnimationController cannot function.", this);
    }

    public void SetRaceState(RaceStartState state)
    {
        if (CurrentRaceState == state) return;
        CurrentRaceState = state;

        if (animator == null) return;
        animator.SetInteger(AnimParamIDs.RaceState, (int)state);
    }

    public void SetNormalizedSpeed(float normalizedSpeed)
    {
        if (animator == null) return;
        animator.SetFloat(AnimParamIDs.NormalizedSpeed, Mathf.Clamp01(normalizedSpeed));
    }

    public void PlayFinishDip()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimParamIDs.FinishDip);
    }

    public void PlayEmote(EmoteType emote)
    {
        if (animator == null) return;
        animator.SetInteger(AnimParamIDs.EmoteID, (int)emote);
        animator.SetTrigger(AnimParamIDs.Emote);
    }

    // Flag hold is a persistent pose (menu context), not a fire-and-forget
    // action, so it's a bool driving an Any-State loop rather than a trigger.
    // A trigger can't represent "stay in this pose until told otherwise".
    public void SetFlagHold(bool isHolding)
    {
        if (IsHoldingFlag == isHolding) return;
        IsHoldingFlag = isHolding;

        if (animator == null) return;
        animator.SetBool(AnimParamIDs.FlagHold, isHolding);
    }

    public void ResetAnimationState()
    {
        SetRaceState(RaceStartState.Idle);
        SetNormalizedSpeed(0f);
    }

    // --- Animation Event receivers ---
    // Add an Animation Event on the last frame of the FinishDip / Emote
    // clips that calls the matching method below by name.
    public void AnimEvent_FinishDipComplete() => OnFinishDipComplete?.Invoke();
    public void AnimEvent_EmoteComplete() => OnEmoteComplete?.Invoke();
}