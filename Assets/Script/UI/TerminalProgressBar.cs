using UnityEngine;

public sealed class TerminalProgressBar : MonoBehaviour
{
    public UnityEngine.UI.Image fill;
    public UITheme.Tone tone = UITheme.Tone.Accent;
    public bool criticalBlink;
    private float value;
    public void SetValue(float normalized)
    {
        value = Mathf.Clamp01(normalized);
        if (fill != null) {
            fill.rectTransform.anchorMax = new Vector2(value, 1);
            fill.rectTransform.offsetMax = Vector2.zero;
        }
    }
    private void Update()
    {
        if (fill == null) return;
        bool critical = criticalBlink && value <= .3f;
        float alpha = critical && TerminalPreferences.Motion ? .55f + .45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 2)) : 1;
        fill.color = UITheme.Alpha(critical ? UITheme.Danger : UITheme.Resolve(tone), alpha);
    }
}
