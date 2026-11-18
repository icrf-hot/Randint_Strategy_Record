using UnityEngine;
using UnityEngine.Events;

/// <summary>Optional per-character adapter. Assign an Animator on the visual child, never the logical root.</summary>
public sealed class CombatAnimationTemplate : MonoBehaviour
{
    [SerializeField] private Animator visualAnimator;
    public UnityEvent onAnticipation = new UnityEvent();
    public UnityEvent onDash = new UnityEvent();
    public UnityEvent onImpactWindow = new UnityEvent();
    public UnityEvent onRecovery = new UnityEvent();
    public UnityEvent onResolve = new UnityEvent();
    private AttackSequence active;
    private string trigger;

    public void Bind(AttackSequence sequence, AttackFeedbackProfile profile)
    {
        active = sequence;
        trigger = profile.animationTrigger;
        if (visualAnimator != null && !string.IsNullOrEmpty(trigger)) visualAnimator.SetTrigger(trigger);
    }

    public void Enter(AttackStage stage)
    {
        switch (stage)
        {
            case AttackStage.Anticipation: onAnticipation.Invoke(); break;
            case AttackStage.Dash: onDash.Invoke(); break;
            case AttackStage.Impact: onImpactWindow.Invoke(); break;
            case AttackStage.Recovery: onRecovery.Invoke(); break;
            case AttackStage.Resolve: onResolve.Invoke(); break;
        }
    }

    // Animation Event at the actual contact/heal frame; shares the sequence's exactly-once gate.
    public void OnImpact() => active?.RequestImpact();
    public void Unbind(AttackSequence sequence)
    {
        if (active != sequence) return;
        if (visualAnimator != null && !string.IsNullOrEmpty(trigger)) visualAnimator.ResetTrigger(trigger);
        active = null;
    }
}
