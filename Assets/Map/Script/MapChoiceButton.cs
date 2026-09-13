using UnityEngine;
using UnityEngine.InputSystem;

public class MapChoiceButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hitArea;
    [SerializeField] private Canvas canvas;
    [SerializeField] private MapNodeFocusObject focusObject;

    [Header("Choice")]
    [SerializeField] private int choiceIndex;

    private void Awake()
    {
        if (hitArea == null)
        {
            hitArea = GetComponent<RectTransform>();
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (focusObject == null)
        {
            focusObject = FindFirstObjectByType<MapNodeFocusObject>();
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        if (!IsMouseOver())
            return;

        if (focusObject == null)
            return;

        focusObject.SelectChoice(choiceIndex);
    }

    private bool IsMouseOver()
    {
        if (hitArea == null)
            return false;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Camera uiCamera =
            GetUICamera();

        return RectTransformUtility.RectangleContainsScreenPoint(
            hitArea,
            mousePosition,
            uiCamera
        );
    }

    private Camera GetUICamera()
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}