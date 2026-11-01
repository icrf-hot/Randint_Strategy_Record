using UnityEngine;

/// <summary>Presentation-only child. The authoritative transform and renderer remain untouched.</summary>
public sealed class CombatSpriteVisual : MonoBehaviour
{
    public SpriteRenderer Source { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public Transform Pivot => Sprite.transform;
    public bool Busy;
    public int Version { get; private set; }
    public int Acquire() { ResetVisual(); Busy = true; return Version; }
    private bool previousForceOff;
    private Vector3 basePosition, baseScale;
    private Quaternion baseRotation;
    private Material flashMaterial;
    private MaterialPropertyBlock properties;
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    public static CombatSpriteVisual Get(GameObject actor)
    {
        if (actor == null) return null;
        var existing = actor.GetComponent<CombatSpriteVisual>(); if (existing != null) return existing;
        return actor.AddComponent<CombatSpriteVisual>();
    }
    private void Awake()
    {
        // Select the illustration rather than targeting indicators or labels.
        foreach (var candidate in GetComponentsInChildren<SpriteRenderer>(true))
            if (candidate.sprite != null && (Source == null || candidate.bounds.size.sqrMagnitude > Source.bounds.size.sqrMagnitude)) Source = candidate;
        if (Source == null) { enabled = false; return; }
        var go = new GameObject("AttackVisual"); go.transform.SetParent(Source.transform, false);
        Sprite = go.AddComponent<SpriteRenderer>();
        var shader = Resources.Load<Shader>("CombatSprite");
        if (shader != null) flashMaterial = new Material(shader);
        properties = new MaterialPropertyBlock();
        basePosition = go.transform.localPosition; baseScale = go.transform.localScale; baseRotation = go.transform.localRotation;
        previousForceOff = Source.forceRenderingOff;
        Source.forceRenderingOff = true;
        Sync();
    }
    private void Sync()
    {
        if (Source == null || Sprite == null) return;
        Sprite.sprite = Source.sprite; Sprite.sharedMaterial = flashMaterial != null ? flashMaterial : Source.sharedMaterial;
        Sprite.sortingLayerID = Source.sortingLayerID; Sprite.sortingOrder = Source.sortingOrder;
        Sprite.flipX = Source.flipX; Sprite.flipY = Source.flipY;
        Sprite.drawMode = Source.drawMode; Sprite.size = Source.size;
        Sprite.enabled = Source.enabled; Sprite.color = Source.color;
    }
    private void LateUpdate() { if (!Busy) Sync(); }
    public void ResetVisual()
    {
        Version++;
        Busy = false;
        if (Sprite == null) return;
        Pivot.localPosition = basePosition; Pivot.localScale = baseScale; Pivot.localRotation = baseRotation; Sync();
        SetFlash(0);
    }
    public void SetFlash(float amount) { if (Sprite == null || properties == null) return; properties.SetFloat(FlashAmount, amount); Sprite.SetPropertyBlock(properties); }
    private void OnDisable() { ResetVisual(); if (Source != null) Source.forceRenderingOff = previousForceOff; if (Sprite != null) Sprite.enabled = false; }
    private void OnEnable() { if (Source != null) Source.forceRenderingOff = true; }
    private void OnDestroy() { if (flashMaterial != null) Destroy(flashMaterial); if (Sprite != null) Destroy(Sprite.gameObject); }
}
