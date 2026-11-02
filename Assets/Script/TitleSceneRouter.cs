using UnityEngine;

/// <summary>Legacy entry point routed through the common transition owner.</summary>
public class TitleSceneRouter : MonoBehaviour
{
    [SerializeField] private RunProgressionSettings progressionSettings;
    public void StartNewRun()
    {
        if (TransitionManager.IsBusy || progressionSettings == null || !progressionSettings.IsValid) return;
        TransitionManager.TryLoad(CampaignScenes.PathFor(1), () => MapRunState.StartNewRun(progressionSettings), message: "> 새 작전 기록 생성 중");
    }
}
