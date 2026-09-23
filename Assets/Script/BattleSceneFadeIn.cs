using System.Collections;
using TMPro;
using UnityEngine;

public class BattleSceneFadeIn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup entryCanvasGroup;
    [SerializeField] private CanvasGroup loadingTextCanvasGroup;
    [SerializeField] private TMP_Text loadingText;

    [Header("Text")]
    [SerializeField] private string loadingMessage = "작전 준비 완료";

    [Min(0.1f)]
    [SerializeField] private float dotInterval = 0.35f;

    [Header("Text Timing")]
    [Min(0f)]
    [SerializeField] private float textFadeInDuration = 0.25f;

    [Min(0f)]
    [SerializeField] private float textHoldDuration = 0.35f;

    [Min(0f)]
    [SerializeField] private float textFadeOutDuration = 0.2f;

    [Header("Screen Timing")]
    [Min(0f)]
    [SerializeField] private float screenFadeOutDelay = 0.1f;

    [Min(0f)]
    [SerializeField] private float screenFadeOutDuration = 0.45f;

    [Header("Game Time")]
    [Min(0f)]
    [SerializeField] private float gameTimeScaleAfterFade = 1f;

    private bool gameTimeRestored;
    private bool isFadeComplete;

    public bool IsFadeComplete => isFadeComplete;

    private void Awake()
    {
        isFadeComplete = false;
        gameTimeRestored = false;

        if (entryCanvasGroup == null)
        {
            Debug.LogError(
                "[Battle Fade] Entry CanvasGroup이 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        Time.timeScale = 0f;

        entryCanvasGroup.gameObject.SetActive(true);
        entryCanvasGroup.alpha = 1f;
        entryCanvasGroup.interactable = true;
        entryCanvasGroup.blocksRaycasts = true;

        if (loadingTextCanvasGroup != null)
        {
            loadingTextCanvasGroup.alpha = 0f;
            loadingTextCanvasGroup.interactable = false;
            loadingTextCanvasGroup.blocksRaycasts = false;
        }

        UpdateLoadingText();
    }

    private IEnumerator Start()
    {
        // Battle Scene의 Awake와 Start가 실행될 시간을 줍니다.
        yield return null;

        // 새 Scene의 TMP를 검은 화면 위에 나타냅니다.
        yield return FadeGroup(
            loadingTextCanvasGroup,
            1f,
            textFadeInDuration
        );

        yield return WaitRealtime(textHoldDuration);

        // Battle TMP를 먼저 제거합니다.
        yield return FadeGroup(
            loadingTextCanvasGroup,
            0f,
            textFadeOutDuration
        );

        yield return WaitRealtime(screenFadeOutDelay);

        // 마지막으로 검은 배경을 제거합니다.
        yield return FadeGroup(
            entryCanvasGroup,
            0f,
            screenFadeOutDuration
        );

        entryCanvasGroup.alpha = 0f;
        entryCanvasGroup.interactable = false;
        entryCanvasGroup.blocksRaycasts = false;
        entryCanvasGroup.gameObject.SetActive(false);

        // 화면 연출이 전부 끝난 후에만 게임 시간을 시작합니다.
        Time.timeScale = gameTimeScaleAfterFade;
        gameTimeRestored = true;
        isFadeComplete = true;

        Debug.Log(
            "[Battle Fade] 진입 연출 완료. 게임과 카드 배분을 시작합니다.",
            this
        );
    }

    private IEnumerator FadeGroup(
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

    private void OnDestroy()
    {
        if (!gameTimeRestored &&
            Time.timeScale == 0f)
        {
            Time.timeScale =
                gameTimeScaleAfterFade;
        }
    }
}