using UnityEngine;

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

public class MapNode : MonoBehaviour
{
    [Header("Node Info")]
    [SerializeField] private int nodeID;
    [SerializeField] private MapNodeType nodeType;

    [Header("Connections")]
    [SerializeField] private MapNode[] connectedNodes;

    [Header("Description")]
    [TextArea(3, 10)]
    [SerializeField] private string description;

    [Header("Choices")]
    [SerializeField] private MapChoice[] choices;

    public int NodeID => nodeID;
    public MapNodeType NodeType => nodeType;
    public MapNode[] ConnectedNodes => connectedNodes;

    public string Description => description;
    public MapChoice[] Choices => choices;

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