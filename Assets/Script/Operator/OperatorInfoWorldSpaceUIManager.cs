using System.Collections;
using UnityEngine;
using TMPro;

public class OperatorInfoWorldSpaceUIManager : MonoBehaviour, IOperatorInfoUI
{
    public static OperatorInfoWorldSpaceUIManager Instance { get; private set; }

    private enum UIState
    {
        Hidden,
        Showing,
        Visible,
        Changing,
        Hiding
    }



    private UIState state = UIState.Hidden;

    [Header("Sprite")]
    [SerializeField] private Transform targetSprite;

    [Header("Operator Information")]
    [SerializeField] private TMP_Text operatorNameText;
    [SerializeField] private TMP_Text operatorInfoText;

    [Header("Realtime Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text artsResistanceText;
    [SerializeField] private TMP_Text attackSpeedText;

    [Header("Camera Viewport Position")]

    [SerializeField]
    private float distance = 5f;

    // 화면 밖
    [SerializeField]
    private Vector2 hiddenViewport = new Vector2(-0.10f, 0.15f);

    // 최종 표시 위치
    [SerializeField]
    private Vector2 visibleViewport = new Vector2(0.30f, 0.15f);

    // 오퍼레이터 변경 시 잠시 들어가는 위치
    [SerializeField]
    private Vector2 changeViewport = new Vector2(0.10f, 0.15f);

    [Header("Move Speed")]

    [SerializeField] private float moveSpeed = 8f;

    [SerializeField] private float fastMoveSpeed = 25f;

    private float currentMoveSpeed;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private OperatorClickHandler currentHandler;

    private Coroutine changeCoroutine;

    [Header("Change Frame Time")]
    [SerializeField] private float changeHideTime = 0.12f;




    private void Awake()
    {
        Instance = this;

        currentMoveSpeed = moveSpeed;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetSprite != null)
        {
            targetSprite.position =
                GetWorldPosition(hiddenViewport);
        }
    }

    // ============================================================
    // UI 표시
    // ============================================================

    public void Show(OperatorClickHandler handler)
    {
        if (handler == null)
            return;

        StopChangeCoroutine();

        currentHandler = handler;

        RefreshUI();

        currentMoveSpeed = moveSpeed;

        state = UIState.Showing;
    }

    // ============================================================
    // UI 숨김
    // ============================================================

    public void Hide()
    {
        StopChangeCoroutine();

        currentHandler = null;

        currentMoveSpeed = moveSpeed;

        state = UIState.Hiding;
    }

    // ============================================================
    // 다른 오퍼레이터로 변경
    // ============================================================

    public void ChangeOperator(OperatorClickHandler handler)
    {
        if (handler == null)
            return;

        StopChangeCoroutine();

        changeCoroutine =
            StartCoroutine(ChangeRoutine(handler));
    }

    private IEnumerator ChangeRoutine(OperatorClickHandler handler)
    {
        // --------------------------------------------------------
        // 1. 빠르게 안쪽으로 이동
        // --------------------------------------------------------

        currentMoveSpeed = fastMoveSpeed;
        state = UIState.Changing;

        // 정보 교체 시점까지 대기
        yield return new WaitForSecondsRealtime(changeHideTime);

        // --------------------------------------------------------
        // 2. 정보 교체
        // --------------------------------------------------------

        currentHandler = handler;

        RefreshUI();

        // --------------------------------------------------------
        // 3. 다시 등장
        // --------------------------------------------------------

        currentMoveSpeed = moveSpeed;
        state = UIState.Showing;

        changeCoroutine = null;
    }

    private void StopChangeCoroutine()
    {
        if (changeCoroutine != null)
        {
            StopCoroutine(changeCoroutine);
            changeCoroutine = null;
        }
    }

    // ============================================================
    // 카메라 기준 World Position
    // ============================================================

    private Vector3 GetWorldPosition(Vector2 viewport)
    {
        if (mainCamera == null)
            return transform.position;

        return mainCamera.ViewportToWorldPoint(
            new Vector3(
                viewport.x,
                viewport.y,
                distance));
    }

    // ============================================================
    // 매 프레임 UI 이동
    // ============================================================

    private void LateUpdate()
    {
        if (mainCamera == null || targetSprite == null)
            return;

        Vector2 viewport;

        switch (state)
        {
            case UIState.Showing:
            case UIState.Visible:
                viewport = visibleViewport;
                break;

            case UIState.Changing:
                viewport = changeViewport;
                break;

            case UIState.Hiding:
                viewport = hiddenViewport;
                break;

            default:
                viewport = hiddenViewport;
                break;
        }

        Vector3 targetPosition =
            GetWorldPosition(viewport);

        targetSprite.position = Vector3.MoveTowards(
            targetSprite.position,
            targetPosition,
            currentMoveSpeed * Time.unscaledDeltaTime);

        targetSprite.rotation =
            mainCamera.transform.rotation;

        // 표시 완료
        if (state == UIState.Showing &&
            Vector3.Distance(
                targetSprite.position,
                targetPosition) < 0.03f)
        {
            targetSprite.position = targetPosition;
            state = UIState.Visible;
        }

        // 변경용 중간 위치 도착
        if (state == UIState.Changing &&
            Vector3.Distance(
                targetSprite.position,
                targetPosition) < 0.03f)
        {
            targetSprite.position = targetPosition;
        }

        // 완전히 숨김
        if (state == UIState.Hiding &&
            Vector3.Distance(
                targetSprite.position,
                targetPosition) < 0.03f)
        {
            targetSprite.position = targetPosition;
            state = UIState.Hidden;
        }
    }

    // ============================================================
    // UI 갱신
    // ============================================================

    private void RefreshUI()
    {
        if (currentHandler == null)
            return;

        Operator op =
            currentHandler.GetComponent<Operator>();

        if (op == null)
            return;

        OperatorData data = op.Data;

        if (data == null)
            return;

        // 기본 정보
        if (operatorNameText != null)
            operatorNameText.text = data.OperatorName;

        if (operatorInfoText != null)
            operatorInfoText.text = data.Info;

        // 실시간 스탯
        if (hpText != null)
            hpText.text = $"{op.CurrentHP} / {op.MaxHP}";

        if (attackText != null)
            attackText.text = op.Attack.ToString();

        if (defenseText != null)
            defenseText.text = op.Defense.ToString();

        if (artsResistanceText != null)
            artsResistanceText.text =
                op.ArtsResistance.ToString();

        if (attackSpeedText != null)
            attackSpeedText.text =
                op.AttackSpeed.ToString("0.00");
    }
}