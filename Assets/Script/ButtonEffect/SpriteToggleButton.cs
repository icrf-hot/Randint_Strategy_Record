using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(50)]
public class SpriteToggleButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Group")]
    [SerializeField] private SpriteToggleGroup group;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Collider2D targetCollider;

    [Header("TMP")]
    [SerializeField] private TMP_Text targetText;

    [Header("Event")]
    [SerializeField] private UnityEvent onSelected;

    private Camera mainCamera;
    private bool hovered;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        if (targetText == null) targetText = GetComponentInChildren<TMP_Text>(true);
        if (group == null) group = GetComponentInParent<SpriteToggleGroup>();

        mainCamera = Camera.main;
        Paint(true);
    }

    private void OnEnable() => Paint(true);
    private void OnDisable() => hovered = false;

    private void Update()
    {
        hovered = false;
        if (mainCamera != null && targetCollider != null && Mouse.current != null &&
            !TransitionManager.IsBusy && !BattleExecuteManager.IsExecuting)
        {
            Vector2 position = Mouse.current.position.ReadValue();
            hovered = IsPointerInsideCollider(position) && !BattleSelectionPresentation.OverlayBlocksPointer(position);
            if (hovered && Mouse.current.leftButton.wasPressedThisFrame) Activate(position);
        }
        Paint(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) Activate(eventData.position);
    }

    private void Activate(Vector2 position)
    {
        if (!isActiveAndEnabled || IsSelected || TransitionManager.IsBusy || BattleExecuteManager.IsExecuting ||
            BattleSelectionPresentation.OverlayBlocksPointer(position)) return;
        if (group != null) group.Select(this); else Select();
        // Mouse 폴링과 EventSystem이 같은 클릭을 전달해도 선택 이벤트는 한 번만 발생합니다.
        onSelected?.Invoke();
    }

    private bool IsPointerInsideCollider(Vector2 pointerPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
        Plane plane = new Plane(Vector3.forward, transform.position);

        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);

        return targetCollider.OverlapPoint(worldPoint);
    }

    public void Select()
    {
        IsSelected = true;
        Paint(true);
    }

    public void Deselect()
    {
        IsSelected = false;
        Paint(true);
    }

    private void Paint(bool instant)
    {
        // AT/DF 공통 표현: 기본은 어두운 바탕, 선택은 청록 바탕 + 어두운 글자입니다.
        Color background = IsSelected ? UITheme.Accent : hovered ? UITheme.BgPanelHi : UITheme.BgBase;
        Color foreground = IsSelected ? UITheme.BgBase : hovered ? UITheme.Accent : UITheme.TextMain;
        float t = instant ? 1f : Mathf.Clamp01(Time.unscaledDeltaTime / UITheme.ButtonTime);
        if (targetRenderer != null) targetRenderer.color = Color.Lerp(targetRenderer.color, background, t);
        if (targetText != null) targetText.color = Color.Lerp(targetText.color, foreground, t);
    }
}
