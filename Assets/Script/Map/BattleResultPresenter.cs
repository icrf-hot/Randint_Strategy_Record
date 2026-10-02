using System;
using System.Collections;
using TMPro;
using UnityEngine;

[Serializable]
public class BattleResultTextSet
{
    [TextArea(2, 5)]
    public string characterDialogue;

    [TextArea(2, 5)]
    public string resultMessage;
}

public class BattleResultPresenter : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField]
    private CanvasGroup resultCanvasGroup;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text resultTitleText;

    [SerializeField]
    private TMP_Text mapNameText;

    [SerializeField]
    private TMP_Text characterDialogueText;

    [SerializeField]
    private TMP_Text resultMessageText;

    [Header("Map Name")]
    [SerializeField]
    private string fallbackMapName = "알 수 없는 작전";

    [Header("Success")]
    [SerializeField]
    private string successTitle = "성공";

    [SerializeField]
    private Color successTitleColor =
        new Color(0.4f, 1f, 0.5f, 1f);

    [SerializeField]
    private BattleResultTextSet successText;

    [Header("Failure")]
    [SerializeField]
    private string failureTitle = "실패";

    [SerializeField]
    private Color failureTitleColor =
        new Color(1f, 0.3f, 0.3f, 1f);

    [SerializeField]
    private BattleResultTextSet failureText;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField]
    private float fadeInDuration = 0.35f;

    [Min(0f)]
    [SerializeField]
    private float displayDuration = 2f;

    [Min(0f)]
    [SerializeField]
    private float fadeOutDuration = 0.35f;

    private bool isShowing;
    private bool gameTimeRestored = true;

    private void Awake()
    {
        HideImmediately();
    }

    public IEnumerator ShowResult(
        BattleResultType resultType)
    {
        if (isShowing)
            yield break;

        if (resultCanvasGroup == null)
        {
            Debug.LogError(
                "[Battle Result] Result CanvasGroup이 없습니다.",
                this
            );

            yield break;
        }

        isShowing = true;
        gameTimeRestored = false;

        SetResultText(resultType);

        Time.timeScale = 0f;

        resultCanvasGroup.gameObject.SetActive(true);
        resultCanvasGroup.alpha = 0f;
        resultCanvasGroup.interactable = false;
        resultCanvasGroup.blocksRaycasts = true;

        yield return FadeCanvasGroup(
            1f,
            fadeInDuration
        );

        if (displayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                displayDuration
            );
        }

        yield return FadeCanvasGroup(
            0f,
            fadeOutDuration
        );

        HideImmediately();

        Time.timeScale = 1f;
        gameTimeRestored = true;
        isShowing = false;
    }

    private void SetResultText(
        BattleResultType resultType)
    {
        bool isSuccess =
            resultType == BattleResultType.Success;

        BattleResultTextSet selectedText =
            isSuccess
                ? successText
                : failureText;

        if (resultTitleText != null)
        {
            resultTitleText.text =
                isSuccess
                    ? successTitle
                    : failureTitle;

            resultTitleText.color =
                isSuccess
                    ? successTitleColor
                    : failureTitleColor;
        }

        if (mapNameText != null)
        {
            string currentMapName =
                MapRunState.CurrentNodeDisplayName;

            mapNameText.text =
                string.IsNullOrWhiteSpace(currentMapName)
                    ? fallbackMapName
                    : currentMapName;
        }

        if (characterDialogueText != null)
        {
            characterDialogueText.text =
                selectedText != null
                    ? selectedText.characterDialogue
                    : string.Empty;
        }

        if (resultMessageText != null)
        {
            resultMessageText.text =
                selectedText != null
                    ? selectedText.resultMessage
                    : string.Empty;
        }
    }

    private IEnumerator FadeCanvasGroup(
        float targetAlpha,
        float duration)
    {
        float startAlpha =
            resultCanvasGroup.alpha;

        if (duration <= 0f)
        {
            resultCanvasGroup.alpha =
                targetAlpha;

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

            t = t * t * (3f - 2f * t);

            resultCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        resultCanvasGroup.alpha =
            targetAlpha;
    }

    private void HideImmediately()
    {
        if (resultCanvasGroup == null)
            return;

        resultCanvasGroup.alpha = 0f;
        resultCanvasGroup.interactable = false;
        resultCanvasGroup.blocksRaycasts = false;
        resultCanvasGroup.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (!gameTimeRestored &&
            Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}