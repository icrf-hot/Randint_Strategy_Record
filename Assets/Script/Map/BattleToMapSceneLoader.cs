using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleToMapSceneLoader : MonoBehaviour
{
    [Header("Map Scene")]
    [SerializeField]
    private string mapSceneName = "Map_1F";

    [Header("Exit Loading UI")]
    [SerializeField]
    private CanvasGroup exitCanvasGroup;

    [SerializeField]
    private CanvasGroup loadingTextCanvasGroup;

    [SerializeField]
    private TMP_Text loadingText;

    [SerializeField]
    private string loadingMessage = "작전 기록 정리 중";

    [Header("Timing")]
    [Min(0f)]
    [SerializeField]
    private float fadeStartDelay = 0.3f;

    [Min(0f)]
    [SerializeField]
    private float fadeToBlackDuration = 0.45f;

    [Min(0f)]
    [SerializeField]
    private float minimumLoadingTime = 0.8f;

    [Min(0f)]
    [SerializeField]
    private float textFadeOutDuration = 0.25f;

    [Min(0.1f)]
    [SerializeField]
    private float dotInterval = 0.35f;

    private bool isLoading;

    public bool IsLoading => isLoading;

    public void LoadMap()
    {
        if (isLoading)
            return;

        if (exitCanvasGroup == null)
        {
            Debug.LogError(
                "[Battle To Map] Exit CanvasGroup이 없습니다.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(mapSceneName))
        {
            Debug.LogError(
                "[Battle To Map] Map Scene 이름이 비어 있습니다.",
                this
            );

            return;
        }

        StartCoroutine(LoadMapRoutine());
    }

    private IEnumerator LoadMapRoutine()
    {
        isLoading = true;

        // 전투 진행을 정지하지만 Fade는 unscaledDeltaTime으로 진행합니다.
        Time.timeScale = 0f;

        exitCanvasGroup.gameObject.SetActive(true);
        exitCanvasGroup.alpha = 0f;
        exitCanvasGroup.interactable = true;
        exitCanvasGroup.blocksRaycasts = true;

        if (loadingTextCanvasGroup != null)
        {
            loadingTextCanvasGroup.alpha = 1f;
            loadingTextCanvasGroup.interactable = false;
            loadingTextCanvasGroup.blocksRaycasts = false;
        }

        UpdateLoadingText();

        if (fadeStartDelay > 0f)
        {
            yield return WaitRealtime(fadeStartDelay);
        }

        // 전투 화면을 검은 화면으로 덮습니다.
        yield return FadeCanvasGroup(
            exitCanvasGroup,
            1f,
            fadeToBlackDuration
        );

        float loadingStartTime =
            Time.realtimeSinceStartup;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                mapSceneName,
                LoadSceneMode.Single
            );

        if (operation == null)
        {
            Debug.LogError(
                "[Battle To Map] Map Scene 로드를 시작하지 못했습니다.",
                this
            );

            Time.timeScale = 1f;
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f ||
               Time.realtimeSinceStartup - loadingStartTime
               < minimumLoadingTime)
        {
            UpdateLoadingText();
            yield return null;
        }

        // 맵이 준비되면 안내 문구만 먼저 숨깁니다.
        yield return FadeCanvasGroup(
            loadingTextCanvasGroup,
            0f,
            textFadeOutDuration
        );

        // 새 Map Scene의 Awake보다 먼저 설정해야 합니다.
        MapRunState.BeginMapEntry();

        // MapSceneFadeIn이 다시 0으로 설정하므로
        // 우선 정상 배율로 복원합니다.
        Time.timeScale = 1f;

        Debug.Log(
            "[Battle To Map] Map Scene 준비 완료.",
            this
        );

        operation.allowSceneActivation = true;
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup targetGroup,
        float targetAlpha,
        float duration)
    {
        if (targetGroup == null)
            yield break;

        float startAlpha = targetGroup.alpha;

        if (duration <= 0f)
        {
            targetGroup.alpha = targetAlpha;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(timer / duration);

            // 부드러운 Smooth Step
            t = t * t * (3f - 2f * t);

            targetGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            UpdateLoadingText();
            yield return null;
        }

        targetGroup.alpha = targetAlpha;
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            UpdateLoadingText();
            yield return null;
        }
    }

    private void UpdateLoadingText()
    {
        if (loadingText == null)
            return;

        int dotCount =
            1 + Mathf.FloorToInt(
                Time.unscaledTime / dotInterval
            ) % 3;

        loadingText.text =
            loadingMessage +
            new string('.', dotCount);
    }
}