using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class SpriteToggleButton : MonoBehaviour
{
    [Header("Group")]
    [SerializeField] private SpriteToggleGroup group;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Collider2D targetCollider;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.75f, 0.75f, 0.75f);

    [Header("TMP")]
    [SerializeField] private TMP_Text targetText;

    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.gray;

    [Header("Event")]
    [SerializeField] private UnityEvent onSelected;

    private Camera mainCamera;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null || targetCollider == null || Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (!IsPointerInsideCollider(mousePosition))
            return;

        group.Select(this);

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
        if (targetRenderer != null)
            targetRenderer.color = selectedColor;

        if (targetText != null)
            targetText.color = selectedTextColor;
    }

    public void Deselect()
    {
        if (targetRenderer != null)
            targetRenderer.color = normalColor;

        if (targetText != null)
            targetText.color = normalTextColor;
    }
}