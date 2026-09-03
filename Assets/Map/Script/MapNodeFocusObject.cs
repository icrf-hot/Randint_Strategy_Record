using System.Collections;
using UnityEngine;
using TMPro;

public class MapNodeFocusObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;

    /*
     * 반드시 MapOrbitCamera의 자식으로 둡니다.
     *
     * 예:
     *
     * Map3DCamera
     *     └─ MapUIRoot
     */
    [SerializeField] private Transform uiRoot;

    [Header("Focus Objects")]
    [SerializeField] private Transform[] choiceObjects;

    [Header("Screen Position")]
    [SerializeField] private float uiDistance = 5f;

    [Tooltip("Node를 기준으로 한 화면상의 위치")]
    [SerializeField]
    private Vector2 screenOffset =
        new Vector2(-2f, 0f);

    [Tooltip("선택지 사이의 세로 간격")]
    [SerializeField]
    private float spacing = 1.5f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.4f;

    [SerializeField] private float exitDuration = 0.15f;

    [Tooltip("화면 오른쪽에서 들어오는 거리")]
    [SerializeField] private float startOffset = 8f;


    private Camera mapCamera;

    private MapNode node;

    private Coroutine animationCoroutine;

    private bool isVisible;
    private bool isClosing;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        node =
            GetComponent<MapNode>();

        if (node == null)
        {
            Debug.LogError(
                "MapNodeFocusObject: " +
                "같은 GameObject에 MapNode이 필요합니다."
            );

            enabled = false;
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (orbitCamera == null)
        {
            Debug.LogError(
                "MapNodeFocusObject: " +
                "Orbit Camera가 지정되지 않았습니다."
            );

            enabled = false;
            return;
        }


        mapCamera =
            orbitCamera.GetComponent<Camera>();


        if (mapCamera == null)
        {
            Debug.LogError(
                "MapNodeFocusObject: " +
                "MapOrbitCamera에 Camera가 없습니다."
            );

            enabled = false;
            return;
        }


        if (uiRoot == null)
        {
            Debug.LogError(
                "MapNodeFocusObject: " +
                "UI Root가 지정되지 않았습니다."
            );

            enabled = false;
            return;
        }


        /*
         * Focus 취소 이벤트
         */
        orbitCamera.OnFocusCanceled +=
            HandleFocusCanceled;


        HideImmediately();
    }


    private void OnDestroy()
    {
        if (orbitCamera != null)
        {
            orbitCamera.OnFocusCanceled -=
                HandleFocusCanceled;
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (orbitCamera == null)
            return;


        bool isThisNodeFocused =
            orbitCamera.FocusedNode == node;


        // =====================================================
        // Focus 상태
        // =====================================================

        if (isThisNodeFocused)
        {
            if (!isVisible &&
                !isClosing &&
                animationCoroutine == null)
            {
                ShowObjects();
                return;
            }


            /*
             * 등장 애니메이션이 끝난 이후
             *
             * Node의 화면 위치를 계속 추적합니다.
             */
            if (isVisible &&
                !isClosing &&
                animationCoroutine == null)
            {
                UpdateObjectPositions();
            }

            return;
        }


        // =====================================================
        // Focus 해제
        // =====================================================

        if (isVisible &&
            !isClosing)
        {
            HideImmediately();
        }
    }


    // =========================================================
    // Focus Cancel
    // =========================================================

    private void HandleFocusCanceled(
        MapNode canceledNode)
    {
        if (canceledNode != node)
            return;


        if (!isVisible &&
            animationCoroutine == null)
        {
            return;
        }


        isClosing = true;


        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );
        }


        animationCoroutine =
            StartCoroutine(
                AnimateObjectsOut()
            );
    }


    // =========================================================
    // Show
    // =========================================================

    private void ShowObjects()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );
        }

        isClosing = false;

        // 현재 MapNode의 선택지 텍스트를 적용
        SetChoiceTexts();

        animationCoroutine =
            StartCoroutine(
                AnimateObjectsIn()
            );
    }


    // =========================================================
    // Hide
    // =========================================================

    private void HideImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine = null;
        }


        isVisible = false;
        isClosing = false;


        if (choiceObjects == null)
            return;


        foreach (Transform obj in choiceObjects)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
            }
        }
    }


    // =========================================================
    // Node 화면 위치 → Camera Local 좌표
    // =========================================================

    private bool CalculateTargetPositions(
        Vector3[] targetPositions)
    {
        if (node == null ||
            mapCamera == null ||
            uiRoot == null)
        {
            return false;
        }


        if (choiceObjects == null ||
            choiceObjects.Length == 0)
        {
            return false;
        }


        /*
         * =====================================================
         * 1. Node를 Screen 좌표로 변환
         * =====================================================
         */

        Vector3 screenPosition =
            mapCamera.WorldToScreenPoint(
                node.transform.position
            );


        if (screenPosition.z <= 0f)
            return false;


        /*
         * =====================================================
         * 2. Viewport 좌표
         * =====================================================
         */

        Vector3 viewport =
            mapCamera.ScreenToViewportPoint(
                screenPosition
            );


        /*
         * =====================================================
         * 3. Camera Local 좌표 계산
         * =====================================================
         *
         * uiDistance 거리의 카메라 평면에서
         * Node와 동일한 화면 위치를 계산합니다.
         */

        float halfHeight =
            Mathf.Tan(
                mapCamera.fieldOfView *
                0.5f *
                Mathf.Deg2Rad
            ) *
            uiDistance;


        float halfWidth =
            halfHeight *
            mapCamera.aspect;


        float localX =
            (viewport.x - 0.5f) *
            halfWidth *
            2f;


        float localY =
            (viewport.y - 0.5f) *
            halfHeight *
            2f;


        /*
         * Inspector Offset
         */
        localX +=
            screenOffset.x;

        localY +=
            screenOffset.y;


        /*
         * =====================================================
         * 4. 선택지 세로 정렬
         * =====================================================
         *
         * 예:
         *
         *      [0]
         *
         *      [1]
         *
         *      [2]
         */

        int count =
            choiceObjects.Length;


        float centerOffset =
            (count - 1) * 0.5f;


        for (int i = 0;
             i < count;
             i++)
        {
            float y =
                localY +
                (i - centerOffset) *
                spacing;


            /*
             * UI Root가 Camera의 자식이므로
             *
             * X = 화면 좌우
             * Y = 화면 상하
             * Z = 카메라와의 거리
             */

            targetPositions[i] =
                new Vector3(
                    localX,
                    y,
                    uiDistance
                );
        }


        return true;
    }


    // =========================================================
    // 위치 갱신
    // =========================================================

    private void UpdateObjectPositions()
    {
        if (choiceObjects == null ||
            choiceObjects.Length == 0)
        {
            return;
        }


        Vector3[] targetPositions =
            new Vector3[
                choiceObjects.Length
            ];


        if (!CalculateTargetPositions(
            targetPositions))
        {
            return;
        }


        for (int i = 0;
             i < choiceObjects.Length;
             i++)
        {
            Transform obj =
                choiceObjects[i];

            if (obj == null)
                continue;


            /*
             * 중요:
             *
             * World Position이 아닙니다.
             *
             * Camera UI Root 기준 Local Position입니다.
             */
            obj.localPosition =
                targetPositions[i];


            /*
             * Hover Effect에
             * 기본 위치 전달
             */
            MapChoiceHoverEffect hover =
                obj.GetComponent<
                    MapChoiceHoverEffect
                >();


            if (hover != null)
            {
                hover.SetBaseLocalPosition(
                    targetPositions[i]
                );
            }
        }
    }


    // =========================================================
    // 등장
    // =========================================================

    private IEnumerator AnimateObjectsIn()
    {
        if (choiceObjects == null ||
            choiceObjects.Length == 0)
        {
            isVisible = true;
            animationCoroutine = null;
            yield break;
        }


        foreach (Transform obj in choiceObjects)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
            }
        }


        float timer = 0f;


        while (timer < duration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / duration
                );


            /*
             * SmoothStep
             */
            t =
                t * t *
                (3f - 2f * t);


            Vector3[] targetPositions =
                new Vector3[
                    choiceObjects.Length
                ];


            if (!CalculateTargetPositions(
                targetPositions))
            {
                yield break;
            }


            /*
             * =================================================
             * 핵심
             * =================================================
             *
             * 월드 X축이 아닙니다.
             *
             * Camera Local X축입니다.
             *
             * 따라서 카메라가 어느 방향을 바라보든
             * "화면 오른쪽"에서 들어옵니다.
             */

            for (int i = 0;
                 i < choiceObjects.Length;
                 i++)
            {
                Transform obj =
                    choiceObjects[i];

                if (obj == null)
                    continue;


                Vector3 start =
                    targetPositions[i];


                /*
                 * 화면 오른쪽
                 */
                start.x +=
                    startOffset;


                /*
                 * 화면 오른쪽 → 목표 위치
                 */
                obj.localPosition =
                    Vector3.Lerp(
                        start,
                        targetPositions[i],
                        t
                    );
            }


            yield return null;
        }


        UpdateObjectPositions();


        isVisible = true;
        animationCoroutine = null;
    }


    // =========================================================
    // 퇴장
    // =========================================================

    private IEnumerator AnimateObjectsOut()
    {
        if (choiceObjects == null ||
            choiceObjects.Length == 0)
        {
            HideImmediately();
            yield break;
        }


        float timer = 0f;


        while (timer < exitDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / exitDuration
                );


            t =
                t * t *
                (3f - 2f * t);


            Vector3[] targetPositions =
                new Vector3[
                    choiceObjects.Length
                ];


            if (!CalculateTargetPositions(
                targetPositions))
            {
                yield break;
            }


            for (int i = 0;
                 i < choiceObjects.Length;
                 i++)
            {
                Transform obj =
                    choiceObjects[i];

                if (obj == null)
                    continue;


                Vector3 end =
                    targetPositions[i];


                /*
                 * 화면 오른쪽
                 */
                end.x +=
                    startOffset;


                /*
                 * 목표 위치 → 오른쪽
                 */
                obj.localPosition =
                    Vector3.Lerp(
                        targetPositions[i],
                        end,
                        t
                    );
            }


            yield return null;
        }


        foreach (Transform obj in choiceObjects)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
            }
        }


        isVisible = false;
        isClosing = false;
        animationCoroutine = null;
    }

    private void SetChoiceTexts()
    {
        if (node == null)
            return;

        if (choiceObjects == null)
            return;

        MapNodeChoice[] choices =
            node.Choices;

        for (int i = 0;
             i < choiceObjects.Length;
             i++)
        {
            Transform obj =
                choiceObjects[i];

            if (obj == null)
                continue;

            TMP_Text text =
                obj.GetComponentInChildren<TMP_Text>();

            if (text == null)
                continue;

            if (choices != null &&
                i < choices.Length)
            {
                text.text =
                    choices[i].text;
            }
            else
            {
                text.text = "";
            }
        }
    }
}