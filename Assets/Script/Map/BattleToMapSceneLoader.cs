using UnityEngine;

public class BattleToMapSceneLoader : MonoBehaviour
{
    public bool IsLoading => TransitionManager.IsBusy;
    public void LoadMap()
    {
        string destination = MapRunState.IsEnded ? RunRecordService.LobbyScenePath : CampaignScenes.CurrentPath;
        TransitionManager.TryLoad(destination, () => { if (!MapRunState.IsEnded) MapRunState.BeginMapEntry(); }, message: "> 작전 기록 동기화 중");
    }
}
