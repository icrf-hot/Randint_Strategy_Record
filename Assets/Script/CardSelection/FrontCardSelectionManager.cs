using TMPro;
using UnityEngine;

public enum FrontActionType
{
    None,
    Attack,
    Defense
    // 나중에 회피, 반격도 추가
}

public class FrontCardSelectionManager : CardSelectionManagerBase
{
    public static FrontCardSelectionManager Instance { get; private set; }

    [Header("Front Panel Unit")]
    [SerializeField] private TMP_Text frontNumberText;

    [Header("Access TMP")]
    [SerializeField] private TMP_Text accessText;

    private Card selectedCard;

    private int selectedNumber;

    [Header("Action")]
    [SerializeField] private SpriteToggleGroup actionGroup;

    private FrontActionType actionType =
        FrontActionType.None;

    public FrontActionType ActionType =>
        actionType;

    public bool IsAccess { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // Action Value
    // =========================================================

    [Header("Action Value")]
    [SerializeField] private int defenseMultiplier = 5;
    [SerializeField] private int attackMultiplier = 100;

    public int SelectedNumber =>
        selectedNumber;

    public int DefenseBonus
    {
        get
        {
            if (actionType != FrontActionType.Defense)
                return 0;

            return selectedNumber *
                   defenseMultiplier;
        }
    }

    public int ArtsResistanceBonus
    {
        get
        {
            if (actionType != FrontActionType.Defense)
                return 0;

            return selectedNumber *
                   defenseMultiplier;
        }
    }

    public int AttackBonus
    {
        get
        {
            if (actionType != FrontActionType.Attack)
                return 0;

            return selectedNumber *
                   attackMultiplier;
        }
    }

    // =========================================================
    // Card Selection
    // =========================================================

    public override void SelectCard(Card card)
    {
        if (card == null)
            return;

        if (card.OwnerManager != null &&
            card.OwnerManager != this)
        {
            return;
        }

        // Front는 조커 사용 불가
        if (card.IsSpecialCard)
            return;

        // 같은 카드 다시 클릭
        if (selectedCard == card)
        {
            selectedCard.SetSelected(false, null);

            selectedCard = null;
            selectedNumber = 0;

            if (frontNumberText != null)
                frontNumberText.text = "";

            UpdateAccess();

            return;
        }

        // 기존 카드 선택 해제
        if (selectedCard != null)
            selectedCard.SetSelected(false, null);

        // 새로운 카드 선택
        selectedCard = card;

        selectedCard.SetSelected(
            true,
            this);

        selectedNumber =
            card.CardNumber;

        if (frontNumberText != null)
            frontNumberText.text =
                selectedNumber.ToString();

        UpdateAccess();

        Debug.Log(
            "Front Number : " +
            card.CardNumber);
    }

    // =========================================================
    // Reset
    // =========================================================

    public override void ResetSelection()
    {
        if (selectedCard != null)
            selectedCard.SetSelected(false, null);

        selectedCard = null;

        selectedNumber = 0;

        actionType =
            FrontActionType.None;

        actionGroup.ResetSelection();

        if (frontNumberText != null)
            frontNumberText.text = "";

        UpdateAccess();
    }

    // =========================================================
    // Access
    // =========================================================

    private void UpdateAccess()
    {
        IsAccess =
            selectedCard != null &&
            actionType != FrontActionType.None;

        if (accessText != null)
        {
            accessText.text =
                IsAccess
                    ? "승인됨"
                    : "승인 대기중";
        }

        if (BattleAccessManager.Instance != null)
            BattleAccessManager.Instance.Refresh();
    }

    // =========================================================
    // Attack
    // =========================================================

    public void SetAttack()
    {
        if (actionType == FrontActionType.Attack)
            return;

        actionType = FrontActionType.Attack;

        UpdateAccess();
    }


    // =========================================================
    // Defense
    // =========================================================

    public void SetDefense()
    {
        if (actionType == FrontActionType.Defense)
            return;

        actionType = FrontActionType.Defense;

        UpdateAccess();
    }
}