
public static class MapRunState
{
    public static RunProgression Progression { get; } = new RunProgression();
    public static PacketWallet Packets => Progression.Packets;

    public static bool EnsureStarted(RunProgressionSettings settings)
    {
        return Progression.IsInitialized || Progression.Start(settings);
    }

    public static bool StartNewRun(RunProgressionSettings settings)
    {
        if (settings == null || !settings.IsValid)
            return false;

        Reset();
        return Progression.Start(settings);
    }

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
        Progression.Clear();
        CurrentNodeID = 0;
        HasCurrentNode = false;
        IsEnteringMapFromBattle = false;
        CurrentNodeDisplayName = string.Empty;
    }
}
