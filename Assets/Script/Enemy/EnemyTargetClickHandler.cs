using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyTargetClickHandler : MonoBehaviour
{
    [SerializeField] private Collider2D targetCollider;

    private Camera mainCamera;
    private EnemyTargetable targetable;

    private void Awake()
    {
        targetable =
            GetComponent<EnemyTargetable>();

        if (targetCollider == null)
            targetCollider =
                GetComponent<Collider2D>();

        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (targetable == null)
            return;

        if (!targetable.CanBeTargeted)
            return;

        if (!IsPointerInsideCollider())
            return;

        EnemyTargetingManager.Instance
            .SelectTarget(targetable);
    }

    private bool IsPointerInsideCollider()
    {
        if (mainCamera == null ||
            targetCollider == null)
            return false;

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
            return false;

        Vector3 worldPoint =
            ray.GetPoint(distance);

        return targetCollider
            .OverlapPoint(worldPoint);
    }
}