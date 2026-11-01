using UnityEngine;

[CreateAssetMenu(menuName = "Randint/Combat/Feedback Library")]
public sealed class AttackFeedbackLibrary : ScriptableObject
{
    [System.Serializable] public struct CharacterProfile { public OperatorData character; public AttackFeedbackProfile profile; }
    public AttackFeedbackProfile front, middle, back, enemy;
    public CharacterProfile[] characters;
    [Min(0)] public float maxFocusDelay = .02f, maxActionGap = .02f;
    public bool automaticFocus;
    public AttackFeedbackProfile For(GameObject actor, OperatorPosition slot)
    {
        var op = actor != null ? actor.GetComponent<Operator>() : null;
        if (op == null) return enemy;
        if (characters != null) foreach (var item in characters)
            if (item.character == op.Data && item.profile != null) return item.profile;
        return slot == OperatorPosition.Front ? front : slot == OperatorPosition.Middle ? middle : back;
    }
}
