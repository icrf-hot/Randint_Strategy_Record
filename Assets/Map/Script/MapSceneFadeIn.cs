using System.Collections;
using TMPro;
using UnityEngine;

public class MapSceneFadeIn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup entryCanvasGroup;
    [SerializeField] private CanvasGroup loadingTextCanvasGroup;
    [SerializeField] private TMP_Text loadingText;

    [Header("Text")]
    [SerializeField]
    private string loadingMessage = "작전 기록 복원 완료";

    [Header("Timing")]
    [Min(0f)]
    [SerializeField]
    private float fadeStartDelay = 0.1f;

    [Min(0f)]
    [SerializeField]
    private float fadeFromBlackDuration = 0.35f;

    [Min(0f)]
    [SerializeField]
    private float minimumDisplayTime = 0.8f;

    [Min(0f)]
    [SerializeField]
    private float mapTextFadeOutDuration = 0.25f;

    [Min(0.1f)]
    [SerializeField]
    private float dotInterval = 0.35f;

    private bool shouldPlayFade;
    private bool gameTimeRestored;
    private bool isFadeComplete;

    public bool IsFadeComplete => isFadeComplete;

    private void Awake()
    {
        shouldPlayFade =
            MapRunState.IsEnteringMapFromBattle;

        isFadeComplete = false;
        gameTimeRestored = false;

        if (entryCanvasGroup == null)
        {
            Debug.LogError(
                "[Map Fade] Entry CanvasGroup이 없습니다.",
                this
            );

            RestoreGameTime();
            enabled = false;
            return;
        }

        // Play → Map 전환이 아니면 진입 화면을 표시하지 않습니다.
        if (!shouldPlayFade)
        {
            HideEntryCanvas();

            isFadeComplete = true;
            gameTimeRestored = true;
            return;
        }

        Time.timeScale = 0f;

        entryCanvasGroup.gameObject.SetActive(true);
        entryCanvasGroup.alpha = 1f;
        entryCanvasGroup.interactable = true;
        entryCanvasGroup.blocksRaycasts = true;

        if (loadingTextCanvasGroup != null)
        {
            // MapBattleSceneLoader와 동일하게
            // 안내 문구를 처음부터 표시합니다.
            loadingTextCanvasGroup.alpha = 1f;
            loadingTextCanvasGroup.interactable = false;
            loadingTextCanvasGroup.blocksRaycasts = false;
        }

        UpdateLoadingText();
    }

    private IEnumerator Start()
    {
        if (!shouldPlayFade)
            yield break;

        // 맵 오브젝트의 Awake와 Start가 실행될 시간을 줍니다.
        yield return null;

        if (fadeStartDelay > 0f)
        {
            yield return WaitRealtime(
                fadeStartDelay
            );
        }

        // MapBattleSceneLoader의 minimumLoadingTime에 대응합니다.
        // 진입 씬은 이미 로드되었으므로 최소 표시 시간으로 사용합니다.
        if (minimumDisplayTime > 0f)
        {
            yield return WaitRealtime(
                minimumDisplayTime
            );
        }

        // 안내 문구를 먼저 숨깁니다.
        yield return FadeCanvasGroup(
            loadingTextCanvasGroup,
            0f,
            mapTextFadeOutDuration
        );

        // 검은 화면을 투명하게 만들어 맵을 보여줍니다.
        yield return FadeCanvasGroup(
            entryCanvasGroup,
            0f,
            fadeFromBlackDuration
        );

        HideEntryCanvas();

        MapRunState.CompleteMapEntry();

        RestoreGameTime();

        isFadeComplete = true;

        Debug.Log(
            "[Map Fade] Map Scene 진입 Fade 완료.",
            this
        );
    }

    private void HideEntryCanvas()
    {
        if (entryCanvasGroup == null)
            return;

        entryCanvasGroup.alpha = 0f;
        entryCanvasGroup.interactable = false;
        entryCanvasGroup.blocksRaycasts = false;
        entryCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup targetGroup,
        float targetAlpha,
        float duration)
    {
        if (targetGroup == null)
            yield break;

        float startAlpha =
            targetGroup.alpha;

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
                Mathf.Clamp01(
                    timer / duration
                );

            // Smooth Step
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

    private IEnumerator WaitRealtime(
        float duration)
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

    private void RestoreGameTime()
    {
        Time.timeScale = 1f;
        gameTimeRestored = true;
    }

    private void OnDestroy()
    {
        if (shouldPlayFade)
        {
            MapRunState.CompleteMapEntry();
        }

        if (!gameTimeRestored &&
            Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}