using System.Collections;
using UnityEngine;


public class OperatorInfoWorldSpaceUIManager : MonoBehaviour, IOperatorInfoUI
{
    public static OperatorInfoWorldSpaceUIManager Instance { get; private set; }

    private enum UIState
    {
        Hidden,
        Showing,
        Visible,
        Hiding
    }

    private UIState state = UIState.Hidden;

    [Header("Sprite")]
    [SerializeField] private Transform targetSprite;

    [Header("Viewport Position")]

    private bool isVisible;
    private Vector3 targetWorldPosition;

    [Header("Camera Offset")]

    // 카메라 앞쪽 거리
    [SerializeField] private float distance = 5f;

    // 숨겨진 위치
    [SerializeField]
    private Vector2 hiddenOffset =
     new Vector2(-1.25f, -0.75f);

    [SerializeField]
    private Vector2 visibleOffset =
        new Vector2(-0.65f, -0.75f);


    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float fastMoveSpeed = 18f;

    private float currentMoveSpeed;

    [SerializeField] private Camera mainCamera;


    private Coroutine moveCoroutine;

    private OperatorClickHandler currentHandler;

    private void Awake()
    {
        Instance = this;

        targetWorldPosition =
            GetWorldPosition(hiddenOffset);

        targetSprite.position = targetWorldPosition;
    }



    public void Show(OperatorClickHandler handler)
    {
        currentHandler = handler;

        RefreshUI();

        currentMoveSpeed = moveSpeed;

        state = UIState.Showing;
    }

    public void Hide()
    {
        currentHandler = null;

        currentMoveSpeed = moveSpeed;

        state = UIState.Hiding;
    }

    public void ChangeOperator(OperatorClickHandler handler)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(ChangeRoutine(handler));
    }

    private IEnumerator ChangeRoutine(OperatorClickHandler handler)
    {
        currentMoveSpeed = fastMoveSpeed;

        state = UIState.Hiding;

        while (state != UIState.Hidden)
            yield return null;

        currentHandler = handler;

        RefreshUI();

        currentMoveSpeed = moveSpeed;

        state = UIState.Showing;
    }

    private Vector3 GetWorldPosition(Vector2 offset)
    {
        Transform cam = mainCamera.transform;

        float height = mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        return cam.position
            + cam.forward * distance
            + cam.right * offset.x * width
            + cam.up * offset.y * height;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector2 offset;

        switch (state)
        {
            case UIState.Showing:
            case UIState.Visible:
                offset = visibleOffset;
                break;

            default:
                offset = hiddenOffset;
                break;
        }

        targetWorldPosition =
            GetWorldPosition(offset);

        targetSprite.position = Vector3.MoveTowards(
            targetSprite.position,
            targetWorldPosition,
            currentMoveSpeed * Time.unscaledDeltaTime);

        targetSprite.rotation = mainCamera.transform.rotation;

        if (state == UIState.Showing &&
            Vector3.Distance(targetSprite.position, targetWorldPosition) < 0.03f)
        {
            state = UIState.Visible;
        }

        if (state == UIState.Hiding &&
            Vector3.Distance(targetSprite.position, targetWorldPosition) < 0.03f)
        {
            state = UIState.Hidden;
        }
    }


    private void RefreshUI()
    {
        Debug.Log(currentHandler.name);

        // 나중에
        // HP
        // Attack
        // Portrait
        // 등을 갱신
    }
}