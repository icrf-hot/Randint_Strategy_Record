using System.Collections;
using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Title/Lobby 전용 전환. 기존 Map ↔ Battle 페이드와는 독립적입니다.</summary>
public sealed class MenuSceneLoader : MonoBehaviour
{
    [SerializeField] private CanvasGroup transitionCanvas;
    [SerializeField] private TMP_Text transitionText;
    [SerializeField, GameTextKey] private string loadingTextKey = "menu.loading";
    [SerializeField, GameTextKey] private string errorTextKey = "menu.load_error";
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        Time.timeScale = 1f;
        transitionCanvas.alpha = 0f;
        transitionCanvas.blocksRaycasts = false;
        transitionCanvas.interactable = false;
        transitionCanvas.gameObject.SetActive(false);
    }

    public bool TryLoadScene(string scenePath, bool resetMapRun = false)
    {
        if (IsLoading) return false;
        // 로드 가능 여부를 확인한 뒤에만 현재 런을 초기화합니다.
        if (string.IsNullOrWhiteSpace(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"[Menu] Build Settings에 없는 Scene: {scenePath}", this);
            StartCoroutine(ShowLoadError());
            return false;
        }
        IsLoading = true;
        StartCoroutine(LoadRoutine(scenePath, resetMapRun));
        return true;
    }

    private IEnumerator ShowLoadError()
    {
        IsLoading = true;
        transitionText.text = GameData.Text(errorTextKey);
        transitionCanvas.gameObject.SetActive(true);
        transitionCanvas.alpha = 1f;
        transitionCanvas.blocksRaycasts = true;
        float until = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < until) yield return null;
        transitionCanvas.gameObject.SetActive(false);
        transitionCanvas.blocksRaycasts = false;
        IsLoading = false;
    }

    private IEnumerator LoadRoutine(string scenePath, bool resetMapRun)
    {
        transitionCanvas.gameObject.SetActive(true);
        transitionCanvas.alpha = 0f;
        transitionCanvas.blocksRaycasts = true;
        transitionText.text = GameData.Text(loadingTextKey);
        float elapsed = 0f;
        // timeScale이 0인 상태에서도 입력 차단 및 전환이 완료됩니다.
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionCanvas.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        transitionCanvas.alpha = 1f;
        AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
        if (operation == null)
        {
            IsLoading = false;
            transitionCanvas.gameObject.SetActive(false);
            yield break;
        }
        // 새 Scene의 Awake/Start보다 먼저 설정합니다. 디스크 파일은 변경하지 않습니다.
        operation.allowSceneActivation = false;
        if (resetMapRun) MapRunState.Reset();
        Time.timeScale = 1f;
        while (operation.progress < 0.9f) yield return null;
        operation.allowSceneActivation = true;
    }
}
