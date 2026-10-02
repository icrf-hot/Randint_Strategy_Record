using System.Collections;
using TMPro;
using UnityEngine;
using Randint.Data;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapBattleSceneLoader : MonoBehaviour
{
    [Header("Battle Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset battleScene;
#endif

    [SerializeField, HideInInspector]
    private string battleScenePath;

    [SerializeField]
    private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

    [Header("Exit Loading UI")]
    [SerializeField] private CanvasGroup exitCanvasGroup;
    [SerializeField] private CanvasGroup loadingTextCanvasGroup;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] [GameTextKey] private string loadingMessageKey;
    private string loadingMessage => GameData.Text(loadingMessageKey);

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float fadeStartDelay = 0.1f;

    [Min(0f)]
    [SerializeField] private float fadeToBlackDuration = 0.35f;

    [Min(0f)]
    [SerializeField] private float minimumLoadingTime = 0.8f;

    [Min(0f)]
    [SerializeField] private float mapTextFadeOutDuration = 0.25f;

    [Min(0.1f)]
    [SerializeField] private float dotInterval = 0.35f;



    private bool isLoading;
    private int battleSceneBuildIndex = -1;

    public bool IsLoading => isLoading;

#if UNITY_EDITOR
    private void OnValidate()
    {
        battleScenePath =
            battleScene != null
                ? AssetDatabase.GetAssetPath(battleScene)
                : string.Empty;
    }
#endif

    private void Awake()
    {
        if (exitCanvasGroup != null)
        {
            exitCanvasGroup.alpha = 0f;
            exitCanvasGroup.interactable = false;
            exitCanvasGroup.blocksRaycasts = false;
        }

        if (loadingTextCanvasGroup != null)
        {
            loadingTextCanvasGroup.alpha = 1f;
            loadingTextCanvasGroup.interactable = false;
            loadingTextCanvasGroup.blocksRaycasts = false;
        }

        UpdateLoadingText();
    }

    public void LoadBattle(MapNode battleNode)
    {
        if (isLoading)
            return;

        if (!ValidateRequest(battleNode))
            return;

        StartCoroutine(LoadBattleRoutine(battleNode));
    }

    private bool ValidateRequest(MapNode battleNode)
    {
        if (battleNode == null)
        {
            Debug.LogError(
                "[Map Battle] Battle Node가 없습니다.",
                this
            );
            return false;
        }

        if (battleNode.NodeType != MapNodeType.Battle)
        {
            Debug.LogWarning(
                $"[Map Battle] Node {battleNode.NodeID}은 " +
                "Battle 타입이 아닙니다.",
                battleNode
            );
            return false;
        }

        if (exitCanvasGroup == null)
        {
            Debug.LogError(
                "[Map Battle] Exit Canvas Group이 지정되지 않았습니다.",
                this
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(battleScenePath))
        {
            Debug.LogError(
                "[Map Battle] Battle Scene이 지정되지 않았습니다.",
                this
            );
            return false;
        }

        battleSceneBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(
                battleScenePath
            );

        if (battleSceneBuildIndex < 0)
        {
            Debug.LogError(
                $"[Map Battle] '{battleScenePath}'이 " +
                "Build Settings에 없습니다.",
                this
            );
            return false;
        }

        return true;
    }

    private IEnumerator LoadBattleRoutine(MapNode battleNode)
    {
        isLoading = true;

        exitCanvasGroup.interactable = true;
        exitCanvasGroup.blocksRaycasts = true;

        Debug.Log(
            $"[Map Battle] 전투 Scene 전환 시작. " +
            $"Node ID: {battleNode.NodeID}",
            battleNode
        );

        // 전투 선택 직후 짧은 연출 여유를 둡니다.
        if (fadeStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                fadeStartDelay
            );
        }

        // Map 화면을 완전히 검게 가립니다.
        yield return FadeToBlack();

        float loadingStartTime =
            Time.realtimeSinceStartup;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                battleSceneBuildIndex,
                loadSceneMode
            );

        if (operation == null)
        {
            Debug.LogError(
                "[Map Battle] Scene 로드를 시작하지 못했습니다.",
                this
            );

            yield return RestoreMapScreen();
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f ||
               Time.realtimeSinceStartup - loadingStartTime <
               minimumLoadingTime)
        {
            UpdateLoadingText();
            yield return null;
        }

        Debug.Log(
            "[Map Battle] 전투 Scene 준비 완료. Scene을 활성화합니다.",
            this
        );

        // Scene이 준비되면 Map의 안내 문구부터 지웁니다.
        yield return FadeCanvasGroup(
            loadingTextCanvasGroup,
            0f,
            mapTextFadeOutDuration
        );

        Debug.Log(
            "[Map Battle] Map Loading TMP Fade Out 완료. " +
            "Battle Scene을 활성화합니다.",
            this
        );

        operation.allowSceneActivation = true;
    }

    private IEnumerator FadeToBlack()
    {
        float timer = 0f;
        float startAlpha = exitCanvasGroup.alpha;

        while (timer < fadeToBlackDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                fadeToBlackDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        timer / fadeToBlackDuration
                    );

            t = t * t * (3f - 2f * t);

            exitCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, 1f, t);

            UpdateLoadingText();
            yield return null;
        }

        exitCanvasGroup.alpha = 1f;
    }

    private IEnumerator RestoreMapScreen()
    {
        float timer = 0f;

        while (timer < fadeToBlackDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                fadeToBlackDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        timer / fadeToBlackDuration
                    );

            exitCanvasGroup.alpha = 1f - t;
            yield return null;
        }

        exitCanvasGroup.alpha = 0f;
        exitCanvasGroup.interactable = false;
        exitCanvasGroup.blocksRaycasts = false;
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
}
