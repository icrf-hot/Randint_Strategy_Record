using TMPro;
using UnityEngine;

public enum FrontActionType
{
    None,
    Attack,
    Defense
    //나중에 회피, 반격도 넣어보기
}

public class FrontCardSelectionManager : CardSelectionManagerBase
{
    [Header("Front Panel Unit")]
    [SerializeField] private TMP_Text frontNumberText;

    [Header("Access TMP")]
    [SerializeField] private TMP_Text accessText;

    private Card selectedCard;

    private int selectedNumber;

    [Header("Action")]

    [SerializeField] private SpriteToggleGroup actionGroup;

    private FrontActionType actionType = FrontActionType.None;
    public FrontActionType ActionType => actionType;

    public bool IsAccess { get; private set; }

    public override void SelectCard(Card card)
    {

        if (card.OwnerManager != null && card.OwnerManager != this)
        {
            return;
        }

        if (card == null)
            return;

        // Front는 조커 사용 불가
        if (card.IsSpecialCard)
            return;

        if (selectedCard == card)
        {
            selectedCard.SetSelected(false, null);
            selectedCard = null;

            selectedNumber = 0;
            frontNumberText.text = "";

            UpdateAccess();


            return;
        }

        if (selectedCard != null)
            selectedCard.SetSelected(false, null);

        selectedCard = card;
        selectedCard.SetSelected(true, this);

        selectedNumber = card.CardNumber;
        frontNumberText.text = selectedNumber.ToString();

        UpdateAccess();

        Debug.Log("Front Number : " + card.CardNumber);
    }

    public override void ResetSelection()
    {
        if (selectedCard != null)
            selectedCard.SetSelected(false, null);

        selectedCard = null;

        selectedNumber = 0;

        actionType = FrontActionType.None;
        actionGroup.ResetSelection();

        if (frontNumberText != null)
            frontNumberText.text = "";

        UpdateAccess();
    }

    private void UpdateAccess()
    {
        IsAccess = 
            selectedCard != null && 
            actionType != FrontActionType.None;

        if (accessText != null)
            accessText.text = IsAccess ? "승인됨" : "승인 대기중";

        BattleAccessManager.Instance.Refresh();
    }

    public void SetAttack()
    {
        if (actionType == FrontActionType.Attack)
            return;

        actionType = FrontActionType.Attack;

        UpdateAccess();
    }

    public void SetDefense()
    {
        if (actionType == FrontActionType.Defense)
            return;

        actionType = FrontActionType.Defense;

        UpdateAccess();
    }
}