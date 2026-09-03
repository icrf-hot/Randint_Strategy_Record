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

[System.Serializable]
public class MapNodeChoice
{
    [TextArea(2, 5)]
    public string text;
}

public class MapNode : MonoBehaviour
{
    [Header("Node Info")]
    [SerializeField] private int nodeID;
    [SerializeField] private MapNodeType nodeType;

    [Header("Connections")]
    [SerializeField] private MapNode[] connectedNodes;

    [Header("Choices")]
    [SerializeField] private MapNodeChoice[] choices;

    public int NodeID => nodeID;
    public MapNodeType NodeType => nodeType;

    public MapNode[] ConnectedNodes =>
        connectedNodes;

    public MapNodeChoice[] Choices =>
        choices;

    public bool IsConnectedTo(MapNode node)
    {
        if (node == null)
            return false;

        foreach (MapNode connected in connectedNodes)
        {
            if (connected == node)
                return true;
        }

        return false;
    }
}