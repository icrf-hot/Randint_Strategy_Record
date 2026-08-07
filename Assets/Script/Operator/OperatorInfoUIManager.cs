using System.Collections;
using UnityEngine;

public class OperatorInfoUIManager : MonoBehaviour, IOperatorInfoUI
{
    public static OperatorInfoUIManager Instance { get; private set; }

    [Header("Sprite")]
    [SerializeField] private Transform targetSprite;

    [Header("Position")]
    [SerializeField] private Transform hiddenPoint;
    [SerializeField] private Transform visiblePoint;

    [Header("Speed")]
    [SerializeField] private float slideTime = 0.35f;
    [SerializeField] private float fastSlideTime = 0.15f;

    [SerializeField] private Camera mainCamera;

    [SerializeField] private Vector3 cameraOffset;

    private Coroutine moveCoroutine;

    private OperatorClickHandler currentHandler;

    private void Awake()
    {
        Instance = this;

        targetSprite.localPosition = hiddenPoint.localPosition;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        transform.position = mainCamera.transform.position + cameraOffset;
    }

    public void Show(OperatorClickHandler handler)
    {
        currentHandler = handler;

        RefreshUI();

        MoveTo(visiblePoint.localPosition, slideTime);
    }

    public void Hide()
    {
        currentHandler = null;

        MoveTo(hiddenPoint.localPosition, slideTime);
    }

    public void ChangeOperator(OperatorClickHandler handler)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(ChangeRoutine(handler));
    }

    private IEnumerator ChangeRoutine(OperatorClickHandler handler)
    {
        yield return MoveRoutine(hiddenPoint.localPosition, fastSlideTime);

        currentHandler = handler;

        RefreshUI();

        yield return MoveRoutine(visiblePoint.localPosition, slideTime);
    }

    private void MoveTo(Vector3 target, float time)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveRoutine(target, time));
    }

    private IEnumerator MoveRoutine(Vector3 target, float time)
    {
        Vector3 start = targetSprite.localPosition;

        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;

            targetSprite.localPosition = Vector3.Lerp(
                start,
                target,
                elapsed / time);

            yield return null;
        }

        targetSprite.localPosition = target;
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