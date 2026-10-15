using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunSaveService
{
    private static RunSaveRepository defaultRepository;
    public static RunSaveRepository Repository => defaultRepository ??
        (defaultRepository = new RunSaveRepository(Path.Combine(Application.persistentDataPath, "Saves"), RunSaveBindings.Instance));

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
        if (!Repository.TryRead(out RunSaveSnapshot saved, out error, true))
        {
            if (error != RunSaveError.NotFound) return false;
            error = RunSaveError.None;
            return true;
        }
        if (saved.runId != MapRunState.RunId) { error = RunSaveError.None; return true; }
        saved.state = "Ended";
        saved.endReason = reason;
        saved.savedUtc = DateTimeOffset.UtcNow.ToString("O");
        return Repository.TryWrite(saved, out error);
    }

    // Keeps tests isolated from a user's actual slot. Runtime UI never changes this.
    public static IDisposable UseRepositoryForVerification(RunSaveRepository repository)
    {
        var previous = defaultRepository;
        defaultRepository = repository;
        return new RepositoryScope(() => defaultRepository = previous);
    }

    private sealed class RepositoryScope : IDisposable
    {
        private Action restore;
        public RepositoryScope(Action restore) { this.restore = restore; }
        public void Dispose() { restore?.Invoke(); restore = null; }
    }
}
