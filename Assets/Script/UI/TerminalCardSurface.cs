using UnityEngine;
using System.Collections.Generic;

/// <summary>Theme the card surface while preserving the original colours of designated artwork.</summary>
public sealed class TerminalCardSurface : MonoBehaviour
{
    public bool preserveTextureColor;
    private sealed class Surface { public Material material; public int users; }
    private static readonly Dictionary<long, Surface> shared = new Dictionary<long, Surface>();
    private bool registered;
    private long key;
    private void Awake()
    {
        if (!TryGetComponent<SpriteRenderer>(out var renderer)) return;
        if (renderer.sprite == null) return;
        Texture texture = renderer.sprite.texture;
        key = ((long)texture.GetInstanceID() << 1) | (preserveTextureColor ? 1L : 0L);
        if (!shared.TryGetValue(key, out var surface) || surface.material == null) {
            var shader = Shader.Find("Randint/TerminalCard"); if (shader == null) return;
            surface = new Surface { material = new Material(shader) };
            surface.material.SetTexture("_MainTex", texture);
            surface.material.SetColor("_PanelColor", UITheme.BgPanelHi);
            surface.material.SetColor("_InkColor", UITheme.Accent);
            surface.material.SetFloat("_PreserveColor", preserveTextureColor ? 1 : 0);
            shared[key] = surface;
        }
        renderer.sharedMaterial = surface.material; renderer.color = UITheme.TextureTint;
        surface.users++; registered = true;
    }
    private void OnDestroy()
    {
        if (registered && shared.TryGetValue(key, out var surface) && --surface.users <= 0) {
            if (surface.material != null) Destroy(surface.material); shared.Remove(key);
        }
    }
}
