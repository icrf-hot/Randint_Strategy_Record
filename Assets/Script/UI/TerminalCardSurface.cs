using UnityEngine;

/// <summary>Remaps monochrome card art without modifying its source texture or collider.</summary>
public sealed class TerminalCardSurface : MonoBehaviour
{
    private static Material shared;
    private static int users;
    private bool registered;
    private void Awake()
    {
        if (!TryGetComponent<SpriteRenderer>(out var renderer)) return;
        if (shared == null) {
            var shader = Shader.Find("Randint/TerminalCard"); if (shader == null) return;
            shared = new Material(shader); shared.SetColor("_PanelColor", UITheme.BgPanelHi); shared.SetColor("_InkColor", UITheme.Accent);
        }
        renderer.sharedMaterial = shared; renderer.color = UITheme.TextureTint;
        users++; registered = true;
    }
    private void OnDestroy() { if (registered && --users <= 0) { if (shared != null) Destroy(shared); shared = null; users = 0; } }
}
