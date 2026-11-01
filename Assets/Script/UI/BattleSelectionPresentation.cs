using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

/// <summary>전투 선택 UI를 카드 위에 유지하며 가려진 카드로의 클릭 관통을 막습니다.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-40)]
public sealed class BattleSelectionPresentation : MonoBehaviour
{
    [SerializeField] private Canvas textCanvas;
    [SerializeField] private Transform[] panels;
    [SerializeField] private SpriteRenderer[] cardBlockingSurfaces;
    private static BattleSelectionPresentation current;
    private static readonly List<RaycastResult> OverlayHits = new List<RaycastResult>();

    private void Awake() => ApplyOrdering();
    private void OnEnable() { current = this; ApplyOrdering(); }
    private void OnDisable() { if (current == this) current = null; }

    public void ApplyOrdering()
    {
        if (textCanvas == null || panels == null) return;
        SortingLayer highest = SortingLayer.layers[0];
        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.value > highest.value) highest = layer;
        // 각 Sprite의 내부 순서는 유지하고 패널 묶음만 카드보다 앞으로 올립니다.
        foreach (Transform panel in panels)
        {
            if (panel == null) continue;
            SortingGroup group = panel.GetComponent<SortingGroup>();
            if (group == null) group = panel.gameObject.AddComponent<SortingGroup>();
            group.sortingLayerID = highest.id;
            group.sortingOrder = UITheme.BattlePanelSortBase;
        }
        textCanvas.sortingLayerID = highest.id;
        textCanvas.sortingOrder = UITheme.BattlePanelTextOrder;
    }

    public static bool BlocksCardPointer(Vector2 screenPosition)
    {
        if (current == null || current.cardBlockingSurfaces == null) return false;
        Camera camera = current.textCanvas != null ? current.textCanvas.worldCamera : null;
        if (camera == null) camera = Camera.main;
        if (camera == null) return false;
        Ray ray = camera.ScreenPointToRay(screenPosition);
        foreach (SpriteRenderer surface in current.cardBlockingSurfaces)
        {
            if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy || surface.color.a <= 0) continue;
            Bounds bounds = surface.bounds;
            if (!new Plane(Vector3.forward, bounds.center).Raycast(ray, out float distance)) continue;
            Vector3 point = ray.GetPoint(distance);
            if (point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y)
                return true;
        }
        return false;
    }

    public static bool OverlayBlocksPointer(Vector2 screenPosition)
    {
        EventSystem system = EventSystem.current;
        if (system == null) return false;
        OverlayHits.Clear();
        system.RaycastAll(new PointerEventData(system) { position = screenPosition }, OverlayHits);
        foreach (RaycastResult hit in OverlayHits)
        {
            // Collider 직접 입력도 설정·결과·전환 Canvas를 뚫지 않게 합니다.
            if (hit.module is UnityEngine.UI.GraphicRaycaster graphic && graphic.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay)
                return true;
        }
        return false;
    }
}
