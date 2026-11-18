using UnityEngine;
using UnityEngine.InputSystem;

public enum PositionType
{
    Front,
    Middle,
    Back
}

public class SpriteObjectToggleButton : MonoBehaviour
{
    [SerializeField] private Collider2D targetCollider;

    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;

    [SerializeField] private PositionType positionType;

    private Camera mainCamera;
    public PositionType Position => positionType;
    public bool SlotAvailable { get; private set; } = true;
    private SpriteRenderer buttonVisual;
    private Color activeColor;

    private void Awake()
    {
        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        mainCamera = Camera.main;
        buttonVisual = GetComponent<SpriteRenderer>();
        if (buttonVisual != null) activeColor = buttonVisual.color;
    }

    public void SetSlotAvailable(bool available)
    {
        if (SlotAvailable == available) return;
        SlotAvailable = available;
        if (targetCollider != null) targetCollider.enabled = available;
        if (buttonVisual != null) buttonVisual.color = available ? activeColor : UITheme.TextDim;
        if (!available) SetObjectsActive(objectsToEnable, false);
    }

    private void Update()
    {
        if (!SlotAvailable || BattleExecuteManager.IsExecuting) return;
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (!IsPointerInsideCollider())
            return;

        ToggleObjects();
    }

    private bool IsPointerInsideCollider()
    {
        if (mainCamera == null || targetCollider == null)
            return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new Plane(Vector3.forward, transform.position);

        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);

        return targetCollider.OverlapPoint(worldPoint);
    }

    private void ToggleObjects()
    {
        if (!SlotAvailable || BattleExecuteManager.IsExecuting) return;
        SetObjectsActive(objectsToDisable, false);
        SetObjectsActive(objectsToEnable, true);

        Debug.Log($"Clicked: {gameObject.name}");

        switch (positionType)
        {
            case PositionType.Front:
                CardPositionManager.Instance.SetFront();
                break;

            case PositionType.Middle:
                CardPositionManager.Instance.SetMiddle();
                break;

            case PositionType.Back:
                CardPositionManager.Instance.SetBack();
                break;
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }


}
