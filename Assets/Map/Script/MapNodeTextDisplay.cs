using System.Collections;
using TMPro;
using UnityEngine;
using Randint.Data;

public class MapNodeTextDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;
    [SerializeField] private MapNode node;
    [SerializeField] private TMP_Text targetText;

    [Header("Text")]
    [SerializeField, GameTextKey] private string displayTextKey;
    // 직접 전달된 문장은 해당 객체의 표시 상태이며 JSON 원본을 변경하지 않습니다.
    private string displayTextOverride;
    private string displayText => displayTextOverride ?? GameData.Text(displayTextKey);

    [Header("Typing")]
    [SerializeField] private float characterInterval = 0.03f;

    [Header("Cursor")]
    [SerializeField] private bool showCursor = true;
    [SerializeField] private string cursorCharacter = "_";
    [SerializeField] private float cursorBlinkInterval = 0.5f;


    private Coroutine typingCoroutine;
    private Coroutine cursorCoroutine;

    private MapNode currentNode;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // 자신의 MapNode 찾기
        // -----------------------------------------------------

        if (node == null)
        {
            node =
                GetComponent<MapNode>();

            if (node == null)
            {
                node =
                    GetComponentInParent<MapNode>();
            }
        }


        if (node == null)
        {
            Debug.LogError(
                "MapNodeTextDisplay: " +
                "MapNode를 찾을 수 없습니다.",
                this
            );

            enabled = false;
            return;
        }


        // -----------------------------------------------------
        // Orbit Camera 확인
        // -----------------------------------------------------

        if (orbitCamera == null)
        {
            Debug.LogError(
                "MapNodeTextDisplay: " +
                "MapOrbitCamera가 지정되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }


        // -----------------------------------------------------
        // TMP 찾기
        // -----------------------------------------------------

        if (targetText == null)
        {
            targetText =
                GetComponent<TMP_Text>();
        }

        if (targetText == null)
        {
            targetText =
                GetComponentInChildren<TMP_Text>();
        }

        if (targetText == null)
        {
            Debug.LogError(
                "MapNodeTextDisplay: " +
                "TMP_Text를 찾을 수 없습니다.",
                this
            );

            enabled = false;
            return;
        }


        targetText.text = "";
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (orbitCamera == null ||
            node == null)
        {
            return;
        }


        // =====================================================
        // Focus 복귀 시작
        // =====================================================

        if (orbitCamera.IsReturningFromFocus)
        {
            if (currentNode != null)
            {
                currentNode = null;
                ClearText();
            }

            return;
        }


        // =====================================================
        // Focus되지 않은 상태
        // =====================================================

        if (!orbitCamera.IsFocused)
        {
            if (currentNode != null)
            {
                currentNode = null;
                ClearText();
            }

            return;
        }


        // =====================================================
        // 현재 Focus된 Node
        // =====================================================

        MapNode focusedNode =
            orbitCamera.FocusedNode;


        // =====================================================
        // 자신의 Node가 Focus됨
        // =====================================================

        if (focusedNode == node)
        {
            /*
             * 새롭게 자신의 Node가 Focus된 경우에만
             * 텍스트를 출력합니다.
             */
            if (currentNode != node)
            {
                currentNode = node;

                ShowText();
            }

            return;
        }


        // =====================================================
        // 다른 Node가 Focus됨
        // =====================================================

        if (currentNode != null)
        {
            currentNode = null;

            ClearText();
        }
    }


    // =========================================================
    // Text 출력
    // =========================================================

    public void ShowText()
    {
        StopAllTextCoroutines();

        typingCoroutine =
            StartCoroutine(
                TypeText(displayText)
            );
    }


    // =========================================================
    // Text 직접 지정
    // =========================================================

    public void ShowText(string text)
    {
        displayTextOverride = text;

        ShowText();
    }


    // =========================================================
    // Typing
    // =========================================================

    private IEnumerator TypeText(string text)
    {
        targetText.text = "";


        if (string.IsNullOrEmpty(text))
        {
            typingCoroutine = null;
            yield break;
        }


        for (int i = 0; i < text.Length; i++)
        {
            targetText.text =
                text.Substring(0, i + 1);


            if (showCursor)
            {
                targetText.text +=
                    cursorCharacter;
            }


            yield return new WaitForSeconds(
                characterInterval
            );
        }


        typingCoroutine = null;


        // -----------------------------------------------------
        // Typing 종료 → Cursor Blink
        // -----------------------------------------------------

        if (showCursor)
        {
            cursorCoroutine =
                StartCoroutine(
                    BlinkCursor(text)
                );
        }
        else
        {
            targetText.text = text;
        }
    }


    // =========================================================
    // Cursor Blink
    // =========================================================

    private IEnumerator BlinkCursor(string text)
    {
        bool cursorVisible = true;


        while (true)
        {
            cursorVisible =
                !cursorVisible;


            if (cursorVisible)
            {
                targetText.text =
                    text +
                    cursorCharacter;
            }
            else
            {
                targetText.text =
                    text;
            }


            yield return new WaitForSeconds(
                cursorBlinkInterval
            );
        }
    }


    // =========================================================
    // 즉시 출력
    // =========================================================

    public void ShowTextImmediately()
    {
        StopAllTextCoroutines();


        targetText.text =
            displayText;


        if (showCursor)
        {
            cursorCoroutine =
                StartCoroutine(
                    BlinkCursor(displayText)
                );
        }
    }


    // =========================================================
    // Text 제거
    // =========================================================

    public void ClearText()
    {
        StopAllTextCoroutines();

        targetText.text = "";
    }


    // =========================================================
    // Coroutine 정리
    // =========================================================

    private void StopAllTextCoroutines()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );

            typingCoroutine = null;
        }


        if (cursorCoroutine != null)
        {
            StopCoroutine(
                cursorCoroutine
            );

            cursorCoroutine = null;
        }
    }
}
