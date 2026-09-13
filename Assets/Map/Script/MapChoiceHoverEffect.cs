using UnityEngine;
using UnityEngine.InputSystem;

public class MapChoiceHoverEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hitArea;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Color")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.gray;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 0.3f;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float moveDuration = 0.15f;

    private bool isHovering;

    private Vector3 baseLocalPosition;
    private Vector3 startLocalPosition;
    private Vector3 targetLocalPosition;
    private float moveTimer;

    private Color startColor;
    private Color targetColor;
    private float colorTimer;

    private void Awake()
    {
        if (hitArea == null)
        {
            hitArea = GetComponent<RectTransform>();
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        isHovering = false;

        baseLocalPosition = transform.localPosition;
        startLocalPosition = baseLocalPosition;
        targetLocalPosition = baseLocalPosition;

        moveTimer = 0f;

        startColor = normalColor;
        targetColor = normalColor;
        colorTimer = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
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
        bool hoveringNow = IsMouseOver();

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

    private bool IsMouseOver()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            hitArea == null)
        {
            return false;
        }

        Vector2 mousePosition =
            mouse.position.ReadValue();

        Camera uiCamera =
            GetUICamera();

        return RectTransformUtility.RectangleContainsScreenPoint(
            hitArea,
            mousePosition,
            uiCamera
        );
    }

    private Camera GetUICamera()
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void BeginHover()
    {
        isHovering = true;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }

        targetColor = hoverColor;
        colorTimer = 0f;

        startLocalPosition = transform.localPosition;
        targetLocalPosition = baseLocalPosition;
        targetLocalPosition.x -= moveDistance;

        moveTimer = 0f;
    }


    // =========================================================
    // End Hover
    // =========================================================

    private void EndHover()
    {
        isHovering = false;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }

        targetColor = normalColor;
        colorTimer = 0f;

        startLocalPosition = transform.localPosition;
        targetLocalPosition = baseLocalPosition;

        moveTimer = 0f;
    }

    private void UpdateMovement()
    {
        if (moveDuration <= 0f)
        {
            transform.localPosition = targetLocalPosition;
            return;
        }

        moveTimer += Time.deltaTime;

        float t =
            Mathf.Clamp01(moveTimer / moveDuration);

        t = t * t * (3f - 2f * t);

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
            spriteRenderer.color = targetColor;
            return;
        }

        colorTimer += Time.deltaTime;

        float t =
            Mathf.Clamp01(colorTimer / fadeDuration);

        t = t * t * (3f - 2f * t);

        spriteRenderer.color =
            Color.Lerp(
                startColor,
                targetColor,
                t
            );
    }

    public void SetBaseLocalPosition(Vector3 position)
    {
        baseLocalPosition = position;

        /*
         * Hover 중이면 건드리지 않습니다.
         *
         * Hover 애니메이션은
         * 이 스크립트가 책임집니다.
         */
        if (isHovering)
            return;

        startLocalPosition = position;
        targetLocalPosition = position;
        transform.localPosition = position;
    }
}