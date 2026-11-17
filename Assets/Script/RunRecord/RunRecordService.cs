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
        bool hiddenThirdFloor = MapRunState.HiddenEndingRouteUnlocked && MapRunState.MapFloor == 3 &&
            MapRunState.CurrentNodeID == CampaignDefinition.TerminalNodeId(3);
        int multiplier = hiddenThirdFloor ? 2 : 1;
        int damage = (int)Math.Min((long)survivingEnemies * perEnemy * multiplier, int.MaxValue);
        // The battle result presentation completes before its loader returns to the lobby.
        bool applied = ApplyLoss(damage, false, true);
        if (applied && !MapRunState.IsEnded && !hiddenThirdFloor && MapRunState.HasCurrentNode &&
            MapRunState.CurrentNodeID == CampaignDefinition.TerminalNodeId(MapRunState.MapFloor))
        {
            if (!RunSaveService.MarkRunEnded("TerminalDefeat", out RunSaveError error))
                Debug.LogWarning("[Run Record] Could not close the defeated terminal run: " + error);
        }
        return applied;
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
        if (!deferLobby)
            RunSummaryWindow.Show(RunEndSummary.Capture(), () =>
                TransitionManager.TryLoad(LobbyScenePath, message: "> 기록 열람 종료 중"));
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
            TransitionManager.TryLoad(LobbyScenePath, message: "> 기록 열람 종료 중");
    }
}
