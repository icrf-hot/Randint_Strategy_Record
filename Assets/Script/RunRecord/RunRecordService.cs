using System;
using Randint.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunRecordService
{
    public const string LobbyScenePath = "Assets/Scenes/Lobby.unity";

    public static bool ReduceIntegrity(int percentagePoints) => ApplyLoss(percentagePoints, false, false);
    public static bool ReduceConsistency(int percentagePoints) => ApplyLoss(percentagePoints, true, false);

    public static bool ApplyBattleDefeat(int survivingEnemies)
    {
        if (survivingEnemies < 0) return false;
        int perEnemy = GameData.Catalog.Rules("default_run").defeatIntegrityLossPerEnemy;
        int damage = (int)Math.Min((long)survivingEnemies * perEnemy, int.MaxValue);
        // The battle result presentation completes before its loader returns to the lobby.
        return ApplyLoss(damage, false, true);
    }

    private static bool ApplyLoss(int amount, bool consistency, bool deferLobby)
    {
        if (amount < 0 || !MapRunState.Progression.IsInitialized || MapRunState.IsEnded) return false;
        if (consistency) MapRunState.RecordHealth.ReduceConsistency(amount);
        else MapRunState.RecordHealth.ReduceIntegrity(amount);
        if (MapRunState.RecordHealth.Integrity > 0) return true;
        if (!RunSaveService.MarkRunEnded("Ejected", out RunSaveError error))
        {
            Debug.LogWarning("[Run Record] Could not mark the saved run ended: " + error);
        }
        if (!deferLobby) ReturnToLobby();
        return true;
    }

    public static void ReturnToLobby()
    {
        if (!Application.CanStreamedLevelBeLoaded(LobbyScenePath))
        {
            Debug.LogError("[Run Record] Lobby is not available in Build Settings.");
            return;
        }
        Time.timeScale = 1f;
        if (SceneManager.GetActiveScene().path != LobbyScenePath)
            SceneManager.LoadSceneAsync(LobbyScenePath);
    }
}
