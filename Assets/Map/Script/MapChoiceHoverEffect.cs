using UnityEngine;
using UnityEngine.InputSystem;

public class MapChoiceHoverEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera mapCamera;

    [Header("Color")]
    [SerializeField]
    private Color normalColor =
        Color.white;

    [SerializeField]
    private Color hoverColor =
        Color.gray;

    [Header("Movement")]
    [SerializeField]
    private float moveDistance =
        0.3f;

    [Header("Animation")]
    [SerializeField]
    private float fadeDuration =
        0.15f;

    [SerializeField]
    private float moveDuration =
        0.15f;


    private bool isHovering;


    // 기본 Local Position
    private Vector3 baseLocalPosition;


    // 이동
    private Vector3 startLocalPosition;
    private Vector3 targetLocalPosition;

    private float moveTimer;


    // 색상
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
    }


    private void OnEnable()
    {
        isHovering = false;

        baseLocalPosition =
            transform.localPosition;

        startLocalPosition =
            baseLocalPosition;

        targetLocalPosition =
            baseLocalPosition;

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
        UpdateMovement();
    }


    // =========================================================
    // Hover Detection
    // =========================================================

    private void CheckHover()
    {
        if (mapCamera == null)
            return;

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        Vector2 mousePosition =
            mouse.position.ReadValue();

        /*
         * =====================================================
         * 마우스의 Screen 좌표를
         * 이 Sprite가 존재하는 월드 깊이로 변환
         * =====================================================
         */

        Vector3 spriteScreenPosition =
            mapCamera.WorldToScreenPoint(
                transform.position
            );

        Vector3 mouseWorldPosition =
            mapCamera.ScreenToWorldPoint(
                new Vector3(
                    mousePosition.x,
                    mousePosition.y,
                    spriteScreenPosition.z
                )
            );


        /*
         * =====================================================
         * 해당 월드 위치에 있는 2D Collider 검색
         * =====================================================
         */

        Collider2D hit =
            Physics2D.OverlapPoint(
                mouseWorldPosition
            );


        bool hoveringNow =
            hit != null &&
            hit.transform == transform;


        /*
         * 상태가 변하지 않았다면 아무것도 하지 않음
         */

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
    // Begin Hover
    // =========================================================

    private void BeginHover()
    {
        isHovering = true;


        // -------------------------
        // Color
        // -------------------------

        startColor =
            spriteRenderer.color;

        targetColor =
            hoverColor;

        colorTimer = 0f;


        // -------------------------
        // Position
        // -------------------------

        startLocalPosition =
            transform.localPosition;


        targetLocalPosition =
            baseLocalPosition;


        /*
         * 화면 왼쪽으로 이동
         *
         * Camera Local X축이
         * 화면의 오른쪽입니다.
         */
        targetLocalPosition.x -=
            moveDistance;


        moveTimer = 0f;
    }


    // =========================================================
    // End Hover
    // =========================================================

    private void EndHover()
    {
        isHovering = false;


        // -------------------------
        // Color
        // -------------------------

        startColor =
            spriteRenderer.color;

        targetColor =
            normalColor;

        colorTimer = 0f;


        // -------------------------
        // Position
        // -------------------------

        startLocalPosition =
            transform.localPosition;

        targetLocalPosition =
            baseLocalPosition;

        moveTimer = 0f;
    }


    // =========================================================
    // Movement
    // =========================================================

    private void UpdateMovement()
    {
        if (moveDuration <= 0f)
        {
            transform.localPosition =
                targetLocalPosition;

            return;
        }


        moveTimer +=
            Time.deltaTime;


        float t =
            Mathf.Clamp01(
                moveTimer /
                moveDuration
            );


        t =
            t * t *
            (3f - 2f * t);


        transform.localPosition =
            Vector3.Lerp(
                startLocalPosition,
                targetLocalPosition,
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
    // MapNodeFocusObject
    // =========================================================

    public void SetBaseLocalPosition(
        Vector3 position)
    {
        baseLocalPosition =
            position;


        /*
         * Hover 중이면 건드리지 않습니다.
         *
         * Hover 애니메이션은
         * 이 스크립트가 책임집니다.
         */
        if (isHovering)
            return;


        startLocalPosition =
            position;

        targetLocalPosition =
            position;

        transform.localPosition =
            position;
    }
}