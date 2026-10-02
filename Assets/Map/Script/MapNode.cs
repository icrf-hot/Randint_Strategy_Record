using UnityEngine;

using Randint.Data;

public enum MapNodeType
{
    Start,
    Battle,
    EliteBattle,
    Event,
    Shop,
    Rest,
    FirstBoss,
    SecondBoss,
    ThridBoss,
    Boss
}

public enum MapNodeVisualState
{
    Normal,
    Current,
    Unavailable,
    ReachablePreview
}

public class MapNode : MonoBehaviour
{
    [Header("Node Info")]

    [SerializeField, GameTextKey]
    private string displayNameKey;
    [SerializeField] private int nodeID;
    [SerializeField] private int floorIndex;
    [SerializeField] private MapNodeType nodeType;

    [Header("Connections")]
    [SerializeField] private MapNode[] connectedNodes;

    [Header("Description")]
    [SerializeField, GameTextKey] private string descriptionKey;

    [Header("Choices")]
    [SerializeField] private MapChoice[] choices;

    [Header("Visual")]
    [SerializeField] private MeshRenderer meshRenderer;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color currentColor = Color.blue;
    [SerializeField] private Color unavailableColor = Color.gray;
    [SerializeField] private Color reachablePreviewColor = Color.green;

    private bool isUnavailable;
    private MapNodeVisualState visualState;

    public int NodeID => nodeID;
    public int FloorIndex => floorIndex;
    public MapNodeType NodeType => nodeType;
    public MapNode[] ConnectedNodes => connectedNodes;
    public string Description => GameData.Text(descriptionKey);
    public MapChoice[] Choices => choices;
    public string DisplayName => GameData.Text(displayNameKey);

    public bool IsUnavailable => isUnavailable;

    private void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        RefreshDefaultVisual(false);
    }

    public void SetUnavailable()
    {
        isUnavailable = true;
    }

    public void SetAvailable()
    {
        isUnavailable = false;
    }

    public void SetVisualState(MapNodeVisualState state)
    {
        visualState = state;
        ApplyColor();
    }

    public void RefreshDefaultVisual(bool isCurrentNode)
    {
        if (isCurrentNode)
        {
            SetVisualState(MapNodeVisualState.Current);
            return;
        }

        if (isUnavailable)
        {
            SetVisualState(MapNodeVisualState.Unavailable);
            return;
        }

        SetVisualState(MapNodeVisualState.Normal);
    }

    private void ApplyColor()
    {
        if (meshRenderer == null)
            return;

        Color color = normalColor;

        switch (visualState)
        {
            case MapNodeVisualState.Current:
                color = currentColor;
                break;

            case MapNodeVisualState.Unavailable:
                color = unavailableColor;
                break;

            case MapNodeVisualState.ReachablePreview:
                color = reachablePreviewColor;
                break;

            case MapNodeVisualState.Normal:
                color = normalColor;
                break;
        }

        meshRenderer.material.color = color;
    }

    public bool IsConnectedTo(MapNode node)
    {
        if (node == null)
            return false;

        if (connectedNodes == null)
            return false;

        foreach (MapNode connected in connectedNodes)
        {
            if (connected == node)
                return true;
        }

        return false;
    }
}
