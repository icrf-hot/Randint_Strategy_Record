using UnityEngine;
using UnityEngine.InputSystem;

public class MapPlayerPositionFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private MapNode startNode;

    [Header("Raycast")]
    [SerializeField] private LayerMask nodeLayerMask = ~0;
    [SerializeField] private float raycastDistance = 1000f;

    private MapNode currentNode;
    private bool showingReachableNodes;

    public MapNode CurrentNode => currentNode;

    private void Awake()
    {
        if (mapCamera == null)
        {
            mapCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (startNode != null)
        {
            SetCurrentNode(startNode);
        }
    }

    private void Update()
    {
        UpdateReachablePreview();
    }

    public void MoveToNode(MapNode nextNode)
    {
        if (nextNode == null)
            return;

        if (nextNode.IsUnavailable)
            return;

        if (currentNode != null &&
            !currentNode.IsConnectedTo(nextNode))
        {
            return;
        }

        SetCurrentNode(nextNode);
    }

    private void SetCurrentNode(MapNode nextNode)
    {
        HideReachableNodes();

        currentNode = nextNode;

        UpdateUnavailableNodesByFloor();
        RefreshAllNodeVisuals();
    }

    private void UpdateUnavailableNodesByFloor()
    {
        if (currentNode == null)
            return;

        MapNode[] nodes =
            FindObjectsByType<MapNode>(
                FindObjectsSortMode.None
            );

        int currentFloor =
            currentNode.FloorIndex;

        foreach (MapNode node in nodes)
        {
            if (node == null)
                continue;

            if (node == currentNode)
            {
                node.SetAvailable();
                continue;
            }

            if (node.FloorIndex < currentFloor)
            {
                node.SetUnavailable();
            }
        }
    }

    private void UpdateReachablePreview()
    {
        MapNode hoveredNode =
            FindNodeUnderMouse();

        bool shouldShowReachableNodes =
            hoveredNode != null &&
            hoveredNode == currentNode;

        if (shouldShowReachableNodes == showingReachableNodes)
            return;

        showingReachableNodes =
            shouldShowReachableNodes;

        if (showingReachableNodes)
        {
            ShowReachableNodes();
        }
        else
        {
            HideReachableNodes();
        }
    }

    private void ShowReachableNodes()
    {
        if (currentNode == null)
            return;

        MapNode[] connectedNodes =
            currentNode.ConnectedNodes;

        if (connectedNodes == null)
            return;

        foreach (MapNode node in connectedNodes)
        {
            if (node == null)
                continue;

            if (node.IsUnavailable)
                continue;

            node.SetVisualState(
                MapNodeVisualState.ReachablePreview
            );
        }

        currentNode.SetVisualState(
            MapNodeVisualState.Current
        );
    }

    private void HideReachableNodes()
    {
        RefreshAllNodeVisuals();
    }

    private MapNode FindNodeUnderMouse()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            mapCamera == null)
        {
            return null;
        }

        Vector2 mousePosition =
            mouse.position.ReadValue();

        Ray ray =
            mapCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            nodeLayerMask))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<MapNode>();
    }

    private void RefreshAllNodeVisuals()
    {
        MapNode[] nodes =
            FindObjectsByType<MapNode>(
                FindObjectsSortMode.None
            );

        foreach (MapNode node in nodes)
        {
            if (node == null)
                continue;

            node.RefreshDefaultVisual(
                node == currentNode
            );
        }
    }

    public bool CanOpenChoicesFor(MapNode node)
    {
        if (node == null)
            return false;

        if (node.IsUnavailable)
            return false;

        if (currentNode == null)
            return node == startNode;

        return currentNode.IsConnectedTo(node);
    }
}