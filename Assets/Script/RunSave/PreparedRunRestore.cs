using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public sealed class PreparedRunRestore
{
    private readonly RunSaveSnapshot snapshot;
    private readonly RunProgression progression;
    private readonly RunCacheInventory inventory;

    private PreparedRunRestore(RunSaveSnapshot snapshot, RunProgression progression, RunCacheInventory inventory)
    {
        this.snapshot = snapshot;
        this.progression = progression;
        this.inventory = inventory;
    }

    public void Apply()
    {
        if (snapshot.state != "Active") throw new InvalidOperationException("An ended run cannot be restored.");
        MapRunState.ApplyRestored(snapshot, progression, inventory);
    }

    public static bool TryPrepare(RunSaveSnapshot source, RunSaveBindings bindings,
        out PreparedRunRestore prepared, out RunSaveError error, bool allowEnded = false)
    {
        prepared = null;
        error = RunSaveError.InvalidData;
        if (source == null || (source.schemaVersion != 1 && source.schemaVersion != 2) || !Guid.TryParseExact(source.runId, "N", out _) ||
            !DateTimeOffset.TryParseExact(source.savedUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            (source.state != "Active" && source.state != "Ended") || source.operatorIds == null ||
            source.ownedCacheIds == null || source.purchasedCacheIds == null || source.visitedNodeIds == null ||
            source.completedNodeIds == null || source.operatorIds.Length != 3 || source.lastBattleNodeId < 0 ||
            (source.lastBattleResult != "None" && source.lastBattleResult != "Victory" && source.lastBattleResult != "Defeat"))
            return false;
        if (source.state == "Active" && !string.IsNullOrEmpty(source.endReason)) return false;
        if (source.state == "Ended" && !allowEnded) { error = RunSaveError.RunEnded; return false; }
        if (source.state == "Ended" && source.endReason != "Ending" && source.endReason != "Ejected") return false;
        if (source.schemaVersion == 2 &&
            (source.integrity < 0 || source.integrity > 100 || source.consistency < 0 || source.consistency > 100 ||
             (source.state == "Active" && source.integrity == 0) ||
             (source.endReason == "Ejected" && source.integrity != 0))) return false;
        error = RunSaveError.IncompatibleData;
        if (bindings == null || source.mapScenePath != bindings.MapScenePath || !bindings.HasNode(source.currentNodeId)) return false;
        var positions = new HashSet<OperatorPosition>();
        foreach (string id in source.operatorIds)
        {
            OperatorData op = bindings.Operator(id);
            if (op == null || !positions.Add(op.Position)) return false;
        }
        var owned = new List<CacheData>();
        var ownedIds = new HashSet<string>(StringComparer.Ordinal);
        var purchased = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in source.ownedCacheIds)
        {
            CacheData cache = bindings.Cache(id);
            if (cache == null || !cache.IsConfigured || !ownedIds.Add(id)) return false;
            owned.Add(cache);
        }
        foreach (string id in source.purchasedCacheIds)
            if (bindings.Cache(id) == null || !purchased.Add(id)) return false;
        var visited = new HashSet<int>(source.visitedNodeIds);
        var completed = new HashSet<int>(source.completedNodeIds);
        if (visited.Count != source.visitedNodeIds.Length || completed.Count != source.completedNodeIds.Length ||
            !visited.Contains(source.currentNodeId) || visited.Any(id => !bindings.HasNode(id)) ||
            completed.Any(id => !visited.Contains(id)) ||
            (source.lastBattleResult == "None" ? source.lastBattleNodeId != 0 : !visited.Contains(source.lastBattleNodeId))) return false;
        var run = new RunProgression();
        if (!run.TryRestore(bindings.Progression, source.packets, source.layer, source.experience)) return false;
        var caches = new RunCacheInventory();
        caches.RestoreValidated(owned, source.purchasedCacheIds);
        // Detach from the caller before the asynchronous scene transition begins.
        var copy = JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(source));
        if (copy.schemaVersion == 1) { copy.integrity = 100; copy.consistency = 100; }
        copy.schemaVersion = 2;
        prepared = new PreparedRunRestore(copy, run, caches);
        error = RunSaveError.None;
        return true;
    }
}
