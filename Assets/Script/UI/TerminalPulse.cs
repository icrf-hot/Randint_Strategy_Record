using UnityEngine;

public sealed class TerminalPulse : MonoBehaviour
{
    private string previous;
    private TMPro.TMP_Text text;
    private float remaining;
    private Vector3 rest;
    private void Awake() { text = GetComponent<TMPro.TMP_Text>(); rest = transform.localScale; previous = text != null ? text.text : ""; }
    private void LateUpdate()
    {
        if (text == null) return;
        if (text.text != previous) { previous = text.text; remaining = UITheme.PulseTime; }
        remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
        float t = remaining / UITheme.PulseTime;
        transform.localScale = rest * (1 + (TerminalPreferences.Motion ? .065f * Mathf.Sin(t * Mathf.PI) : 0));
    }
}
