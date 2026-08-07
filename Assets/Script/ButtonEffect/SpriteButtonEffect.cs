using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class SpriteButtonEffect : MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Collider2D targetCollider;
    [SerializeField] private Color hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private float colorChangeTime = 0.2f;

    private Color originalColor;
    private bool isHovering;

    private Coroutine colorCoroutine;
    private Camera mainCamera;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        if (targetRenderer != null)
            originalColor = targetRenderer.color;

        mainCamera = Camera.main;
    }

    private void OnDisable()
    {
        isHovering = false;
        StopColorChange();
        SetColor(originalColor);
    }

    private void Update()
    {
        if (mainCamera == null || targetCollider == null || Mouse.current == null)
            return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        bool isPointerInside = IsPointerInsideCollider(pointerPosition);

        if (!isPointerInside)
        {
            if (isHovering)
                StartColorChange(originalColor);

            isHovering = false;
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartColorChange(originalColor);
            return;
        }

        if (!isHovering)
        {
            isHovering = true;
            StartColorChange(hoverColor);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //StartColorChange(originalColor);
        //ignoreHoverUntilExit = true;
    }

    private bool IsPointerInsideCollider(Vector2 pointerPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
        Plane buttonPlane = new Plane(Vector3.forward, transform.position);

        if (!buttonPlane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        return targetCollider.OverlapPoint(worldPoint);
    }

    private void StartColorChange(Color targetColor)
    {
        if (targetRenderer == null)
            return;

        StopColorChange();
        colorCoroutine = StartCoroutine(ChangeColor(targetColor));
    }

    private void StopColorChange()
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
    }

    private void SetColor(Color color)
    {
        if (targetRenderer != null)
            targetRenderer.color = color;
    }

    private IEnumerator ChangeColor(Color targetColor)
    {
        Color startColor = targetRenderer.color;
        float elapsed = 0f;

        while (elapsed < colorChangeTime)
        {
            elapsed += Time.deltaTime;

            targetRenderer.color = Color.Lerp(
                startColor,
                targetColor,
                elapsed / colorChangeTime);

            yield return null;
        }

        targetRenderer.color = targetColor;
        colorCoroutine = null;
    }
}
