using System.Collections;
using UnityEngine;

public class MapNodeFocusObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;

    [Header("Focus Objects")]
    [SerializeField] private Transform[] choiceObjects;

    [Header("Screen Position")]
    [SerializeField] private float uiDistance = 5f;

    [Tooltip("노드를 기준으로 선택지 전체가 이동하는 화면 기준 오프셋")]
    [SerializeField] private Vector2 screenOffset = new Vector2(-2f, 0f);

    [Tooltip("선택지 사이의 세로 간격")]
    [SerializeField] private float spacing = 1.5f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float exitDuration = 0.15f;
    [SerializeField] private float startOffset = 8f;

    private Camera mapCamera;

    private MapNode node;

    private Coroutine animationCoroutine;

    // 현재 선택지가 화면에 표시되고 있는가
    private bool isVisible;

    // 현재 오른쪽으로 사라지는 중인가
    private bool isClosing;


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

        /*
         * Focus 취소 이벤트 등록
         */
        orbitCamera.OnFocusCanceled +=
            HandleFocusCanceled;

        HideImmediately();
    }


    private void OnDestroy()
    {
        /*
         * 반드시 이벤트를 해제합니다.
         */
        if (orbitCamera != null)
        {
            orbitCamera.OnFocusCanceled -=
                HandleFocusCanceled;
        }
    }


    private void Update()
    {
        if (orbitCamera == null)
            return;

        /*
         * 이 Node가 현재 Focus되었는지 확인
         */
        bool isThisNodeFocused =
            orbitCamera.FocusedNode == node;


        /*
         * =====================================================
         * Focus된 경우
         * =====================================================
         */

        if (isThisNodeFocused)
        {
            /*
             * 닫히는 중이 아니고
             * 아직 표시되지 않았다면 등장
             */
            if (!isVisible &&
                !isClosing &&
                animationCoroutine == null)
            {
                ShowObjects();

                return;
            }


            /*
             * 등장/퇴장 애니메이션이 끝난 후
             * Node를 계속 따라갑니다.
             */
            if (isVisible &&
                !isClosing &&
                animationCoroutine == null)
            {
                UpdateObjectPositions();
            }

            return;
        }


        /*
         * =====================================================
         * Focus되지 않은 경우
         * =====================================================
         */

        /*
         * 정상적으로 Focus가 풀렸다면
         * 선택지를 즉시 숨깁니다.
         *
         * 실제 CancelFocus의 경우에는
         * OnFocusCanceled가 먼저 처리하므로
         * 여기로 들어오지 않습니다.
         */
        if (!isThisNodeFocused)
        {
            /*
             * 실제로 Focus가 해제된 시점
             */
            if (isClosing)
            {
                isClosing = false;
            }


            if (isVisible)
            {
                HideImmediately();
            }

            return;
        }
    }


    // =========================================================
    // Focus 취소 이벤트
    // =========================================================

    private void HandleFocusCanceled(
        MapNode canceledNode)
    {
        /*
         * 다른 Node의 취소 이벤트는 무시
         */
        if (canceledNode != node)
            return;


        /*
         * 현재 선택지가 없다면 할 일이 없음
         */
        if (!isVisible &&
            animationCoroutine == null)
        {
            return;
        }


        /*
         * 닫히는 중이라는 것을 표시
         *
         * 이 값이 중요합니다.
         *
         * MapOrbitCamera는 카메라가 완전히 돌아갈 때까지
         * FocusedNode를 유지하기 때문입니다.
         */
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


        animationCoroutine =
            StartCoroutine(
                AnimateObjectsIn()
            );
    }


    // =========================================================
    // 즉시 숨김
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
    // 목표 위치 계산
    // =========================================================

    private bool CalculateTargetPositions(
    Vector3[] targetPositions)
    {
        if (node == null)
            return false;

        if (mapCamera == null)
            return false;

        if (choiceObjects == null ||
            choiceObjects.Length == 0)
            return false;


        /*
         * =====================================================
         * 1. Node의 화면 위치
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
         * 2. Screen → Viewport
         * =====================================================
         */

        Vector3 viewport =
            mapCamera.ScreenToViewportPoint(
                screenPosition
            );


        /*
         * =====================================================
         * 3. Node와 동일한 화면 위치에 있는
         *    카메라 앞의 기준점
         * =====================================================
         */

        Vector3 centerPosition =
            mapCamera.ViewportToWorldPoint(
                new Vector3(
                    viewport.x,
                    viewport.y,
                    uiDistance
                )
            );


        /*
         * =====================================================
         * 4. 카메라의 화면 방향
         * =====================================================
         */

        Vector3 cameraRight =
            mapCamera.transform.right;

        Vector3 cameraUp =
            mapCamera.transform.up;


        /*
         * =====================================================
         * 5. Inspector에서 지정한 화면 오프셋 적용
         * =====================================================
         *
         * screenOffset.x
         *     화면 오른쪽 / 왼쪽
         *
         * screenOffset.y
         *     화면 위 / 아래
         */

        centerPosition +=
            cameraRight * screenOffset.x;

        centerPosition +=
            cameraUp * screenOffset.y;


        /*
         * =====================================================
         * 6. 선택지 세로 정렬
         * =====================================================
         *
         * 예:
         *
         *       [0]
         *
         *       [1]
         *
         *       [2]
         *
         * 전체가 centerPosition을 중심으로 정렬됩니다.
         */

        int count =
            choiceObjects.Length;


        float centerOffset =
            (count - 1) * 0.5f;


        for (int i = 0;
             i < count;
             i++)
        {
            float verticalOffset =
                (i - centerOffset) * spacing;


            targetPositions[i] =
                centerPosition +
                cameraUp * verticalOffset;
        }


        return true;
    }


    // =========================================================
    // 현재 위치 갱신
    // =========================================================

    private void UpdateObjectPositions()
    {
        if (choiceObjects == null ||
            choiceObjects.Length == 0)
            return;


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
             * 기본 위치를 먼저 지정
             */
            obj.position =
                targetPositions[i];


            /*
             * Hover Effect가 있다면
             * 기본 위치를 알려줍니다.
             */
            MapChoiceHoverEffect hoverEffect =
                obj.GetComponent<
                    MapChoiceHoverEffect
                >();


            if (hoverEffect != null)
            {
                hoverEffect.SetBasePosition(
                    targetPositions[i]
                );
            }
        }
    }


    // =========================================================
    // 등장 애니메이션
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


        /*
         * Sprite 활성화
         */
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
            timer += Time.deltaTime;


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


            /*
             * 현재 Node의 목표 위치를 계산
             */
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
             * 카메라의 현재 오른쪽
             */
            Vector3 cameraRight =
                mapCamera.transform.right;


            /*
             * 8 → 0
             *
             * 오른쪽에서 들어오면서
             * 점점 Node 위치로 접근
             */
            float offset =
                Mathf.Lerp(
                    startOffset,
                    0f,
                    t
                );


            for (int i = 0;
                 i < choiceObjects.Length;
                 i++)
            {
                Transform obj =
                    choiceObjects[i];

                if (obj == null)
                    continue;


                /*
                 * 목표 위치에서
                 * 화면 오른쪽으로 offset
                 */
                Vector3 position =
                    targetPositions[i] +
                    cameraRight * offset;


                obj.position =
                    position;
            }


            yield return null;
        }


        /*
         * 마지막 프레임은 정확하게
         * 목표 위치에 맞춥니다.
         */
        UpdateObjectPositions();


        isVisible = true;
        animationCoroutine = null;
    }


    // =========================================================
    // 퇴장 애니메이션
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
            timer += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / exitDuration
                );


            t =
                t * t *
                (3f - 2f * t);


            /*
             * 현재 위치를 기준으로 하지 않고
             * 현재 화면상의 목표 위치를 기준으로 합니다.
             */
            Vector3[] targetPositions =
                new Vector3[
                    choiceObjects.Length
                ];


            if (!CalculateTargetPositions(
                targetPositions))
            {
                yield break;
            }


            Vector3 cameraRight =
                mapCamera.transform.right;


            /*
             * 0 → startOffset
             */
            float offset =
                Mathf.Lerp(
                    0f,
                    startOffset,
                    t
                );


            for (int i = 0;
                 i < choiceObjects.Length;
                 i++)
            {
                Transform obj =
                    choiceObjects[i];

                if (obj == null)
                    continue;


                obj.position =
                    targetPositions[i] +
                    cameraRight * offset;
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
        animationCoroutine = null;
    }
}