
public static class MapRunState
{
    public static bool HasCurrentNode { get; private set; }
    public static int CurrentNodeID { get; private set; }

    public static bool IsEnteringMapFromBattle
    {
        get;
        private set;
    }

    public static string CurrentNodeDisplayName
    {
        get;
        private set;
    }

    public static void SaveCurrentNode(int nodeID, string displayName)
    {
        CurrentNodeID = nodeID;
        CurrentNodeDisplayName = displayName;
        HasCurrentNode = true;
    }

    public static void BeginMapEntry()
    {
        IsEnteringMapFromBattle = true;
    }

    public static void CompleteMapEntry()
    {
        IsEnteringMapFromBattle = false;
    }

    public static void Reset()
    {
        CurrentNodeID = 0;
        HasCurrentNode = false;
        IsEnteringMapFromBattle = false;
        CurrentNodeDisplayName = string.Empty;
    }
}