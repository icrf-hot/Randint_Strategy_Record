using UnityEngine;
using UnityEngine.InputSystem;

public class MapNodeClickHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;

    [Header("Click")]
    [SerializeField] private float dragThreshold = 5f;

    private Vector2 mouseDownPosition;
    private bool possibleClick;

    private void Awake()
    {
        if (orbitCamera == null)
        {
            orbitCamera = FindFirstObjectByType<MapOrbitCamera>();
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            mouseDownPosition = mouse.position.ReadValue();
            possibleClick = true;
        }

        if (possibleClick)
        {
            Vector2 currentPosition = mouse.position.ReadValue();
            Vector2 delta = currentPosition - mouseDownPosition;

            if (delta.sqrMagnitude > dragThreshold * dragThreshold)
            {
                possibleClick = false;
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (possibleClick)
            {
                TryClickNode(mouse.position.ReadValue());
            }

            possibleClick = false;
        }
    }

    private void TryClickNode(Vector2 mousePosition)
    {
        if (orbitCamera == null)
            return;

        Camera mapCamera =
            orbitCamera.GetComponent<Camera>();

        if (mapCamera == null)
            return;

        Ray ray =
            mapCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        MapNode node =
            hit.collider.GetComponentInParent<MapNode>();

        if (node == null)
            return;

        orbitCamera.FocusNode(node);
    }
}