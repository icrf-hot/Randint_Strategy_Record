using UnityEngine;

using Randint.Data;

public enum MapNodeType
{
    Start = 0,
    Battle = 1,
    EliteBattle = 2,
    Event = 3,
    Shop = 4,
    Rest = 5,
    FirstBoss = 6,
    SecondBoss = 7,
    ThirdBoss = 8,
    Boss = 9
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

    private Color normalColor {
        get {
            string kind = meshRenderer != null ? meshRenderer.name.ToLowerInvariant() : name.ToLowerInvariant();
            return kind.Contains("queen") ? UITheme.Danger : kind.Contains("king") ? UITheme.Warn :
                kind.Contains("pawn") ? UITheme.TextDim : UITheme.Accent;
        }
    }
    private readonly Color currentColor = UITheme.Ok;
    private readonly Color unavailableColor = UITheme.Line;
    private readonly Color reachablePreviewColor = UITheme.Accent;

    private MaterialPropertyBlock themeProperties;
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

        if (themeProperties == null) themeProperties = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(themeProperties);
        themeProperties.SetColor("_BaseColor", color);
        themeProperties.SetColor("_Color", color);
        meshRenderer.SetPropertyBlock(themeProperties);
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
