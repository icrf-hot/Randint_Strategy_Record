using System;
using System.IO;
using UnityEngine;

public static class RunRecordVerification
{
    public static void RunFinal()
    {
        RunSaveVerification.RunFinal();
        Run();
    }

    public static void Run()
    {
        var health = new RunRecordHealth();
        Require(health.Integrity == 100 && health.Consistency == 100, "Initial health is incorrect.");
        Require(!health.ReduceIntegrity(-1) && !health.ReduceConsistency(-1), "Negative damage accepted.");
        health.ReduceConsistency(80);
        Require(health.Integrity == 100 && health.Consistency == 20, "Consistency damage hit integrity early.");
        health.ReduceConsistency(30);
        Require(health.Integrity == 90 && health.Consistency == 0, "Consistency overflow was lost.");
        health.ReduceConsistency(10);
        Require(health.Integrity == 80, "Zero consistency did not redirect damage.");
        health.ReduceIntegrity(int.MaxValue);
        Require(health.Integrity == 0 && health.Consistency == 0, "Damage did not clamp.");
        health.Reset();
        Require(health.Integrity == 100 && health.Consistency == 100, "Reset failed.");

        var bindings = RunSaveBindings.Instance;
        string directory = Path.Combine(Path.GetTempPath(), "RSR_RecordVerify_" + Guid.NewGuid().ToString("N"));
        var repository = new RunSaveRepository(directory, bindings);
        using var scope = RunSaveService.UseRepositoryForVerification(repository);
        try
        {
            Require(MapRunState.StartNewRun(bindings.Progression), "Run initialization failed.");
            MapRunState.SaveCurrentNode(5, "Battle");
            Require(!RunRecordService.ApplyBattleDefeat(-1), "Invalid survivor count accepted.");
            Require(RunRecordService.ApplyBattleDefeat(3), "Defeat damage failed.");
            Require(MapRunState.RecordHealth.Integrity == 70 && MapRunState.RecordHealth.Consistency == 100,
                "Three survivors must cost 30 integrity only.");
            RunRecordService.ReduceConsistency(15);
            var snapshot = MapRunState.Capture(bindings);
            Require(snapshot.schemaVersion == 2 && repository.TryWrite(snapshot, out _), "Health save failed.");
            MapRunState.Reset();
            Require(repository.TryRead(out var loaded, out _), "Health read failed.");
            Require(PreparedRunRestore.TryPrepare(loaded, bindings, out var restored, out _), "Health restore failed.");
            restored.Apply();
            Require(MapRunState.RecordHealth.Integrity == 70 && MapRunState.RecordHealth.Consistency == 85,
                "Health was not restored.");
            foreach (Action<RunSaveSnapshot> corrupt in new Action<RunSaveSnapshot>[]
            {
                data => data.integrity = -1, data => data.integrity = 101,
                data => data.consistency = -1, data => data.consistency = 101,
                data => data.integrity = 0
            })
            {
                var invalid = JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(snapshot));
                corrupt(invalid);
                Require(!PreparedRunRestore.TryPrepare(invalid, bindings, out _, out _), "Invalid health accepted.");
            }
            var legacy = JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(snapshot));
            legacy.schemaVersion = 1;
            legacy.integrity = legacy.consistency = -1;
            Require(PreparedRunRestore.TryPrepare(legacy, bindings, out var migrated, out _), "Legacy save rejected.");
            migrated.Apply();
            Require(MapRunState.RecordHealth.Integrity == 100 && MapRunState.RecordHealth.Consistency == 100 &&
                legacy.schemaVersion == 1 && legacy.integrity == -1, "Legacy migration changed source or defaults.");
            // The same run's previous slot must become unusable, not just the in-memory run.
            Require(RunRecordService.ApplyBattleDefeat(int.MaxValue) && MapRunState.IsEnded &&
                MapRunState.EndReason == "Ejected", "Zero integrity did not end the run.");
            Require(!repository.TryRead(out _, out RunSaveError error) && error == RunSaveError.RunEnded,
                "Ejected run can continue from an older save.");
            Require(repository.TryRead(out var ended, out _, true) && ended.schemaVersion == 2 && ended.integrity == 0,
                "Terminal file lost actual health.");
            Require(!RunRecordService.ApplyBattleDefeat(1) && !RunRecordService.ReduceConsistency(1),
                "Ended run accepted more damage.");
            MapRunState.StartNewRun(bindings.Progression);
            Require(MapRunState.RecordHealth.Integrity == 100 && MapRunState.RecordHealth.Consistency == 100,
                "New game did not reset health.");
        }
        finally
        {
            MapRunState.Reset();
            repository.TryDelete(out _);
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
        Debug.Log("[Run Record Verification] Health bounds, consistency overflow, defeat damage, save round-trip, v1 migration, terminal invalidation and new game reset passed.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
