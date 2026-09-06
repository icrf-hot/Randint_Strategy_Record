using System.Collections;
using TMPro;
using UnityEngine;

public class MapNodeFocusObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;


    [Header("Objects")]
    [SerializeField] private Transform descriptionObject;
    [SerializeField] private Transform choicesObject;


    [Header("Description")]
    [SerializeField] private TMP_Text descriptionText;


    [Header("Choice Buttons")]
    [SerializeField] private GameObject[] choiceButtons;

    [SerializeField] private TMP_Text[] choiceTexts;


    [Header("Movement")]
    [SerializeField] private float descriptionOffset = 10f;
    [SerializeField] private float choicesOffset = 10f;


    [Header("Animation")]
    [SerializeField] private float descriptionMoveDuration = 0.15f;
    [SerializeField] private float choicesMoveDuration = 0.15f;


    private Vector3 descriptionBasePosition;
    private Vector3 choicesBasePosition;


    private Vector3 descriptionStartPosition;
    private Vector3 descriptionTargetPosition;

    private Vector3 choicesStartPosition;
    private Vector3 choicesTargetPosition;


    private float descriptionMoveTimer;
    private float choicesMoveTimer;


    private Coroutine animationCoroutine;

    private bool previousFocusState;


    private void Awake()
    {
        descriptionBasePosition =
            descriptionObject.localPosition;

        choicesBasePosition =
            choicesObject.localPosition;
    }


    private void Start()
    {
        descriptionObject.gameObject.SetActive(false);
        choicesObject.gameObject.SetActive(false);

        previousFocusState =
            orbitCamera.IsFocused;
    }


    private void OnEnable()
    {
        if (orbitCamera != null)
        {
            orbitCamera.OnFocusCanceled += OnFocusCanceled;
        }
    }


    private void OnDisable()
    {
        if (orbitCamera != null)
        {
            orbitCamera.OnFocusCanceled -= OnFocusCanceled;
        }
    }


    private void Update()
    {
        bool currentFocusState =
            orbitCamera.IsFocused;


        // Focus 시작
        if (!previousFocusState &&
            currentFocusState)
        {
            ShowObjects();
        }


        previousFocusState =
            currentFocusState;
    }


    // =========================================================
    // Focus 취소
    // =========================================================

    private void OnFocusCanceled(MapNode canceledNode)
    {
        HideObjects();
    }


    // =========================================================
    // Show
    // =========================================================

    private void ShowObjects()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }


        MapNode focusedNode =
            orbitCamera.FocusedNode;


        if (focusedNode == null)
            return;


        // -----------------------------------------------------
        // Node Description
        // -----------------------------------------------------

        if (descriptionText != null)
        {
            descriptionText.text =
                focusedNode.Description;
        }


        // -----------------------------------------------------
        // Choice 설정
        // -----------------------------------------------------

        SetupChoices(
            focusedNode
        );


        // -----------------------------------------------------
        // 활성화
        // -----------------------------------------------------

        descriptionObject.gameObject.SetActive(true);
        choicesObject.gameObject.SetActive(true);


        // -----------------------------------------------------
        // Description 위치
        // -----------------------------------------------------

        descriptionStartPosition =
            descriptionBasePosition +
            Vector3.left * descriptionOffset;

        descriptionTargetPosition =
            descriptionBasePosition;


        // -----------------------------------------------------
        // Choices 위치
        // -----------------------------------------------------

        choicesStartPosition =
            choicesBasePosition +
            Vector3.right * choicesOffset;

        choicesTargetPosition =
            choicesBasePosition;


        descriptionObject.localPosition =
            descriptionStartPosition;

        choicesObject.localPosition =
            choicesStartPosition;


        descriptionMoveTimer = 0f;
        choicesMoveTimer = 0f;


        animationCoroutine =
            StartCoroutine(
                MoveIn()
            );
    }


    // =========================================================
    // Choice 설정
    // =========================================================

    private void SetupChoices(MapNode node)
    {
        // 모든 버튼 비활성화
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].SetActive(false);

            if (i < choiceTexts.Length &&
                choiceTexts[i] != null)
            {
                choiceTexts[i].text = "";
            }
        }

        MapChoice[] choices = node.Choices;

        if (choices == null)
            return;

        // 최대 3개까지만 표시
        int count = Mathf.Min(
            choices.Length,
            choiceButtons.Length
        );

        for (int i = 0; i < count; i++)
        {
            MapChoice choice = choices[i];

            if (choice == null)
                continue;

            // 버튼 활성화
            choiceButtons[i].SetActive(true);

            // TMP 변경
            if (i < choiceTexts.Length &&
                choiceTexts[i] != null)
            {
                choiceTexts[i].text =
                    choice.ChoiceText;
            }
        }
    }


    // =========================================================
    // Hide
    // =========================================================

    private void HideObjects()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }


        descriptionStartPosition =
            descriptionObject.localPosition;

        descriptionTargetPosition =
            descriptionBasePosition +
            Vector3.left * descriptionOffset;


        choicesStartPosition =
            choicesObject.localPosition;

        choicesTargetPosition =
            choicesBasePosition +
            Vector3.right * choicesOffset;


        descriptionMoveTimer = 0f;
        choicesMoveTimer = 0f;


        animationCoroutine =
            StartCoroutine(
                MoveOut()
            );
    }


    // =========================================================
    // Move In
    // =========================================================

    private IEnumerator MoveIn()
    {
        bool descriptionFinished = false;
        bool choicesFinished = false;


        while (!descriptionFinished ||
               !choicesFinished)
        {
            if (!descriptionFinished)
            {
                descriptionMoveTimer +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        descriptionMoveTimer /
                        descriptionMoveDuration
                    );


                t =
                    t * t *
                    (3f - 2f * t);


                descriptionObject.localPosition =
                    Vector3.Lerp(
                        descriptionStartPosition,
                        descriptionTargetPosition,
                        t
                    );


                if (descriptionMoveTimer >=
                    descriptionMoveDuration)
                {
                    descriptionObject.localPosition =
                        descriptionTargetPosition;

                    descriptionFinished = true;
                }
            }


            if (!choicesFinished)
            {
                choicesMoveTimer +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        choicesMoveTimer /
                        choicesMoveDuration
                    );


                t =
                    t * t *
                    (3f - 2f * t);


                choicesObject.localPosition =
                    Vector3.Lerp(
                        choicesStartPosition,
                        choicesTargetPosition,
                        t
                    );


                if (choicesMoveTimer >=
                    choicesMoveDuration)
                {
                    choicesObject.localPosition =
                        choicesTargetPosition;

                    choicesFinished = true;
                }
            }


            yield return null;
        }


        animationCoroutine = null;
    }


    // =========================================================
    // Move Out
    // =========================================================

    private IEnumerator MoveOut()
    {
        bool descriptionFinished = false;
        bool choicesFinished = false;


        while (!descriptionFinished ||
               !choicesFinished)
        {
            if (!descriptionFinished)
            {
                descriptionMoveTimer +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        descriptionMoveTimer /
                        descriptionMoveDuration
                    );


                t =
                    t * t *
                    (3f - 2f * t);


                descriptionObject.localPosition =
                    Vector3.Lerp(
                        descriptionStartPosition,
                        descriptionTargetPosition,
                        t
                    );


                if (descriptionMoveTimer >=
                    descriptionMoveDuration)
                {
                    descriptionObject.localPosition =
                        descriptionTargetPosition;

                    descriptionFinished = true;
                }
            }


            if (!choicesFinished)
            {
                choicesMoveTimer +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        choicesMoveTimer /
                        choicesMoveDuration
                    );


                t =
                    t * t *
                    (3f - 2f * t);


                choicesObject.localPosition =
                    Vector3.Lerp(
                        choicesStartPosition,
                        choicesTargetPosition,
                        t
                    );


                if (choicesMoveTimer >=
                    choicesMoveDuration)
                {
                    choicesObject.localPosition =
                        choicesTargetPosition;

                    choicesFinished = true;
                }
            }


            yield return null;
        }


        // 이동이 끝난 후 전체 Choices 숨김
        descriptionObject.gameObject.SetActive(false);
        choicesObject.gameObject.SetActive(false);


        // 다음 Focus를 위해 위치 복구
        descriptionObject.localPosition =
            descriptionBasePosition;

        choicesObject.localPosition =
            choicesBasePosition;


        animationCoroutine = null;
    }
}