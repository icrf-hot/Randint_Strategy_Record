using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Randint.Data;

[Serializable]
public class BattleResultTextSet
{
    [GameTextKey] public string characterDialogueKey;

    [GameTextKey] public string resultMessageKey;
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
    [GameTextKey] private string fallbackMapNameKey;

    [Header("Success")]
    [SerializeField]
    [GameTextKey] private string successTitleKey;

    [SerializeField]
    private Color successTitleColor =
        new Color(0.4f, 1f, 0.5f, 1f);

    [SerializeField]
    private BattleResultTextSet successText;

    [Header("Failure")]
    [SerializeField]
    [GameTextKey] private string failureTitleKey;

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
                    ? GameData.Text(successTitleKey)
                    : GameData.Text(failureTitleKey);

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
                    ? GameData.Text(fallbackMapNameKey)
                    : currentMapName;
        }

        if (characterDialogueText != null)
        {
            characterDialogueText.text =
                selectedText != null
                    ? GameData.Text(selectedText.characterDialogueKey)
                    : string.Empty;
        }

        if (resultMessageText != null)
        {
            resultMessageText.text =
                selectedText != null
                    ? GameData.Text(selectedText.resultMessageKey)
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
