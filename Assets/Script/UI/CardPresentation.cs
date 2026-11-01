using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

/// <summary>Keeps a card together and ahead of battlefield graphics; modal/transition canvases still cover it.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-50)]
public sealed class CardPresentation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SpriteRenderer[] sprites;
    public int RenderOrder { get; private set; }
    private Canvas[] canvases;
    private int handIndex;
    private bool hovered, selected;
    private void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>(true)) {
            sprite.sortingOrder = sprite.transform == transform ? 0 : 10;
            if (sprite.transform != transform) {
                Vector3 position = sprite.transform.localPosition; position.z = -.01f; sprite.transform.localPosition = position;
            }
        }
        foreach (var canvas in canvases) {
            Vector3 position = canvas.transform.localPosition; position.z = -.02f; canvas.transform.localPosition = position;
            canvas.overrideSorting = true;
        }
        RefreshOrder();
    }
    public void SetHandIndex(int index) { handIndex = Mathf.Clamp(index, 0, 100); RefreshOrder(); }
    public void SetSelected(bool value) { selected = value; RefreshOrder(); }
    private void RefreshOrder()
    {
        if (sprites == null) return;
        var layers = SortingLayer.layers; var highest = layers[0];
        foreach (var layer in layers) if (layer.value > highest.value) highest = layer;
        int order = UITheme.CardSortBase + handIndex * UITheme.CardSortStride + (hovered || selected ? UITheme.CardSortRaise : 0);
        RenderOrder = order;
        foreach (var sprite in sprites) { sprite.sortingLayerID = highest.id; sprite.sortingOrder = order + (sprite.transform == transform ? 0 : 10); }
        foreach (var canvas in canvases) { canvas.sortingLayerID = highest.id; canvas.sortingOrder = order + 20; }
    }
    public void OnPointerEnter(PointerEventData data) { hovered = true; RefreshOrder(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; RefreshOrder(); }
    private void OnDisable() { hovered = false; }
    private void OnEnable() => RefreshOrder();
}
