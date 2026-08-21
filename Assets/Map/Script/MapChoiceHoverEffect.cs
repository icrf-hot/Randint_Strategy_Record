using UnityEngine;
using UnityEngine.InputSystem;

public class MapChoiceHoverEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera mapCamera;

    [Header("Color")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.gray;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 0.3f;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float moveDuration = 0.15f;

    private bool isHovering;

    // MapNodeFocusObject가 지정하는 원래 위치
    private Vector3 basePosition;

    // 이동 애니메이션
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float moveTimer;

    // 색상 애니메이션
    private Color startColor;
    private Color targetColor;

    private float colorTimer;


    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (mapCamera == null)
        {
            Debug.LogError(
                "MapChoiceHoverEffect: Map Camera가 지정되지 않았습니다."
            );
        }
    }


    private void OnEnable()
    {
        isHovering = false;

        basePosition =
            transform.position;

        startPosition =
            basePosition;

        targetPosition =
            basePosition;

        moveTimer = 0f;

        startColor =
            normalColor;

        targetColor =
            normalColor;

        colorTimer = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                normalColor;
        }
    }


    private void Update()
    {
        CheckHover();

        UpdateColor();
    }


    private void LateUpdate()
    {
        UpdateMovement();
    }


    // =========================================================
    // Hover 검출
    // =========================================================

    private void CheckHover()
    {
        if (mapCamera == null)
            return;

        Mouse mouse =
            Mouse.current;

        if (mouse == null)
            return;

        Vector2 mousePosition =
            mouse.position.ReadValue();


        Ray ray =
            mapCamera.ScreenPointToRay(
                mousePosition
            );


        RaycastHit2D hit =
            Physics2D.GetRayIntersection(
                ray
            );


        bool hoveringNow =
            hit.collider != null &&
            hit.collider.transform == transform;


        if (hoveringNow == isHovering)
            return;


        if (hoveringNow)
        {
            BeginHover();
        }
        else
        {
            EndHover();
        }
    }


    // =========================================================
    // Hover 시작
    // =========================================================

    private void BeginHover()
    {
        isHovering = true;


        // -----------------------------
        // Color
        // -----------------------------

        startColor =
            spriteRenderer.color;

        targetColor =
            hoverColor;

        colorTimer = 0f;


        // -----------------------------
        // Position
        // -----------------------------

        startPosition =
            transform.position;


        /*
         * 카메라의 오른쪽 방향을 가져옵니다.
         *
         * 화면에서:
         *
         *        오른쪽 →
         *
         * camera.transform.right
         *
         * 따라서 왼쪽은 반대 방향입니다.
         */
        Vector3 screenLeft =
            -mapCamera.transform.right;


        targetPosition =
            basePosition +
            screenLeft * moveDistance;


        moveTimer = 0f;
    }


    // =========================================================
    // Hover 종료
    // =========================================================

    private void EndHover()
    {
        isHovering = false;


        // -----------------------------
        // Color
        // -----------------------------

        startColor =
            spriteRenderer.color;

        targetColor =
            normalColor;

        colorTimer = 0f;


        // -----------------------------
        // Position
        // -----------------------------

        startPosition =
            transform.position;

        targetPosition =
            basePosition;

        moveTimer = 0f;
    }


    // =========================================================
    // Movement
    // =========================================================

    private void UpdateMovement()
    {
        if (moveDuration <= 0f)
        {
            transform.position =
                targetPosition;

            return;
        }


        moveTimer +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                moveTimer /
                moveDuration
            );


        // SmoothStep
        t =
            t * t *
            (3f - 2f * t);


        transform.position =
            Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );
    }


    // =========================================================
    // Color
    // =========================================================

    private void UpdateColor()
    {
        if (spriteRenderer == null)
            return;

        if (fadeDuration <= 0f)
        {
            spriteRenderer.color =
                targetColor;

            return;
        }


        colorTimer +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                colorTimer /
                fadeDuration
            );


        t =
            t * t *
            (3f - 2f * t);


        spriteRenderer.color =
            Color.Lerp(
                startColor,
                targetColor,
                t
            );
    }


    // =========================================================
    // MapNodeFocusObject에서 호출
    // =========================================================

    public void SetBasePosition(
        Vector3 position)
    {
        basePosition =
            position;


        /*
         * Hover 중이 아닐 때만
         * 실제 위치를 갱신합니다.
         *
         * Hover 중에는 Hover 애니메이션이
         * 위치를 관리합니다.
         */
        if (!isHovering)
        {
            startPosition =
                position;

            targetPosition =
                position;

            transform.position =
                position;
        }
    }
}