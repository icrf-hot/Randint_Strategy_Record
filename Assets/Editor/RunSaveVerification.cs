using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class RunSaveVerification
{
    public static void RunFinal()
    {
        MergedJsonVerification.Run();
        Run();
    }

    [Serializable] private sealed class Envelope
    {
        public int version;
        public string payload;
        public string checksum;
    }

    public static void Run()
    {
        RunSaveBindings bindings = RunSaveBindings.Instance;
        Require(bindings != null && bindings.DefaultPartyIds.Length == 3, "Save bindings are missing.");
        string directory = Path.Combine(Path.GetTempPath(), "RSR_SaveVerify_" + Guid.NewGuid().ToString("N"));
        var repository = new RunSaveRepository(directory, bindings);
        using var isolated = RunSaveService.UseRepositoryForVerification(repository);
        try
        {
            Require(!repository.TryRead(out _, out RunSaveError empty) && empty == RunSaveError.NotFound, "Missing slot not detected.");
            Require(MapRunState.StartNewRun(bindings.Progression), "Run initialization failed.");
            MapRunState.SaveCurrentNode(1, "Start");
            MapRunState.SaveCurrentNode(2, "Event");
            MapRunState.SaveCurrentNode(5, "Battle");
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
            Require(MapRunState.TryPurchaseCache(catalog, "cache_hp") == CachePurchaseResult.Success, "HP fixture purchase failed.");
            MapRunState.TryGrantVictory(out _, out _);
            Require(MapRunState.TryPurchaseCache(catalog, "cache_rewards") == CachePurchaseResult.Success, "Reward fixture purchase failed.");
            MapRunState.TryGrantVictory(out _, out _);
            MapRunState.RecordBattleResult(true);
            RunSaveSnapshot snapshot = MapRunState.Capture(bindings);
            Require(snapshot.packets == 24 && snapshot.layer == 2 && snapshot.experience == 14, "Fixture economy is incorrect.");
            Require(repository.TryWrite(snapshot, out _), "Initial file write failed.");
            string savedFile = File.ReadAllText(repository.FilePath);
            var reopened = new RunSaveRepository(directory, bindings);
            Require(reopened.TryRead(out RunSaveSnapshot loaded, out _), "New repository could not reopen saved data.");
            MapRunState.Reset();
            Require(PreparedRunRestore.TryPrepare(loaded, bindings, out PreparedRunRestore prepared, out _), "Restore preparation failed.");
            Require(!MapRunState.Progression.IsInitialized, "Preparation changed current run.");
            prepared.Apply();
            Require(MapRunState.RunId == snapshot.runId && MapRunState.CurrentNodeID == 5 && MapRunState.Packets.Balance == 24 &&
                MapRunState.Progression.Layer == 2 && MapRunState.Progression.Experience == 14 &&
                MapRunState.Caches.Owned.Count == 2 && MapRunState.Caches.HasPurchased("cache_hp") &&
                MapRunState.PartyIds.SequenceEqual(snapshot.operatorIds) &&
                MapRunState.Capture(bindings).completedNodeIds.SequenceEqual(new[] { 5 }), "Round-trip lost run data.");
            Require(MapRunState.TryPurchaseCache(catalog, "cache_hp") == CachePurchaseResult.AlreadyPurchased, "Purchase history was lost.");
            Require(!RunSaveService.Save(out RunSaveError gated) && gated == RunSaveError.NotAllowed,
                "Non-Map scene accepted gameplay save.");

            foreach (Action<RunSaveSnapshot> corrupt in new Action<RunSaveSnapshot>[]
            {
                data => data.schemaVersion = 999,
                data => data.packets = -1,
                data => data.experience = bindings.Progression.RequiredExperience(data.layer),
                data => data.currentNodeId = 9999,
                data => data.operatorIds[0] = "unknown_operator",
                data => data.operatorIds[1] = data.operatorIds[0],
                data => data.ownedCacheIds[0] = "unknown_cache",
                data => data.purchasedCacheIds = new[] { "cache_hp", "cache_hp" },
                data => data.visitedNodeIds = new[] { 1, 1, 5 },
                data => data.completedNodeIds = new[] { 8 }
            })
            {
                RunSaveSnapshot invalid = Clone(snapshot);
                corrupt(invalid);
                Require(!repository.TryWrite(invalid, out _), "Invalid save was accepted.");
                Require(File.ReadAllText(repository.FilePath) == savedFile && MapRunState.Packets.Balance == 24,
                    "Invalid save changed existing file or runtime state.");
            }
            File.WriteAllText(repository.FilePath, "{broken");
            Require(!repository.TryRead(out _, out RunSaveError broken) && broken == RunSaveError.InvalidData,
                "Malformed JSON was accepted.");
            Require(repository.TryWrite(snapshot, out _), "Valid save could not replace corrupted file.");
            Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(repository.FilePath));
            envelope.payload += " ";
            File.WriteAllText(repository.FilePath, JsonUtility.ToJson(envelope));
            Require(!repository.TryRead(out _, out RunSaveError checksum) && checksum == RunSaveError.InvalidData,
                "Checksum mismatch was accepted.");
            RunSaveSnapshot unknown = Clone(snapshot);
            unknown.ownedCacheIds[0] = "removed_cache";
            WriteEnvelope(repository.FilePath, unknown);
            Require(!repository.TryRead(out _, out RunSaveError incompatible) && incompatible == RunSaveError.IncompatibleData,
                "Unknown runtime ID was accepted.");
            File.WriteAllText(repository.FilePath, new string('x', 1024 * 1024 + 1));
            Require(!repository.TryRead(out _, out RunSaveError oversized) && oversized == RunSaveError.InvalidData, "Oversized save was accepted.");
            Require(repository.TryWrite(snapshot, out _), "Recovery write failed.");
            Require(File.Exists(repository.FilePath + ".bak"), "Atomic replacement did not retain previous file.");
            Require(RunSaveService.MarkRunEnded("Ending", out _) && MapRunState.IsEnded, "End hook failed.");
            Require(!repository.TryRead(out _, out RunSaveError ended) && ended == RunSaveError.RunEnded,
                "Ended run can be continued.");
            Require(repository.TryRead(out RunSaveSnapshot terminal, out _, true) &&
                !PreparedRunRestore.TryPrepare(terminal, bindings, out _, out _), "Ended snapshot could be restored directly.");
            Require(PreparedRunRestore.TryPrepare(terminal, bindings, out PreparedRunRestore endedCandidate, out _, true),
                "Terminal data validation failed.");
            bool refused = false;
            try { endedCandidate.Apply(); } catch (InvalidOperationException) { refused = true; }
            Require(refused && MapRunState.IsEnded, "Validation-only terminal candidate revived a run.");
            File.WriteAllText(repository.FilePath + ".tmp", "interrupted");
            Require(repository.TryDelete(out _) && !repository.HasFiles && repository.TryDelete(out _),
                "Delete did not clear primary/backup/temp or was not idempotent.");
            Require(MapRunState.Caches.Owned.Count == 2, "Deleting saved slot altered live inventory.");
            string blocker = Path.Combine(directory, "blocker");
            File.WriteAllText(blocker, "file instead of directory");
            var blocked = new RunSaveRepository(blocker, bindings);
            Require(!blocked.TryWrite(snapshot, out RunSaveError io) && io == RunSaveError.IoError, "I/O failure was not reported.");
            File.Delete(blocker);
        }
        finally
        {
            MapRunState.Reset();
            repository.TryDelete(out _);
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
        Debug.Log("[Run Save Verification] Full run round-trip, validation, unchanged-on-failure, checksum, atomic replacement, terminal blocking, delete and I/O isolation passed.");
    }

    private static RunSaveSnapshot Clone(RunSaveSnapshot snapshot) => JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(snapshot));
    private static void WriteEnvelope(string path, RunSaveSnapshot snapshot)
    {
        string payload = JsonUtility.ToJson(snapshot);
        using var sha = SHA256.Create();
        File.WriteAllText(path, JsonUtility.ToJson(new Envelope
        { version = 1, payload = payload, checksum = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))) }));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
