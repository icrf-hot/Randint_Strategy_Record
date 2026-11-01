using UnityEngine;

/// <summary>Attach beside an Animator. Bind the current sequence before playing its clip.</summary>
public sealed class BattleAnimationImpactRelay : MonoBehaviour
{
    private AttackSequence active;
    public void Bind(AttackSequence sequence) => active = sequence;
    public void OnImpact() => active?.RequestImpact();
    public void Unbind(AttackSequence sequence) { if (active == sequence) active = null; }
}
