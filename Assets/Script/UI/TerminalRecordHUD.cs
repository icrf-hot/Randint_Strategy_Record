using TMPro;
using UnityEngine;

public sealed class TerminalRecordHUD : MonoBehaviour
{
    private TMP_Text[] values = new TMP_Text[2], states = new TMP_Text[2];
    private TerminalProgressBar[] bars = new TerminalProgressBar[2];
    private int[] displayed = { -1, -1 };
    private GameObject canvas;
    private void Start()
    {
        var owner = TerminalUI.Canvas("RecordHealthHUD", 12); canvas = owner.gameObject;
        var panel = TerminalUI.Panel("RecordHealthPanel", owner.transform, new Vector2(20, 20), new Vector2(350, 108));
        var rect = panel.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        for (int i = 0; i < 2; i++) {
            var row = TerminalUI.Rect("Row" + i, panel.transform, new Vector2(0, 25 - i * 48), new Vector2(326, 26));
            var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 8; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            var label = TerminalUI.Label("Label", row, i == 0 ? "무결성" : "일관성", Vector2.zero, new Vector2(95, 26), 17, UITheme.Tone.Dim);
            values[i] = TerminalUI.Label("Value", row, "", Vector2.zero, new Vector2(65, 26), 18, UITheme.Tone.Main, true);
            states[i] = TerminalUI.Label("State", row, "", Vector2.zero, new Vector2(110, 26), 16, UITheme.Tone.Dim);
            TMP_Text[] fields = { label, values[i], states[i] }; float[] widths = { 95, 65, 110 };
            for (int j = 0; j < fields.Length; j++) {
                var item = fields[j].gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); item.preferredWidth = widths[j]; item.preferredHeight = 26;
            }
            values[i].gameObject.AddComponent<TerminalPulse>();
            bars[i] = TerminalUI.Progress("Bar" + i, panel.transform, new Vector2(0, 5 - i * 48), new Vector2(310, 5));
            bars[i].criticalBlink = i == 0;
        }
    }
    private void LateUpdate()
    {
        if (canvas == null) return;
        canvas.SetActive(MapRunState.Progression.IsInitialized && !TransitionManager.IsBusy);
        for (int i = 0; i < 2; i++) {
            int value = i == 0 ? MapRunState.RecordHealth.Integrity : MapRunState.RecordHealth.Consistency;
            states[i].color = value <= 30 ? UITheme.Danger : value < 70 ? UITheme.Warn : UITheme.Ok;
            if (displayed[i] == value) continue; displayed[i] = value;
            values[i].text = value.ToString("D3") + "%";
            states[i].text = value <= 30 ? "위험" : value < 70 ? "저하" : "정상";
            states[i].color = value <= 30 ? UITheme.Danger : value < 70 ? UITheme.Warn : UITheme.Ok;
            bars[i].SetValue(value / (float)RunRecordHealth.Maximum);
        }
    }
    private void OnDestroy() { if (canvas != null) Destroy(canvas); }
}
