using System.Collections;
using TMPro;
using UnityEngine;
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

    private int battleSceneBuildIndex = -1;

    [SerializeField]
    private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

    [Header("Loading UI")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private string loadingMessage = "작전 지역으로 이동 중";

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.3f;

    [Min(0f)]
    [SerializeField] private float minimumLoadingTime = 0.8f;

    [Min(0.1f)]
    [SerializeField] private float dotInterval = 0.35f;

    private bool isLoading;
    private GameObject transitionRoot;

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
        SetLoadingState(false);
        UpdateLoadingText();
    }

    public void LoadBattle(MapNode battleNode)
    {
        if (isLoading)
            return;

        if (!ValidateBattleRequest(battleNode))
            return;

        StartCoroutine(LoadBattleRoutine(battleNode));
    }

    private bool ValidateBattleRequest(MapNode battleNode)
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

        if (loadingCanvasGroup == null)
        {
            Debug.LogError(
                "[Map Battle] Loading Canvas Group이 지정되지 않았습니다.",
                this
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(battleScenePath))
        {
            Debug.LogError(
                "[Map Battle] Battle Scene을 지정하십시오.",
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
                "Build Settings에 등록되어 있지 않습니다.",
                this
            );
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                battleSceneBuildIndex))
        {
            Debug.LogError(
                $"[Map Battle] Build Index " +
                $"{battleSceneBuildIndex}의 Scene을 불러올 수 없습니다.",
                this
            );
            return false;
        }

        return true;
    }

    private IEnumerator LoadBattleRoutine(MapNode battleNode)
    {
        isLoading = true;

        // Single 모드로 Scene이 바뀌어도 Loading UI가 살아 있도록 유지합니다.
        transitionRoot = transform.root.gameObject;
        DontDestroyOnLoad(transitionRoot);

        loadingCanvasGroup.blocksRaycasts = true;
        loadingCanvasGroup.interactable = true;

        yield return FadeCanvas(0f, 1f);

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
                "[Map Battle] 비동기 Scene 로드를 시작하지 못했습니다.",
                this
            );

            yield return FadeCanvas(1f, 0f);

            SetLoadingState(false);
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        Debug.Log(
            $"[Map Battle] Loading 시작. " +
            $"Node ID: {battleNode.NodeID}, " +
            $"Floor: {battleNode.FloorIndex}, " +
            $"Scene: {battleScenePath}, " +
            $"Build Index: {battleSceneBuildIndex}",
            battleNode
        );

        while (operation.progress < 0.9f ||
               Time.realtimeSinceStartup - loadingStartTime <
               minimumLoadingTime)
        {
            UpdateLoadingText();
            yield return null;
        }

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            UpdateLoadingText();
            yield return null;
        }

        // Battle Scene의 첫 화면이 준비될 시간을 한 프레임 제공합니다.
        yield return null;

        yield return FadeCanvas(1f, 0f);

        SetLoadingState(false);

        Debug.Log(
            "[Map Battle] Loading 완료.",
            this
        );

        if (transitionRoot != null)
        {
            Destroy(transitionRoot);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeCanvas(float from, float to)
    {
        if (loadingCanvasGroup == null)
        {
            Debug.LogError(
                "[Map Battle] Loading Canvas Group이 사라졌습니다.",
                this
            );
            yield break;
        }

        if (fadeDuration <= 0f)
        {
            loadingCanvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;
        loadingCanvasGroup.alpha = from;

        while (timer < fadeDuration)
        {
            if (loadingCanvasGroup == null)
                yield break;

            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(timer / fadeDuration);

            t = t * t * (3f - 2f * t);

            loadingCanvasGroup.alpha =
                Mathf.Lerp(from, to, t);

            UpdateLoadingText();

            yield return null;
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = to;
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

    private void SetLoadingState(bool visible)
    {
        if (loadingCanvasGroup == null)
            return;

        loadingCanvasGroup.alpha =
            visible ? 1f : 0f;

        loadingCanvasGroup.interactable = visible;
        loadingCanvasGroup.blocksRaycasts = visible;
    }
}