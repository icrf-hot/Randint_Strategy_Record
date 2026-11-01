using System;
using System.Collections;
using UnityEngine;

public enum AttackStage { Anticipation, Dash, Impact, Recovery, Resolve }
public enum AttackDamageType { PHYS, ARTS, HEAL }
public enum AttackOutcome { Normal, Weak, Resist, Miss, Null }

[Serializable]
public struct AttackCardSnapshot
{
    public string label, role;
    public int value;
    public Vector3 worldOrigin;
}

/// <summary>Confirmed domain result. Damage is actual HP lost, including the lethal clamp.</summary>
[Serializable]
public struct AttackHitInfo
{
    public long actionId;
    public int damage, attackValue, hpBefore, hpAfter, maxHP;
    public AttackDamageType damageType;
    public AttackOutcome outcome;
    public bool isCritical, isKill, targetIsAlly;
    public string attackerSlot, targetSlot;
    public AttackCardSnapshot[] cards;
    public Vector3 position, direction;
    public GameObject attacker, target;
    public int logicalHitIndex, presentationHitCount;
}

/// <summary>The only commit point. Animation events and skip share the same idempotent gate.</summary>
public sealed class AttackSequence
{
    private static long nextId;
    public readonly long Id = System.Threading.Interlocked.Increment(ref nextId);
    public AttackStage Stage { get; private set; }
    public bool HasCommitted { get; private set; }
    public bool IsComplete { get; private set; }
    public event Action<AttackSequence, AttackStage> StageEntered, StageExited;
    public event Action<GameObject, GameObject, AttackHitInfo> OnImpact;
    private readonly Func<AttackHitInfo[]> commit;
    public AttackSequence(Func<AttackHitInfo[]> commit) { this.commit = commit; }

    // Called by the coroutine, skip, or BattleAnimationImpactRelay at the hit frame.
    public bool RequestImpact()
    {
        if (HasCommitted || IsComplete || Stage != AttackStage.Impact) return false;
        HasCommitted = true; // Gate BEFORE domain callbacks, including re-entrant animation events.
        var hits = commit?.Invoke();
        if (hits != null) foreach (var result in hits) {
            var hit = result; hit.actionId = Id;
            if (OnImpact != null) foreach (Action<GameObject, GameObject, AttackHitInfo> listener in OnImpact.GetInvocationList())
                try { listener(hit.attacker, hit.target, hit); } catch (Exception e) { Debug.LogException(e); }
        }
        return true;
    }
    public IEnumerator Run(AttackFeedbackProfile profile, Func<bool> skip, Func<float> delta,
        Func<AttackStage, float, IEnumerator> animate)
    {
        foreach (AttackStage phase in Enum.GetValues(typeof(AttackStage))) {
            Stage = phase; StageEntered?.Invoke(this, phase);
            if (phase == AttackStage.Impact && (!profile.animationDrivenImpact || skip())) RequestImpact();
            float duration = phase switch {
                AttackStage.Anticipation => profile.anticipation, AttackStage.Dash => profile.dash,
                AttackStage.Impact => profile.impact, AttackStage.Recovery => profile.recovery, _ => 0
            };
            if (!skip() && duration > 0) {
                var animation = animate?.Invoke(phase, duration);
                if (animation != null) yield return animation;
                else { float elapsed = 0; while (elapsed < duration && !skip()) { elapsed += delta(); yield return null; } }
            }
            // An absent/late Animation Event must never swallow a game action.
            if (phase == AttackStage.Impact) RequestImpact();
            StageExited?.Invoke(this, phase);
        }
        IsComplete = true;
    }
}
