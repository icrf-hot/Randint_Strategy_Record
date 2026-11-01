using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Bounded, reusable screen-space sprites/numbers; no per-hit GameObject destruction.</summary>
public sealed class CombatFeedbackPool : MonoBehaviour
{
    private sealed class Item
    {
        public RectTransform rect; public TextMeshProUGUI text; public UnityEngine.UI.Image image;
        public float time, life; public Action<RectTransform, float> animate; public Color color;
        public bool isText;
    }
    private readonly List<Item> items = new List<Item>();
    public RectTransform Root { get; private set; }
    public Func<float> Delta;
    private Camera view;
    public int ActiveCount { get { int count = 0; foreach (var item in items) if (item.rect.gameObject.activeSelf) count++; return count; } }
    public void Initialize(RectTransform root, Camera camera) { Root = root; view = camera; }
    public Vector2 Project(Vector3 world)
    {
        if (view == null) return Vector2.zero;
        var screen = view.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, null, out var point); return point;
    }
    public RectTransform Emit(string text, Sprite sprite, Vector2 size, Color color, float life, Action<RectTransform, float> animate)
    {
        bool isText = text != null;
        Item item = items.Find(x => !x.rect.gameObject.activeSelf && x.isText == isText);
        if (item == null) {
            if (items.Count >= 200) return null;
            var rect = TerminalUI.Rect("Feedback", Root, Vector2.zero, size);
            item = new Item { rect = rect, isText = isText };
            if (isText) {
                item.text = rect.gameObject.AddComponent<TextMeshProUGUI>(); item.text.font = UITheme.Mono;
                item.text.alignment = TextAlignmentOptions.Center; item.text.raycastTarget = false;
                item.text.textWrappingMode = TextWrappingModes.NoWrap;
                item.text.outlineColor = UITheme.BgBase; item.text.outlineWidth = .2f;
            } else { item.image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); item.image.raycastTarget = false; }
            items.Add(item);
        }
        item.time = 0; item.life = Mathf.Max(.001f, life); item.animate = animate; item.color = color;
        item.rect.sizeDelta = size; item.rect.localScale = Vector3.one; item.rect.localRotation = Quaternion.identity;
        if (isText) { item.text.text = text; item.text.fontSize = size.y * .65f; item.text.color = color; }
        else { item.image.sprite = sprite; item.image.color = color; item.image.preserveAspect = sprite != null; }
        item.rect.gameObject.SetActive(true); animate?.Invoke(item.rect, 0); return item.rect;
    }
    private void Update()
    {
        float dt = Delta != null ? Delta() : Time.unscaledDeltaTime;
        int count = items.Count;
        for (int i = 0; i < count; i++) {
            var item = items[i];
            if (!item.rect.gameObject.activeSelf) continue;
            item.time += dt; float t = Mathf.Clamp01(item.time / item.life);
            item.animate?.Invoke(item.rect, t);
            var color = item.color; color.a *= 1 - t;
            if (item.isText) item.text.color = color; else item.image.color = color;
            if (t >= 1) { item.rect.gameObject.SetActive(false); item.animate = null; }
        }
    }
    public void Clear() { foreach (var item in items) { item.rect.gameObject.SetActive(false); item.animate = null; } }
}
