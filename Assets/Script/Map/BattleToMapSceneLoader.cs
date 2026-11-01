using UnityEngine;

public class BattleToMapSceneLoader : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "Map_1F";
    public bool IsLoading => TransitionManager.IsBusy;
    public void LoadMap()
    {
        string destination = MapRunState.IsEnded ? RunRecordService.LobbyScenePath : mapSceneName;
        TransitionManager.TryLoad(destination, () => { if (!MapRunState.IsEnded) MapRunState.BeginMapEntry(); }, message: "> 작전 기록 동기화 중");
    }
}
