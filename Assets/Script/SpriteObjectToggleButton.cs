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

    private void Awake()
    {
        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        mainCamera = Camera.main;
    }

    private void Update()
    {
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