using TMPro;
using UnityEngine;

[ExecuteAlways]
public sealed class UIThemeBinding : MonoBehaviour
{
    public UITheme.Tone tone = UITheme.Tone.Main;
    [Range(0, 1)] public float opacity = 1;
    public bool monospace;
    private void OnEnable() => Apply();
    private void OnValidate() => Apply();
    public void Apply()
    {
        var graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null) graphic.color = UITheme.Alpha(UITheme.Resolve(tone), opacity);
        foreach (var border in GetComponents<UnityEngine.UI.Outline>()) border.effectColor = UITheme.Line;
        if (graphic is TMP_Text text) {
            text.font = monospace ? UITheme.Mono : UITheme.Font;
            text.raycastTarget = false;
            if (monospace) text.fontFeatures.Clear();
        }
    }
}
