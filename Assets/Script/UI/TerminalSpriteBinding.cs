using UnityEngine;

[ExecuteAlways, DefaultExecutionOrder(-200)]
public sealed class TerminalSpriteBinding : MonoBehaviour
{
    public UITheme.Tone tone = UITheme.Tone.Panel;
    [Range(0, 1)] public float opacity = 1;
    private void Awake() => Apply();
    private void OnEnable() => Apply();
    private void OnValidate() => Apply();
    public void Apply() { if (TryGetComponent<SpriteRenderer>(out var renderer)) renderer.color = UITheme.Alpha(UITheme.Resolve(tone), opacity); }
}
