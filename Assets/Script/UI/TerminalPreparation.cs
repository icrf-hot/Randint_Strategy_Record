using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Presentation of the existing position-constrained roster; never invents combat stats.</summary>
public sealed class TerminalPreparation : MonoBehaviour
{
    private RectTransform drawer;
    private Transform root;
    private UnityEngine.UI.Button[] changes;
    private void Start()
    {
        root = GameObject.Find("PreparationCanvas")?.transform;
        if (root == null) return;
        var loss = root.Find("DataLossLevel")?.GetComponent<TMP_Text>();
        if (loss != null) loss.text = "STAGE 00 / 도입 구간";
        var lossBar = TerminalUI.Progress("DataLossBar", root, new Vector2(-315, -250), new Vector2(350, 8), UITheme.Tone.Warn);
        // New run starts with the actual RunRecordHealth baseline, independent of a previous run.
        var health = new RunRecordHealth(); float value = RunRecordHealth.Maximum - health.Integrity;
        lossBar.SetValue(value / RunRecordHealth.Maximum);
        TerminalUI.Label("LossValue", root, value.ToString("000") + "%", new Vector2(-95, -230), new Vector2(90, 30), 18, UITheme.Tone.Warn, true);
        TerminalUI.Label("FormationHint", root, "FRONT → MIDDLE → BACK : 각 위치에 지정된 대원의 카드와 전투 역할이 적용됩니다.",
            new Vector2(0, -191), new Vector2(1100, 30), 16, UITheme.Tone.Dim);
        bool valid = PreparationSceneController.TryGetRoster(out OperatorData[] roster);
        string[] names = { "FrontOperator", "MiddleOperator", "BackOperator" };
        changes = new UnityEngine.UI.Button[3];
        for (int i = 0; i < names.Length; i++) {
            var slot = root.Find(names[i]); if (slot == null) continue;
            var op = valid ? roster[i] : null;
            TerminalUI.Label("CoreStats", slot, op != null ? $"HP {op.MaxHP:D4}  ATK {op.Attack:D3}  DEF {op.Defense:D3}" : "데이터 없음",
                new Vector2(0, -124), new Vector2(320, 26), 15, UITheme.Tone.Main, true);
            var classLabel = TerminalUI.Tag("ClassTag", slot, op != null ? op.DetailedClass.ToString().ToUpperInvariant() : "UNAVAILABLE",
                new Vector2(0, -95), new Vector2(180, 24));
            classLabel.rectTransform.anchoredPosition = new Vector2(12, 0);
            classLabel.rectTransform.sizeDelta = new Vector2(145, 24);
            var icon = op != null && op.DetailedClass == OperatorClass.Defender ? TerminalGraphic.Shape.Shield : op != null && op.DetailedClass == OperatorClass.Healer ? TerminalGraphic.Shape.Heal : TerminalGraphic.Shape.Caster;
            TerminalGraphic.Create("ClassIcon", classLabel.transform.parent, icon, new Vector2(-72, 0), new Vector2(19, 19));
            TerminalGraphic.Create("GroundGlow", slot, TerminalGraphic.Shape.Glow, new Vector2(0, -37), new Vector2(210, 26)).transform.SetSiblingIndex(1);
            var change = slot.Find("ChangeOperator")?.GetComponent<UnityEngine.UI.Button>();
            if (change == null) continue;
            changes[i] = change;
            int index = i;
            // Existing locked-roster handler remains available as a legacy API; this view explains available data.
            var controller = GetComponent<PreparationSceneController>();
            change.onClick.RemoveListener(controller.ShowLockedWarning);
            change.onClick.AddListener(() => Open(index));
        }
        var startButton = root.Find("StartRun")?.GetComponent<UnityEngine.UI.Button>();
        if (startButton != null) {
            var tooltip = startButton.gameObject.AddComponent<TerminalTooltip>();
            tooltip.message = "편성이 완료되지 않았습니다";
        }
    }
    private void Open(int index)
    {
        if (TransitionManager.IsBusy || drawer != null) return;
        var backdrop = TerminalUI.Panel("RosterBackdrop", root, Vector2.zero, Vector2.zero, UITheme.Tone.Clear);
        TerminalUI.Stretch(backdrop.rectTransform); backdrop.raycastTarget = true;
        drawer = TerminalUI.Panel("RosterDrawer", backdrop.transform, Vector2.zero, new Vector2(410, 720)).rectTransform;
        drawer.anchorMin = drawer.anchorMax = drawer.pivot = new Vector2(1, .5f);
        TerminalUI.Label("Heading", drawer, new[] { "FRONT", "MIDDLE", "BACK" }[index] + " / 대원 목록", new Vector2(0, 300), new Vector2(370, 40), 24);
        if (PreparationSceneController.TryGetRoster(out OperatorData[] roster)) {
            OperatorData op = roster[index];
            TerminalUI.Tag("Class", drawer, op.DetailedClass.ToString().ToUpperInvariant(), new Vector2(0, 235), new Vector2(300, 30));
            TerminalUI.Label("Name", drawer, op.OperatorName, new Vector2(0, 175), new Vector2(370, 45), 24);
            TerminalUI.Label("Stats", drawer, $"HP {op.MaxHP:D4} / ATK {op.Attack:D3}\nDEF {op.Defense:D3}", new Vector2(0, 110), new Vector2(370, 65), 18, UITheme.Tone.Main, true);
            TerminalUI.Label("Availability", drawer, "현재 데이터에 등록된 해당 포지션의 대원입니다.", new Vector2(0, 20), new Vector2(380, 45), 16, UITheme.Tone.Dim);
            var select = TerminalUI.Button("Select", drawer, "현재 편성 유지", new Vector2(0, -70), new Vector2(300, 44));
            select.onClick.AddListener(Close);
        } else TerminalUI.Label("Unavailable", drawer, "편성 데이터를 불러올 수 없습니다", new Vector2(0, 80), new Vector2(370, 60));
        var close = TerminalUI.Button("Close", drawer, "닫기", new Vector2(0, -285), new Vector2(300, 44)); close.onClick.AddListener(Close);
        StartCoroutine(Slide()); close.Select();
    }
    private IEnumerator Slide()
    {
        float time = 0;
        do { time += Time.unscaledDeltaTime; drawer.anchoredPosition = new Vector2(TerminalPreferences.Motion ? Mathf.Lerp(410, 0, Mathf.Clamp01(time / .1f)) : 0, 0); yield return null; }
        while (drawer != null && time < .1f);
    }
    private void Close() { if (drawer != null) Destroy(drawer.parent.gameObject); drawer = null; }
    private void Update() { if (drawer != null && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close(); }
}
