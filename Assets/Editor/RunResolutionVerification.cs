using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class RunResolutionVerification
{
    public static string Run()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Edit Mode required.");
        var bindings = RunSaveBindings.Instance;
        var original = MapRunState.Progression.IsInitialized && MapRunState.HasCurrentNode && !MapRunState.IsEnded
            ? MapRunState.Capture(bindings) : null;
        string folder = Path.Combine(Path.GetTempPath(), "RSR_Resolution_" + Guid.NewGuid().ToString("N"));
        var repository = new RunSaveRepository(folder, bindings);
        using var isolated = RunSaveService.UseRepositoryForVerification(repository);
        try
        {
            StartBattle(1, 5, false, 100);
            MapRunState.RecordBattleResult(false);
            Require(RunRecordService.ApplyBattleDefeat(3), "Normal defeat rejected.");
            Require(MapRunState.IsCompleted(5) && !MapRunState.IsEnded && MapRunState.RecordHealth.Integrity == 70,
                "Defeated node was not consumed or survivor loss changed.");
            var oldDefeat = MapRunState.Capture(bindings);
            oldDefeat.schemaVersion = 5;
            oldDefeat.completedNodeIds = Array.Empty<int>();
            Require(PreparedRunRestore.TryPrepare(oldDefeat, bindings, out var migrated, out _), "Old defeated save rejected.");
            migrated.Apply();
            Require(MapRunState.IsCompleted(5) && oldDefeat.completedNodeIds.Length == 0, "Old defeat migration or source mutation.");

            StartBattle(3, CampaignDefinition.TerminalNodeId(3), false, 100);
            Require(repository.TryWrite(MapRunState.Capture(bindings), out _), "Terminal fixture save failed.");
            MapRunState.RecordBattleResult(false);
            RunRecordService.ApplyBattleDefeat(1);
            Require(MapRunState.IsEnded && MapRunState.EndReason == "TerminalDefeat" &&
                MapRunState.RecordHealth.Integrity == 90 && !MapRunState.TryAdvanceMapFloor(), "Normal boss defeat did not end run.");
            Require(!repository.TryRead(out _, out var ended) && ended == RunSaveError.RunEnded, "Terminal defeat can resume.");
            Require(repository.TryRead(out _, out _, true), "Terminal defeat end reason is not readable.");
            Require(!RunEndSummary.Capture().Cleared, "Defeat summary reports clear.");

            StartBattle(3, CampaignDefinition.TerminalNodeId(3), true, 100);
            MapRunState.RecordBattleResult(false);
            RunRecordService.ApplyBattleDefeat(1);
            Require(!MapRunState.IsEnded && MapRunState.RecordHealth.Integrity == 80 && MapRunState.TryAdvanceMapFloor() &&
                MapRunState.MapFloor == 4, "Hidden third-floor defeat did not advance with double loss.");
            var hidden = MapRunState.Capture(bindings);
            Require(PreparedRunRestore.TryPrepare(hidden, bindings, out var hiddenRestore, out _), "Hidden route save rejected.");
            hiddenRestore.Apply();
            Require(MapRunState.HiddenEndingRouteUnlocked && MapRunState.EndingFloor == 5, "Hidden route flag lost.");
            hidden.hiddenEndingRouteUnlocked = false;
            Require(!PreparedRunRestore.TryPrepare(hidden, bindings, out _, out _), "Upper floor accepted without route flag.");

            StartBattle(3, CampaignDefinition.TerminalNodeId(3), true, 20);
            MapRunState.RecordBattleResult(false);
            RunRecordService.ApplyBattleDefeat(1);
            Require(MapRunState.IsEnded && MapRunState.EndReason == "Ejected" && !MapRunState.TryAdvanceMapFloor(),
                "Lethal hidden-boss loss advanced the run.");

            StartBattle(3, CampaignDefinition.TerminalNodeId(3), false, 100);
            MapRunState.RecordBattleResult(true);
            Require(MapRunState.EndingFloor == 3 && !MapRunState.TryAdvanceMapFloor(), "Normal route passes its ending.");
            MapRunState.MarkEnded("Ending");
            Require(RunEndSummary.Capture().Cleared, "Clear summary reports defeat.");

            var encounter = EncounterVerification.Fixture(2);
            MapRunState.RecordHealth.Restore(95, 96);
            encounter.selectedMask = EncounterJournal.Solution(encounter);
            int before = MapRunState.Packets.Balance;
            Require(EncounterService.Execute(encounter, EncounterAction.Verify, out _), "Temporary reward proof rejected.");
            Require(EncounterService.Execute(encounter, EncounterAction.Claim, out _), "Temporary reward claim rejected.");
            Require(MapRunState.RecordHealth.Integrity == 97 && MapRunState.RecordHealth.Consistency == 98 &&
                MapRunState.Packets.Balance == before - encounter.tier.verifyCost, "Temporary +2/+2 reward or cost mismatch.");
            Require(!EncounterService.Execute(encounter, EncounterAction.Claim, out _), "Temporary reward repeated.");
            Require(PreparedRunRestore.TryPrepare(MapRunState.Capture(bindings), bindings, out _, out _), "Temporary reward save rejected.");

            encounter = EncounterVerification.Fixture(2);
            encounter.tier.temporaryRewardHp = 0;
            encounter.selectedMask = EncounterJournal.Solution(encounter);
            EncounterService.Execute(encounter, EncounterAction.Verify, out _);
            var legacy = MapRunState.Capture(bindings);
            legacy.schemaVersion = 5;
            Require(PreparedRunRestore.TryPrepare(legacy, bindings, out var legacyRestore, out _), "Legacy verified reward rejected.");
            legacyRestore.Apply();
            encounter = MapRunState.Encounters.Find(encounter.nodeId);
            before = MapRunState.Packets.Balance;
            EncounterService.Execute(encounter, EncounterAction.Claim, out _);
            Require(MapRunState.Packets.Balance == before + 8, "Saved reward promise was replaced by placeholder.");

            MapRunState.StartNewRun(bindings.Progression);
            MapRunState.SaveCurrentNode(1, "Summary fixture");
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
            MapRunState.Packets.TryAdd(100);
            Require(MapRunState.TryPurchaseCache(catalog, "cache_all_stats") == CachePurchaseResult.Success, "Summary purchase fixture failed.");
            Require(MapRunState.Caches.Grant(bindings.Cache("defender_relic")), "Summary grant fixture failed.");
            MapRunState.MarkEnded("Ending");
            var summary = RunEndSummary.Capture();
            Require(summary.Purchased.Count == 1 && summary.Owned.Count == 2 && summary.Operators.Count == 3,
                "Summary omitted owned/granted items or party.");
            foreach (var op in summary.Operators)
            {
                var percentages = op.Stats.Where(stat => stat.Percent.HasValue).Select(stat => stat.Percent.Value).ToArray();
                Require(percentages.SequenceEqual(percentages.OrderByDescending(value => value)), "Stat changes are not sorted.");
                Require(op.Stats.Single(stat => stat.Key == "HP").Bonus == 250, "Summary bonuses disagree with inventory.");
            }
            Require(summary.Operators.Any(op => op.Stats.Any(stat => stat.Key == "RES" && !stat.Percent.HasValue)),
                "Zero-base resistance was shown as an invented percentage.");
            return "PASS: consumed defeats, old-save migration, normal/hidden endings, double-loss continuation, lethal precedence, temporary/legacy rewards, summary ownership and sorted stat percentages.";
        }
        finally
        {
            repository.TryDelete(out _);
            if (Directory.Exists(folder)) Directory.Delete(folder, false);
            MapRunState.Reset();
            if (original != null && PreparedRunRestore.TryPrepare(original, bindings, out var previous, out _)) previous.Apply();
        }
    }

    private static void StartBattle(int floor, int node, bool hidden, int integrity)
    {
        EncounterVerification.Fixture(floor);
        if (hidden) MapRunState.TryUnlockHiddenEndingRoute();
        MapRunState.SaveCurrentNode(node, "Battle fixture");
        MapRunState.RecordHealth.Restore(integrity, 100);
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
