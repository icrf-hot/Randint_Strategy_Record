using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunSaveService
{
    private static RunSaveRepository defaultRepository;
    private static RunSaveRepository Root => defaultRepository ??
        (defaultRepository = new RunSaveRepository(Path.Combine(Application.persistentDataPath, "Saves"), RunSaveBindings.Instance));
    public static int SelectedSlot { get; private set; } = 1;
    public static RunSaveSlots Slots => new RunSaveSlots(Root);
    public static RunSaveRepository Repository => Slots.At(SelectedSlot);
    public static bool SelectSlot(int slot)
    {
        if (!Slots.TryPageCount(out int pages, out _) || slot < 1 || slot > pages * RunSaveSlots.SlotsPerPage) return false;
        SelectedSlot = slot;
        return true;
    }

    public static bool CanSave
    {
        get
        {
            RunSaveBindings bindings = RunSaveBindings.Instance;
            if (bindings == null || SceneManager.GetActiveScene().path != bindings.MapScenePath ||
                !MapRunState.Progression.IsInitialized || !MapRunState.HasCurrentNode || MapRunState.IsEnded ||
                ShopSceneRouter.IsTransitioning) return false;
            MapSceneFadeIn fade = UnityEngine.Object.FindFirstObjectByType<MapSceneFadeIn>();
            MapBattleSceneLoader loader = UnityEngine.Object.FindFirstObjectByType<MapBattleSceneLoader>();
            return (fade == null || fade.IsFadeComplete) && (loader == null || !loader.IsLoading);
        }
    }

    public static bool Save(out RunSaveError error)
    {
        error = RunSaveError.NotAllowed;
        if (!CanSave) return false;
        return Repository.TryWrite(MapRunState.Capture(RunSaveBindings.Instance), out error);
    }

    public static bool MarkRunEnded(string reason, out RunSaveError error)
    {
        if (reason != "Ending" && reason != "Ejected") throw new ArgumentException(nameof(reason));
        MapRunState.MarkEnded(reason);
        error = RunSaveError.None;
        int[] ids;
        try { ids = Slots.ExistingSlotIds(); }
        catch (IOException) { error = RunSaveError.IoError; return false; }
        catch (UnauthorizedAccessException) { error = RunSaveError.IoError; return false; }
        foreach (int id in ids)
        {
            RunSaveRepository repository = Slots.At(id);
            if (!repository.TryRead(out RunSaveSnapshot saved, out RunSaveError readError, true))
            {
                if (readError == RunSaveError.IoError) error = readError;
                continue;
            }
            if (saved.runId != MapRunState.RunId) continue;
            saved.state = "Ended";
            saved.endReason = reason;
            saved.schemaVersion = 2;
            saved.integrity = MapRunState.RecordHealth.Integrity;
            saved.consistency = MapRunState.RecordHealth.Consistency;
            saved.savedUtc = DateTimeOffset.UtcNow.ToString("O");
            if (!repository.TryWrite(saved, out RunSaveError failure) && !repository.TryDelete(out failure))
                error = failure;
        }
        return error == RunSaveError.None;
    }

    // Keeps tests isolated from a user's actual slot. Runtime UI never changes this.
    public static IDisposable UseRepositoryForVerification(RunSaveRepository repository)
    {
        var previous = defaultRepository;
        int previousSlot = SelectedSlot;
        defaultRepository = repository;
        SelectedSlot = repository.SlotId;
        return new RepositoryScope(() => { defaultRepository = previous; SelectedSlot = previousSlot; });
    }

    private sealed class RepositoryScope : IDisposable
    {
        private Action restore;
        public RepositoryScope(Action restore) { this.restore = restore; }
        public void Dispose() { restore?.Invoke(); restore = null; }
    }
}
