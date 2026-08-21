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

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        // 마우스 누름
        if (mouse.leftButton.wasPressedThisFrame)
        {
            mouseDownPosition =
                mouse.position.ReadValue();

            possibleClick = true;
        }

        // 마우스 이동
        if (possibleClick)
        {
            Vector2 currentPosition =
                mouse.position.ReadValue();

            Vector2 delta =
                currentPosition - mouseDownPosition;

            if (delta.sqrMagnitude >
                dragThreshold * dragThreshold)
            {
                possibleClick = false;
            }
        }

        // 마우스 놓음
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (possibleClick)
            {
                TryClickNode(
                    mouse.position.ReadValue()
                );
            }

            possibleClick = false;
        }
    }

    private void TryClickNode(Vector2 mousePosition)
    {
        Camera mapCamera =
            orbitCamera.GetComponent<Camera>();

        if (mapCamera == null)
        {
            Debug.LogError(
                "MapNodeClickHandler: " +
                "MapOrbitCamera에 Camera가 없습니다."
            );

            return;
        }

        Ray ray =
            mapCamera.ScreenPointToRay(
                mousePosition
            );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit))
        {
            return;
        }

        MapNode node =
            hit.collider.GetComponent<MapNode>();

        if (node == null)
            return;


        // 이미 Focus된 상태
        if (orbitCamera.IsFocused)
        {
            // 현재 Focus된 노드가 아니면 무시
            if (node != orbitCamera.FocusedNode)
            {
                return;
            }

            // 같은 노드라면 FocusNode() 내부에서
            // CancelFocus()를 실행
            orbitCamera.FocusNode(node);

            return;
        }


        // 아직 Focus되지 않은 상태
        orbitCamera.FocusNode(node);
    }
}