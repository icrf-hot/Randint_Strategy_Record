using UnityEngine;
using UnityEngine.InputSystem;

public class OperatorClickHandler : MonoBehaviour
{
    [SerializeField] private Collider2D targetCollider;

    private Camera mainCamera;
    private Operator operatorData;

    public OperatorPosition Position
    {
        get
        {
            if (operatorData == null)
            {
                operatorData =
                    GetComponent<Operator>();
            }

            if (operatorData == null)
                return OperatorPosition.Front;

            return operatorData.Position;
        }
    }

    private void Awake()
    {
        operatorData =
            GetComponent<Operator>();

        if (targetCollider == null)
        {
            targetCollider =
                GetComponent<Collider2D>();
        }

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

        if (operatorData == null)
            return;

        if (OperatorFocusManager.Instance == null)
            return;

        OperatorFocusManager.Instance.EnterFocus(this);
    }

    private bool IsPointerInsideCollider()
    {
        if (mainCamera == null ||
            targetCollider == null)
        {
            return false;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(mousePosition);

        Plane plane =
            new Plane(
                Vector3.forward,
                transform.position);

        if (!plane.Raycast(
            ray,
            out float distance))
        {
            return false;
        }

        Vector3 worldPoint =
            ray.GetPoint(distance);

        return targetCollider.OverlapPoint(worldPoint);
    }
}