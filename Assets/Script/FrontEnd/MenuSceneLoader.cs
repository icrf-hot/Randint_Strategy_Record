using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Existing menu API; transitions are owned by the shared manager.</summary>
public sealed class MenuSceneLoader : MonoBehaviour
{
    [SerializeField] private CanvasGroup transitionCanvas;
    [SerializeField] private TMP_Text transitionText;
    [SerializeField, GameTextKey] private string loadingTextKey = "menu.loading";
    [SerializeField, GameTextKey] private string errorTextKey = "menu.load_error";
    [SerializeField, Min(0)] private float fadeDuration = .25f;
    [SerializeField] private RunProgressionSettings progressionSettings;
    public bool IsLoading => TransitionManager.IsBusy;
    private void Awake()
    {
        if (transitionCanvas != null) {
            transitionCanvas.alpha = 0; transitionCanvas.blocksRaycasts = false; transitionCanvas.interactable = false;
            transitionCanvas.gameObject.SetActive(false);
        }
    }
    public bool TryLoadScene(string scenePath, bool resetMapRun = false)
    {
        if (IsLoading) return false;
        if (resetMapRun && (progressionSettings == null || !progressionSettings.IsValid || !PreparationSceneController.TryGetRoster(out _))) {
            Debug.LogError("[Menu] Invalid progression or roster.", this); return false;
        }
        bool accepted = TransitionManager.TryLoad(scenePath,
            resetMapRun ? () => MapRunState.StartNewRun(progressionSettings) : (System.Action)null,
            message: "> " + GameData.Text(loadingTextKey));
        if (!accepted) Debug.LogError("[Menu] " + GameData.Text(errorTextKey) + ": " + scenePath, this);
        return accepted;
    }
    public bool TryContinue(RunSaveSnapshot snapshot)
    {
        var bindings = RunSaveBindings.Instance;
        if (IsLoading || SceneManager.GetActiveScene().name != "Lobby" || bindings == null ||
            !PreparedRunRestore.TryPrepare(snapshot, bindings, out var prepared, out _)) return false;
        return TransitionManager.TryLoad(prepared.MapScenePath, prepared.Apply, message: "> 작전 데이터 복원 중");
    }
}
