using UnityEngine;

/// <summary>Texture-free terminal grid, scanline, line icons and portrait grounding.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TerminalGraphic : UnityEngine.UI.MaskableGraphic
{
    public enum Shape { Grid, Scanline, Gear, Folder, Launch, Glow, Dot, Frame, Shield, Heal, Caster }
    public Shape shape;
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear(); var r = rectTransform.rect;
        if (shape == Shape.Frame) {
            Quad(vh, new Rect(r.xMin, r.yMin, r.width, UITheme.Border), color);
            Quad(vh, new Rect(r.xMin, r.yMax - UITheme.Border, r.width, UITheme.Border), color);
            Quad(vh, new Rect(r.xMin, r.yMin, UITheme.Border, r.height), color);
            Quad(vh, new Rect(r.xMax - UITheme.Border, r.yMin, UITheme.Border, r.height), color);
        } else if (shape == Shape.Grid) {
            for (float x = r.xMin; x < r.xMax; x += 40) Quad(vh, new Rect(x, r.yMin, 1, r.height), UITheme.Alpha(UITheme.Accent, UITheme.GridAlpha));
            for (float y = r.yMin; y < r.yMax; y += 40) Quad(vh, new Rect(r.xMin, y, r.width, 1), UITheme.Alpha(UITheme.Accent, UITheme.GridAlpha));
        } else if (shape == Shape.Scanline) {
            float t = TerminalPreferences.Motion ? Mathf.Repeat(Time.unscaledTime * .025f, 1) : .5f;
            Quad(vh, new Rect(r.xMin, r.yMin + r.height * t, r.width, 3), UITheme.Alpha(UITheme.Accent, UITheme.ScanAlpha));
        } else if (shape == Shape.Glow || shape == Shape.Dot) {
            Color c = shape == Shape.Glow ? UITheme.Alpha(UITheme.Accent, UITheme.GlowAlpha) : UITheme.Ok;
            if (shape == Shape.Dot && TerminalPreferences.Motion) c.a = .5f + .5f * Mathf.Abs(Mathf.Cos(Time.unscaledTime * Mathf.PI / UITheme.StatusPeriod));
            int center = vh.currentVertCount; vh.AddVert(Vector3.zero, c, Vector2.zero);
            for (int i = 0; i <= 40; i++) {
                float a = i * Mathf.PI * 2 / 40;
                vh.AddVert(new Vector3(Mathf.Cos(a) * r.width / 2, Mathf.Sin(a) * r.height / 2),
                    shape == Shape.Glow ? UITheme.Clear : c, Vector2.zero);
                if (i > 0) vh.AddTriangle(center, center + i, center + i + 1);
            }
        } else {
            float s = Mathf.Min(r.width, r.height) * .36f;
            if (shape == Shape.Heal) {
                Line(vh, new Vector2(-s, 0), new Vector2(s, 0), UITheme.Accent);
                Line(vh, new Vector2(0, -s), new Vector2(0, s), UITheme.Accent);
            } else if (shape == Shape.Shield) {
                Vector2[] p = { new Vector2(-s, s), new Vector2(s, s), new Vector2(s, 0), new Vector2(0, -s), new Vector2(-s, 0), new Vector2(-s, s) };
                for (int i = 1; i < p.Length; i++) Line(vh, p[i-1], p[i], UITheme.Accent);
            } else if (shape == Shape.Caster) {
                Line(vh, new Vector2(-s, -s), new Vector2(s, s), UITheme.Accent);
                Line(vh, new Vector2(s * .4f, s), new Vector2(s, s * .4f), UITheme.Accent);
            } else if (shape == Shape.Gear) {
                for (int i = 0; i < 12; i++) {
                    float a = i * Mathf.PI / 6, b = (i + 1) * Mathf.PI / 6;
                    Line(vh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * s, UITheme.Accent);
                    if (i % 2 == 0) Line(vh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 1.22f, UITheme.Accent);
                }
                Line(vh, new Vector2(-s * .3f, 0), new Vector2(s * .3f, 0), UITheme.Accent);
            } else if (shape == Shape.Folder) {
                Line(vh, new Vector2(-s, -s * .6f), new Vector2(s, -s * .6f), UITheme.Accent);
                Line(vh, new Vector2(-s, -s * .6f), new Vector2(-s, s * .65f), UITheme.Accent);
                Line(vh, new Vector2(-s, s * .65f), new Vector2(-s * .2f, s * .65f), UITheme.Accent);
                Line(vh, new Vector2(-s * .2f, s * .65f), new Vector2(0, s * .35f), UITheme.Accent);
                Line(vh, new Vector2(0, s * .35f), new Vector2(s, s * .35f), UITheme.Accent);
                Line(vh, new Vector2(s, s * .35f), new Vector2(s, -s * .6f), UITheme.Accent);
            } else {
                Line(vh, new Vector2(-s * .45f, -s * .7f), new Vector2(s * .6f, 0), UITheme.Accent);
                Line(vh, new Vector2(s * .6f, 0), new Vector2(-s * .45f, s * .7f), UITheme.Accent);
                Line(vh, new Vector2(-s * .45f, s * .7f), new Vector2(-s * .45f, -s * .7f), UITheme.Accent);
            }
        }
    }
    private void Update() { if (shape == Shape.Scanline || shape == Shape.Dot) SetVerticesDirty(); }
    private static void Quad(UnityEngine.UI.VertexHelper vh, Rect r, Color c)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(r.xMin, r.yMin), c, Vector2.zero); vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), c, Vector2.zero); vh.AddVert(new Vector3(r.xMax, r.yMin), c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
    private static void Line(UnityEngine.UI.VertexHelper vh, Vector2 a, Vector2 b, Color c)
    {
        int i = vh.currentVertCount; Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized;
        vh.AddVert(a-n, c, Vector2.zero); vh.AddVert(a+n, c, Vector2.zero);
        vh.AddVert(b+n, c, Vector2.zero); vh.AddVert(b-n, c, Vector2.zero);
        vh.AddTriangle(i, i+1, i+2); vh.AddTriangle(i, i+2, i+3);
    }
    public static TerminalGraphic Create(string name, Transform parent, Shape shape, Vector2 position, Vector2 size)
    {
        var rect = TerminalUI.Rect(name, parent, position, size);
        var graphic = rect.gameObject.AddComponent<TerminalGraphic>(); graphic.shape = shape; graphic.raycastTarget = false; graphic.SetAllDirty(); return graphic;
    }
}
