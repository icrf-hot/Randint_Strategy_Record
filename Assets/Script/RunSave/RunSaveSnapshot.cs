using System;

[Serializable]
public sealed class RunSaveSnapshot
{
    public int schemaVersion;
    public string runId;
    public string savedUtc;
    public string state;
    public string endReason;
    public string mapScenePath;
    public int currentNodeId = -1;
    public int packets = -1;
    public int layer = -1;
    public int experience = -1;
    public int integrity = -1;
    public int consistency = -1;
    public string[] operatorIds;
    public string[] ownedCacheIds;
    public string[] purchasedCacheIds;
    public int[] visitedNodeIds;
    public int[] completedNodeIds;
    public int lastBattleNodeId;
    public string lastBattleResult;
}

public enum RunSaveError
{
    None, NotFound, NotAllowed, InvalidData, IncompatibleData, RunEnded, IoError
}

public static class RunSaveMessages
{
    public static string Error(RunSaveError error) =>
        Randint.Data.GameData.Text("save.error." + error.ToString().ToLowerInvariant());
}
