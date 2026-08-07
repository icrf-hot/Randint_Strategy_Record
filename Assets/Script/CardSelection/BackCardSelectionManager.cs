using TMPro;
using UnityEngine;

public class BackCardSelectionManager : CardSelectionManagerBase
{
    [Header("Back UI")]
    [SerializeField] private TMP_Text firstNumberText;
    [SerializeField] private TMP_Text secondNumberText;

    [Header("Access TMP")]
    [SerializeField] private TMP_Text accessText;

    private Card firstCard;
    private Card secondCard;

    private int firstNumber;
    private int secondNumber;

    public static BackCardSelectionManager Instance { get; private set; }

    public bool IsAccess { get; private set; }

    //private void Awake()
    //{
    //    if (Instance != null && Instance != this)
    //    {
    //        Destroy(gameObject);
    //        return;
    //    }

    //    Instance = this;
    //}

    public override void SelectCard(Card card)
    {
        if (card == null)
            return;

        // 조커 사용 불가
        if (card.IsSpecialCard)
            return;

        // 첫 번째 카드 다시 클릭
        if (card == firstCard)
        {
            ClearFirstCard();
            return;
        }

        // 두 번째 카드 다시 클릭
        if (card == secondCard)
        {
            ClearSecondCard();
            return;
        }

        // 첫 번째 슬롯
        if (firstCard == null)
        {
            SetFirstCard(card);
            return;
        }

        // 두 번째 슬롯
        if (secondCard == null)
        {
            SetSecondCard(card);
            return;
        }
    }

    private void SetFirstCard(Card card)
    {
        firstCard = card;
        firstCard.SetSelected(true, this);

        firstNumber = card.CardNumber;

        if (firstNumberText != null)
            firstNumberText.text = firstNumber.ToString();

        UpdateAccess();
    }

    private void SetSecondCard(Card card)
    {
        secondCard = card;
        secondCard.SetSelected(true, this);

        secondNumber = card.CardNumber;

        if (secondNumberText != null)
            secondNumberText.text = secondNumber.ToString();

        UpdateAccess();
    }

    private void ClearFirstCard()
    {
        if (firstCard != null)
            firstCard.SetSelected(false, null);

        firstCard = null;
        firstNumber = 0;

        if (firstNumberText != null)
            firstNumberText.text = "";

        UpdateAccess();
    }
    private void ClearSecondCard()
    {
        if (secondCard != null)
            secondCard.SetSelected(false, null);

        secondCard = null;
        secondNumber = 0;

        if (secondNumberText != null)
            secondNumberText.text = "";

        UpdateAccess();
    }

    public override void ResetSelection()
    {
        ClearFirstCard();
        ClearSecondCard();

        UpdateAccess();
    }

    private void UpdateAccess()
    {
        IsAccess =
            firstCard != null &&
            secondCard != null;

        if (accessText != null)
            accessText.text = IsAccess ? "승인됨" : "승인 대기중";

        BattleAccessManager.Instance.Refresh();
    }
}