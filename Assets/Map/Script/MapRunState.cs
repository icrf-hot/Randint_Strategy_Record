
using System;
using System.Collections.Generic;
using System.Linq;

public static class MapRunState
{
    public static RunProgression Progression { get; private set; } = new RunProgression();
    public static EncounterJournal Encounters { get; } = new EncounterJournal();
    public static PacketWallet Packets => Progression.Packets;
    public static RunCacheInventory Caches { get; private set; } = new RunCacheInventory();
    public static RunRecordHealth RecordHealth { get; } = new RunRecordHealth();
    private static readonly HashSet<int> visitedNodes = new HashSet<int>();
    private static readonly HashSet<int> completedNodes = new HashSet<int>();
    private static string[] partyIds = Array.Empty<string>();
    public static string RunId { get; private set; } = "";
    public static bool IsEnded { get; private set; }
    public static string EndReason { get; private set; } = "";
    public static int LastBattleNodeId { get; private set; }
    public static string LastBattleResult { get; private set; } = "None";
    public static IReadOnlyList<string> PartyIds => Array.AsReadOnly(partyIds);
    public static int MapFloor { get; private set; } = 1;
    public static MapNodeType BattleNodeType { get; private set; } = MapNodeType.Battle;
    public static void SetBattleNodeType(MapNodeType type) => BattleNodeType = type;
    public static bool IsCompleted(int nodeId) => completedNodes.Contains(nodeId);
    public static void CompleteNode(int nodeId) { if (visitedNodes.Contains(nodeId)) completedNodes.Add(nodeId); }
    public static bool TryAdvanceMapFloor()
    {
        int terminal = MapFloor == 1 ? 8 : CampaignDefinition.NodeId(MapFloor, (int)Math.Pow(CampaignDefinition.Active.Floor(MapFloor).size, 3)-1);
        if (IsEnded || MapFloor >= 5 || CurrentNodeID != terminal || !IsCompleted(terminal)) return false;
        MapFloor++; CurrentNodeID = CampaignDefinition.NodeId(MapFloor, 0); HasCurrentNode = true;
        CurrentNodeDisplayName = ""; visitedNodes.Add(CurrentNodeID); BattleNodeType = MapNodeType.Battle;
        return true;
    }

    public static void RecordBattleResult(bool victory)
    {
        if (!HasCurrentNode) return;
        LastBattleNodeId = CurrentNodeID;
        LastBattleResult = victory ? "Victory" : "Defeat";
        if (victory) completedNodes.Add(CurrentNodeID);
    }

    public static RunSaveSnapshot Capture(RunSaveBindings bindings)
    {
        return new RunSaveSnapshot
        {
            schemaVersion = 4, mapFloor = MapFloor, runId = RunId, savedUtc = DateTimeOffset.UtcNow.ToString("O"),
            state = IsEnded ? "Ended" : "Active", endReason = EndReason,
            mapScenePath = bindings.MapScenePath, currentNodeId = CurrentNodeID,
            packets = Packets.Balance, layer = Progression.Layer, experience = Progression.Experience,
            integrity = RecordHealth.Integrity, consistency = RecordHealth.Consistency,
            operatorIds = (string[])partyIds.Clone(),
            ownedCacheIds = Caches.Owned.Select(cache => cache.CacheId).ToArray(),
            purchasedCacheIds = Caches.PurchasedIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            visitedNodeIds = visitedNodes.OrderBy(id => id).ToArray(),
            completedNodeIds = completedNodes.OrderBy(id => id).ToArray(),
            encounters = Encounters.Export(),
            lastBattleNodeId = LastBattleNodeId, lastBattleResult = LastBattleResult
        };
    }

    public static void ApplyRestored(RunSaveSnapshot snapshot, RunProgression run, RunCacheInventory inventory)
    {
        Progression = run;
        Encounters.Restore(snapshot.encounters);
        MapFloor = snapshot.mapFloor;
        Caches = inventory;
        RecordHealth.Restore(snapshot.integrity, snapshot.consistency);
        RunId = snapshot.runId;
        IsEnded = false;
        EndReason = "";
        partyIds = (string[])snapshot.operatorIds.Clone();
        visitedNodes.Clear();
        completedNodes.Clear();
        visitedNodes.UnionWith(snapshot.visitedNodeIds);
        completedNodes.UnionWith(snapshot.completedNodeIds);
        CurrentNodeID = snapshot.currentNodeId;
        CurrentNodeDisplayName = "";
        HasCurrentNode = true;
        IsEnteringMapFromBattle = false;
        LastBattleNodeId = snapshot.lastBattleNodeId;
        LastBattleResult = snapshot.lastBattleResult;
    }

    public static void MarkEnded(string reason)
    {
        if (reason != "Ending" && reason != "Ejected") throw new ArgumentException(nameof(reason));
        IsEnded = true;
        EndReason = reason;
    }

    public static bool TryGrantVictory(out int packetsGained, out int layersGained)
    {
        if (IsEnded) { packetsGained = 0; layersGained = 0; return false; }
        long packets = 0, experience = 0;
        foreach (CacheData cache in Caches.Owned)
        {
            if (cache == null || !cache.IsConfigured || cache.EffectKind != CacheEffectKind.GlobalPacketAndExperience)
                continue;
            packets += cache.PacketGainPercent;
            experience += cache.ExperienceGainPercent;
        }
        packetsGained = 0;
        layersGained = 0;
        return packets <= int.MaxValue && experience <= int.MaxValue &&
            Progression.TryGrantVictory(out packetsGained, out layersGained, (int)packets, (int)experience);
    }

    public static CachePurchaseResult TryPurchaseCache(ShopCatalogData catalog, string cacheId)
    {
        if (!Progression.IsInitialized || IsEnded)
            return CachePurchaseResult.RunNotStarted;
        return Caches.TryPurchase(Packets, catalog, cacheId);
    }

    public static bool EnsureStarted(RunProgressionSettings settings)
    {
        return Progression.IsInitialized || StartNewRun(settings);
    }

    public static bool StartNewRun(RunProgressionSettings settings)
    {
        if (settings == null || !settings.IsValid)
            return false;

        Reset();
        RunId = Guid.NewGuid().ToString("N");
        partyIds = RunSaveBindings.Instance != null ? RunSaveBindings.Instance.DefaultPartyIds : Array.Empty<string>();
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
        visitedNodes.Add(nodeID);
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
        Encounters.Clear();
        Caches.Clear();
        RecordHealth.Reset();
        CurrentNodeID = 0;
        HasCurrentNode = false;
        IsEnteringMapFromBattle = false;
        CurrentNodeDisplayName = string.Empty;
        RunId = "";
        IsEnded = false;
        EndReason = "";
        partyIds = Array.Empty<string>();
        visitedNodes.Clear();
        completedNodes.Clear();
        LastBattleNodeId = 0;
        LastBattleResult = "None";
        MapFloor = 1; BattleNodeType = MapNodeType.Battle;
    }
}
