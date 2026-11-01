using TMPro;
using UnityEngine;

public static class TerminalUI
{
    public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        SlotUI.Place(rect, position, size);
        return rect;
    }
    public static UIThemeBinding Bind(GameObject go, UITheme.Tone tone, bool mono = false, float opacity = 1)
    {
        var binding = go.GetComponent<UIThemeBinding>() ?? go.AddComponent<UIThemeBinding>();
        binding.tone = tone; binding.monospace = mono; binding.opacity = opacity; binding.Apply();
        return binding;
    }
    public static UnityEngine.UI.Image Panel(string name, Transform parent, Vector2 position, Vector2 size, UITheme.Tone tone = UITheme.Tone.Panel)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.raycastTarget = false;
        Bind(rect.gameObject, tone);
        if (tone != UITheme.Tone.Clear) {
            var border = rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            border.effectColor = UITheme.Line; border.effectDistance = new Vector2(UITheme.Border, -UITheme.Border);
        }
        return image;
    }
    public static TMP_Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size,
        float fontSize = 20, UITheme.Tone tone = UITheme.Tone.Main, bool mono = false)
    {
        var rect = Rect(name, parent, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        Bind(rect.gameObject, tone, mono);
        return text;
    }
    public static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 position,
        Vector2 size, TerminalButton.Variant variant = TerminalButton.Variant.Default)
    {
        var image = Panel(name, parent, position, size, UITheme.Tone.Clear);
        image.raycastTarget = true;
        Object.DestroyImmediate(image.GetComponent<UIThemeBinding>());
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        Label("Label", image.transform, label, Vector2.zero, size - new Vector2(16, 6));
        image.gameObject.AddComponent<TerminalButton>().variant = variant;
        return button;
    }
    public static TerminalProgressBar Progress(string name, Transform parent, Vector2 position, Vector2 size, UITheme.Tone tone = UITheme.Tone.Accent)
    {
        var image = Panel(name, parent, position, size, UITheme.Tone.Hover);
        var fill = Panel("Fill", image.transform, Vector2.zero, Vector2.zero, tone);
        var rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var bar = image.gameObject.AddComponent<TerminalProgressBar>(); bar.fill = fill; bar.tone = tone;
        return bar;
    }
    public static TMP_Text Tag(string name, Transform parent, string value, Vector2 position, Vector2 size)
    {
        var panel = Panel(name, parent, position, size, UITheme.Tone.Hover);
        return Label("Label", panel.transform, value, Vector2.zero, size, 17, UITheme.Tone.Accent, true);
    }
    public static Canvas Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        return canvas;
    }
    public static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
