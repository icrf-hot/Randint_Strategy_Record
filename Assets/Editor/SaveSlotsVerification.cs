using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class SaveSlotsVerification
{
    public static void RunFinal()
    {
        RunRecordVerification.RunFinal();
        Run();
    }

    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "RSR_Slots_" + Guid.NewGuid().ToString("N"));
        var root = new RunSaveRepository(directory, RunSaveBindings.Instance);
        using var scope = RunSaveService.UseRepositoryForVerification(root);
        try
        {
            var slots = RunSaveService.Slots;
            Require(slots.TryPageCount(out int count, out _) && count == 1 && !Directory.Exists(directory),
                "Default page must show ten empty slots without creating files.");
            Require(root.FilePath == slots.At(1).FilePath && Path.GetFileName(root.FilePath) == "run.json",
                "Legacy file was not retained as slot one.");
            Require(!RunSaveService.SelectSlot(11) && !RunSaveService.SelectSlot(0), "Unallocated slot accepted.");
            Require(slots.TryAddPage(out count, out _) && count == 2 && slots.TryAddPage(out count, out _) && count == 3,
                "Page expansion failed.");
            Require(new RunSaveSlots(root).TryPageCount(out count, out _) && count == 3, "Empty added pages did not persist.");
            MapRunState.StartNewRun(RunSaveBindings.Instance.Progression);
            MapRunState.SaveCurrentNode(5, "Battle");
            var first = MapRunState.Capture(RunSaveBindings.Instance);
            Require(root.TryWrite(first, out _) && slots.At(11).TryWrite(first, out _), "Independent saves failed.");
            MapRunState.StartNewRun(RunSaveBindings.Instance.Progression);
            MapRunState.SaveCurrentNode(5, "Battle");
            var second = MapRunState.Capture(RunSaveBindings.Instance);
            Require(slots.At(22).TryWrite(second, out _), "Other run save failed.");
            Require(slots.ExistingSlotIds().SequenceEqual(new[] { 1, 11, 22 }), "Slot discovery failed.");
            Require(slots.At(11).TryWrite(first, out _) && File.Exists(slots.At(11).FilePath + ".bak"), "Slot backup missing.");
            string other = File.ReadAllText(slots.At(22).FilePath);
            Require(slots.At(11).TryDelete(out _) && root.HasFiles && File.ReadAllText(slots.At(22).FilePath) == other &&
                !slots.At(11).HasFiles, "Deleting one slot affected other slots.");
            Require(slots.TryPageCount(out count, out _) && count == 3, "Deleting a slot removed pages.");
            Require(slots.At(11).TryWrite(first, out _), "Deleted slot could not be reused.");
            Require(PreparedRunRestore.TryPrepare(first, RunSaveBindings.Instance, out var prepared, out _), "Fixture restore failed.");
            prepared.Apply();
            Require(RunSaveService.SelectSlot(11), "Selection failed.");
            Require(RunRecordService.ApplyBattleDefeat(10) && MapRunState.IsEnded, "Terminal fixture failed.");
            foreach (int id in new[] { 1, 11 })
                Require(!slots.At(id).TryRead(out _, out RunSaveError ended) && ended == RunSaveError.RunEnded,
                    "Another slot revived the ejected run.");
            Require(slots.At(22).TryRead(out var unaffected, out _) && unaffected.runId == second.runId &&
                File.ReadAllText(slots.At(22).FilePath) == other, "Ending one run changed another run.");
            Require(new RunSaveSlots(root).TryPageCount(out count, out _) && count == 3, "Reopening lost page allocation.");
            string blocker = Path.Combine(directory, "blocker");
            File.WriteAllText(blocker, "file");
            var blocked = new RunSaveSlots(new RunSaveRepository(blocker, RunSaveBindings.Instance));
            Require(!blocked.TryAddPage(out _, out RunSaveError io) && io == RunSaveError.IoError, "Page I/O error was hidden.");
            File.Delete(blocker);
        }
        finally
        {
            MapRunState.Reset();
            foreach (int id in new[] { 1, 11, 22 }) new RunSaveRepository(directory, RunSaveBindings.Instance, id).TryDelete(out _);
            foreach (string name in new[] { "pages.json", "pages.json.tmp", "blocker" })
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path)) File.Delete(path);
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
        Debug.Log("[Save Slots Verification] Default ten, persistent +ten pages, legacy slot, independent writes/deletes/backups, selection, all copies ended and other-run preservation passed.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
