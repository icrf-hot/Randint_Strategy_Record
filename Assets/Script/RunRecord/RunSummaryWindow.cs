using System;
using TMPro;
using UnityEngine;

public sealed class RunSummaryWindow : MonoBehaviour
{
    private static RunSummaryWindow current;
    public static bool IsOpen => current != null;
    private UnityEngine.UI.Button continueButton;

    public static void Show(RunEndSummary summary, Func<bool> onContinue)
    {
        if (IsOpen) return;
        var canvas = TerminalUI.Canvas("RunSummaryWindow", 230);
        current = canvas.gameObject.AddComponent<RunSummaryWindow>();
        current.Build(summary, onContinue);
    }

    private void Build(RunEndSummary summary, Func<bool> onContinue)
    {
        var backdrop = TerminalUI.Panel("InputBlocker", transform, Vector2.zero, Vector2.zero, UITheme.Tone.Base);
        TerminalUI.Stretch(backdrop.rectTransform);
        backdrop.raycastTarget = true;
        var panel = TerminalUI.Panel("RunReport", backdrop.transform, Vector2.zero, new Vector2(1110, 660));
        var tone = summary.Cleared ? UITheme.Tone.Ok : UITheme.Tone.Danger;
        TerminalUI.Label("Outcome", panel.transform, RunEndSummary.Text(summary.Cleared ? "cleared" : "defeated"),
            new Vector2(0, 284), new Vector2(1030, 45), 32, tone);
        string reason = summary.Cleared ? "clear_note" : summary.EndReason == "TerminalDefeat" ? "terminal_defeat" : "ejected";
        TerminalUI.Label("Reason", panel.transform, RunEndSummary.Text(reason), new Vector2(0, 240), new Vector2(1030, 36), 20);

        var scrollRoot = TerminalUI.Rect("ReportScroll", panel.transform, new Vector2(0, -8), new Vector2(1030, 430));
        // Let TMP upload a newly populated dynamic atlas and register the clipping mask before showing the report.
        var reportVisibility = scrollRoot.gameObject.AddComponent<CanvasGroup>();
        reportVisibility.alpha = 0;
        var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        var viewport = TerminalUI.Panel("Viewport", scrollRoot, Vector2.zero, Vector2.zero);
        TerminalUI.Stretch(viewport.rectTransform);
        viewport.raycastTarget = true;
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var content = TerminalUI.Rect("Content", viewport.transform, Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = new Vector2(-36, 0);
        var text = content.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = summary.Report();
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = false;
        text.raycastTarget = false;
        TerminalUI.Bind(text.gameObject, UITheme.Tone.Main, true);
        var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32;
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1;
        StartCoroutine(RevealReport(reportVisibility, text));

        var notice = TerminalUI.Label("Notice", panel.transform, RunEndSummary.Text("scroll_hint"),
            new Vector2(0, -248), new Vector2(1030, 28), 17, UITheme.Tone.Dim);
        continueButton = TerminalUI.Button("ReturnToLobby", panel.transform, RunEndSummary.Text("lobby"),
            new Vector2(0, -292), new Vector2(360, 42));
        continueButton.onClick.AddListener(() =>
        {
            if (TransitionManager.IsBusy) return;
            if (onContinue()) Destroy(gameObject);
            else notice.text = RunEndSummary.Text("lobby_error");
        });
    }

    private System.Collections.IEnumerator RevealReport(CanvasGroup visibility, TMP_Text text)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        text.ForceMeshUpdate();
        yield return null;
        visibility.alpha = 1;
    }

    private void Update()
    {
        if (continueButton != null) continueButton.interactable = !TransitionManager.IsBusy;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
