using UnityEngine;

[CreateAssetMenu(menuName = "Randint/Combat/Feedback Library")]
public sealed class AttackFeedbackLibrary : ScriptableObject
{
    [System.Serializable] public struct CharacterProfile
    {
        public OperatorData character;
        public AttackFeedbackProfile profile, defense;
    }
    [System.Serializable] public struct ClassProfile
    {
        public OperatorClass classId;
        [Tooltip("Empty matches the class; e.g. heavy_shooter matches a JSON branchId.")]
        public string branchId;
        public AttackFeedbackProfile profile, defense;
    }
    public AttackFeedbackProfile front, middle, back, enemy;
    public AttackFeedbackProfile frontDefense, supporter;
    public CharacterProfile[] characters;
    public ClassProfile[] classes;
    [Header("Readable battle pacing (1x baseline)")]
    [Range(.1f, 1)] public float basePlaybackSpeed = .3f;
    [Min(0)] public float maxFocusDelay = .02f, maxActionGap = .12f;
    public bool automaticFocus;
    public AttackFeedbackProfile For(GameObject actor, OperatorPosition slot, CombatActionKind action = CombatActionKind.Attack)
    {
        var op = actor != null ? actor.GetComponent<Operator>() : null;
        if (op == null) return enemy;
        bool defense = action == CombatActionKind.Defense;
        if (characters != null) foreach (var item in characters)
            if (item.character == op.Data && (defense ? item.defense : item.profile) != null)
                return defense ? item.defense : item.profile;
        if (classes != null)
        {
            // Exact branch before the generic class, independent of Inspector ordering.
            foreach (bool exactBranch in new[] { true, false }) foreach (var item in classes)
            {
                bool hasBranch = !string.IsNullOrEmpty(item.branchId);
                if (item.classId != op.Class || hasBranch != exactBranch ||
                    (hasBranch && item.branchId != op.Data.BranchId)) continue;
                var match = defense ? item.defense : item.profile;
                if (match != null) return match;
            }
        }
        if (defense && frontDefense != null) return frontDefense;
        if (action == CombatActionKind.SupportAttack && supporter != null) return supporter;
        return slot == OperatorPosition.Front ? front : slot == OperatorPosition.Middle ? middle : back;
    }
}
