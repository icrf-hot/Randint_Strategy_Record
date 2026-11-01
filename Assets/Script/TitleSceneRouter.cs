using UnityEngine;

/// <summary>Legacy entry point routed through the common transition owner.</summary>
public class TitleSceneRouter : MonoBehaviour
{
    [SerializeField] private string mapScenePath = "Assets/Scenes/Map_1F.unity";
    [SerializeField] private RunProgressionSettings progressionSettings;
    public void StartNewRun()
    {
        if (TransitionManager.IsBusy || progressionSettings == null || !progressionSettings.IsValid) return;
        TransitionManager.TryLoad(mapScenePath, () => MapRunState.StartNewRun(progressionSettings), message: "> 새 작전 기록 생성 중");
    }
}
