using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class TerminalButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public enum Variant { Default, Emphasis, Danger, Start }
    public Variant variant;
    public bool liftOnHover;
    public UnityEvent clicked = new UnityEvent();
    public static event System.Action ClickSoundRequested;
    private UnityEngine.UI.Button button;
    private UnityEngine.UI.Image image;
    private TerminalGraphic border, glow;
    private TMPro.TMP_Text label;
    private bool hovered, pressed;
    private Vector3 rest;
    private RectTransform visual;
    private Color startBackground, targetBackground;
    private float colorElapsed;
    private Vector3 startPosition, targetPosition;
    private float liftElapsed;
    private void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        image = button.targetGraphic as UnityEngine.UI.Image ?? GetComponent<UnityEngine.UI.Image>();
        foreach (var oldBorder in image.GetComponents<UnityEngine.UI.Outline>()) oldBorder.enabled = false;
        foreach (var oldBorder in GetComponents<UnityEngine.UI.Outline>()) oldBorder.enabled = false;
        border = TerminalGraphic.Create("Border", image.transform, TerminalGraphic.Shape.Frame, Vector2.zero, Vector2.zero);
        TerminalUI.Stretch(border.rectTransform);
        glow = TerminalGraphic.Create("FocusGlow", image.transform, TerminalGraphic.Shape.Frame, Vector2.zero, Vector2.zero);
        TerminalUI.Stretch(glow.rectTransform); glow.rectTransform.offsetMin = -Vector2.one * 2; glow.rectTransform.offsetMax = Vector2.one * 2;
        label = (transform.Find("Label") ?? transform.Find("Content/Label"))?.GetComponent<TMPro.TMP_Text>();
        // Move a child visual, so layout groups retain sole ownership of the card root.
        visual = transform.Find("Content") as RectTransform;
        if (visual != null) rest = visual.localPosition;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        button.onClick.AddListener(OnClick);
        Paint(true);
    }
    private void OnClick() { clicked.Invoke(); ClickSoundRequested?.Invoke(); }
    private void Update() => Paint(false);
    private void Paint(bool instant)
    {
        if (button == null || image == null) return;
        bool enabledState = button.IsInteractable();
        Color ink = variant == Variant.Danger ? UITheme.Danger : variant == Variant.Start ? UITheme.Ok : UITheme.Accent;
        bool filled = variant != Variant.Default;
        Color background = !enabledState ? UITheme.BgPanel : pressed ? UITheme.BgPanelHi :
            hovered || filled ? UITheme.Alpha(ink, hovered ? .16f : .1f) : UITheme.Clear;
        if (background != targetBackground) { startBackground = image.color; targetBackground = background; colorElapsed = 0; }
        colorElapsed = instant ? UITheme.ButtonTime : colorElapsed + Time.unscaledDeltaTime;
        image.color = Color.Lerp(startBackground, targetBackground, Mathf.Clamp01(colorElapsed / UITheme.ButtonTime));
        border.color = !enabledState ? UITheme.Line : hovered || pressed || filled ? ink : liftOnHover ? UITheme.Line : UITheme.Accent;
        glow.color = UITheme.Alpha(ink, enabledState && (hovered || filled) ? UITheme.GlowAlpha : 0);
        if (label != null) label.color = enabledState ? UITheme.TextMain : UITheme.TextDim;
        if (visual != null && liftOnHover) {
            Vector3 destination = rest + (hovered && enabledState ? Vector3.up : Vector3.zero);
            if (destination != targetPosition) { startPosition = visual.localPosition; targetPosition = destination; liftElapsed = 0; }
            liftElapsed = instant ? UITheme.ButtonTime : liftElapsed + Time.unscaledDeltaTime;
            visual.localPosition = Vector3.Lerp(startPosition, targetPosition, Mathf.Clamp01(liftElapsed / UITheme.ButtonTime));
        }
    }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) => pressed = true;
    public void OnPointerUp(PointerEventData e) => pressed = false;
    private void OnDisable() { hovered = pressed = false; }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(OnClick); }
}
