using TMPro;
using UnityEngine;

public enum CardAttribute
{
    None,
    ARTS,
    BURN,
    NERVOUS,
    NECROSIS,
    CORROSION
}

public class MiddleCardSelectionManager : CardSelectionManagerBase
{
    public static MiddleCardSelectionManager Instance { get; private set; }

    [Header("속성 숫자")]
    [SerializeField] private int ARTS = 1;
    [SerializeField] private int BURN = 2;
    [SerializeField] private int CORROSION = 3;
    [SerializeField] private int NECROSIS = 4;
    [SerializeField] private int NERVOUS = 5;

    [Header("속성 Sprite")]
    [SerializeField] private GameObject ARTS_Sprite;
    [SerializeField] private GameObject BURN_Sprite;
    [SerializeField] private GameObject CORROSION_Sprite;
    [SerializeField] private GameObject NECROSIS_Sprite;
    [SerializeField] private GameObject NERVOUS_Sprite;
    

    [Header("선택 TMP")]
    [SerializeField] private TMP_Text specialCardText;
    [SerializeField] private TMP_Text secondNumberText;
    [SerializeField] private TMP_Text thirdNumberText;

    [Header("공격 타입 버튼")]
    [SerializeField] private GameObject PHYS_Button;
    [SerializeField] private GameObject ARTS_Button;

    [Header("공격 타입 Sprite")]
    [SerializeField] private GameObject PHYS_Sprite;

    [Header("공격력 계산기")]
    [SerializeField] private AttackCalculator attackCalculator;
    [SerializeField] private GameObject calculateAttackButton;

    private bool isPhysicalMode = false;

    private Card specialCard;
    private Card attributeCard;
    private Card secondCard;
    private Card thirdCard;

    private int secondNumber;
    private int thirdNumber;

    private CardAttribute selectedAttribute = CardAttribute.None;

    public bool IsAccess { get; private set; }
    public bool IsPhysicalMode => isPhysicalMode;

    [Header("Access TMP")]
    [SerializeField] private TMP_Text accessText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DisableAllAttributeSprites();
        ClearTexts();
        SetArtsMode();
        if (calculateAttackButton != null)
            calculateAttackButton.SetActive(false);

    }

    public override void SelectCard(Card card)
    {
        if (card == null)
            return;

        if (card == attributeCard)
        {
            ClearAttributeSlot();
            return;
        }

        if (card == secondCard)
        {
            ClearSecondSlot();
            return;
        }

        if (card == thirdCard)
        {
            ClearThirdSlot();
            return;
        }

        if (attributeCard == null)
        {
            SetAttributeSlot(card);
            return;
        }

        if (card.IsSpecialCard)
            return;

        if (secondCard == null)
        {
            SetSecondSlot(card);
            return;
        }

        if (thirdCard == null)
        {
            SetThirdSlot(card);
            return;
        }
    }

    public void SetPhysicalMode()
    {
        isPhysicalMode = true;

        if (PHYS_Button != null)
            PHYS_Button.SetActive(false);

        if (ARTS_Button != null)
            ARTS_Button.SetActive(true);

        DisableAllAttributeSprites();

        if (specialCardText != null)
        {
            specialCardText.text = "";
            specialCardText.gameObject.SetActive(false);
        }

        if (PHYS_Sprite != null)
            PHYS_Sprite.SetActive(true);
    }

    public void SetArtsMode()
    {
        isPhysicalMode = false;

        if (ARTS_Button != null)
            ARTS_Button.SetActive(false);

        if (PHYS_Button != null)
            PHYS_Button.SetActive(true);

        if (PHYS_Sprite != null)
            PHYS_Sprite.SetActive(false);

        if (attributeCard != null && attributeCard.IsSpecialCard)
        {
            if (specialCardText != null)
            {
                specialCardText.gameObject.SetActive(true);
                specialCardText.text = attributeCard.SpecialCardText;
            }

            return;
        }

        ShowSelectedAttributeSprite();
    }

    private void ShowSelectedAttributeSprite()
    {
        DisableAllAttributeSprites();

        if (selectedAttribute == CardAttribute.ARTS && ARTS_Sprite != null)
            ARTS_Sprite.SetActive(true);
        else if (selectedAttribute == CardAttribute.BURN && BURN_Sprite != null)
            BURN_Sprite.SetActive(true);
        else if (selectedAttribute == CardAttribute.NERVOUS && NERVOUS_Sprite != null)
            NERVOUS_Sprite.SetActive(true);
        else if (selectedAttribute == CardAttribute.NECROSIS && NECROSIS_Sprite != null)
            NECROSIS_Sprite.SetActive(true);
        else if (selectedAttribute == CardAttribute.CORROSION && CORROSION_Sprite != null)
            CORROSION_Sprite.SetActive(true);
    }

    //private void SetSpecialSlot(Card card)
    //{
    //    specialCard = card;
    //    specialCard.SetSelected(true);

    //    if (specialCardText != null)
    //        specialCardText.text = card.SpecialCardText;
    //}

    private void SetAttributeSlot(Card card)
    {
        attributeCard = card;
        attributeCard.SetSelected(true, this);

        if (card.IsSpecialCard)
        {
            DisableAllAttributeSprites();

            if (specialCardText != null)
            {
                specialCardText.gameObject.SetActive(true);
                specialCardText.text = card.SpecialCardText;
            }
            UpdateCalculateAttackButton();
            return;
        }

        SelectAttribute(card.CardNumber);
        UpdateCalculateAttackButton();

        UpdateAccess();
    }

    private void SetSecondSlot(Card card)
    {
        secondCard = card;
        secondCard.SetSelected(true, this);

        secondNumber = card.CardNumber;

        if (secondNumberText != null)
            secondNumberText.text = secondNumber.ToString();
        UpdateCalculateAttackButton();

        UpdateAccess();
    }

    private void SetThirdSlot(Card card)
    {
        thirdCard = card;
        thirdCard.SetSelected(true, this);

        thirdNumber = card.CardNumber;

        if (thirdNumberText != null)
            thirdNumberText.text = thirdNumber.ToString();

        if (calculateAttackButton != null)
            calculateAttackButton.SetActive(true);
        UpdateCalculateAttackButton();

        UpdateAccess();
        // 나중에 Operator 구현 위치
        // CalculateWithOperator();
    }

    private void ClearSpecialSlot()
    {
        if (specialCard != null)
            specialCard.SetSelected(false, null);

        specialCard = null;

        if (specialCardText != null)
            specialCardText.text = "";

        UpdateAccess();
    }

    private void ClearAttributeSlot()
    {
        if (attributeCard != null)
            attributeCard.SetSelected(false, null);

        if (attributeCard != null && attributeCard.IsSpecialCard && specialCardText != null)
        {
            specialCardText.text = "";
            specialCardText.gameObject.SetActive(false);
        }

        if (PHYS_Sprite != null)
            PHYS_Sprite.SetActive(false);

        attributeCard = null;
        selectedAttribute = CardAttribute.None;

        DisableAllAttributeSprites();
        UpdateCalculateAttackButton();

        UpdateAccess();
    }

    private void ClearSecondSlot()
    {
        if (secondCard != null)
            secondCard.SetSelected(false, null);

        secondCard = null;
        secondNumber = 0;

        if (secondNumberText != null)
            secondNumberText.text = "";
        UpdateCalculateAttackButton();

        UpdateAccess();
    }

    private void ClearThirdSlot()
    {
        if (thirdCard != null)
            thirdCard.SetSelected(false, null);

        thirdCard = null;
        thirdNumber = 0;

        if (thirdNumberText != null)
            thirdNumberText.text = "";

        if (calculateAttackButton != null)
            calculateAttackButton.SetActive(false);
        UpdateCalculateAttackButton();

        UpdateAccess();
    }

    private void SelectAttribute(int number)
    {
        selectedAttribute = CardAttribute.None;

        if (number == ARTS || number == 5 + ARTS)
            selectedAttribute = CardAttribute.ARTS;
        else if (number == BURN || number == 5 + BURN)
            selectedAttribute = CardAttribute.BURN;
        else if (number == NERVOUS || number == 5 + NERVOUS)
            selectedAttribute = CardAttribute.NERVOUS;
        else if (number == NECROSIS || number == 5 + NECROSIS)
            selectedAttribute = CardAttribute.NECROSIS;
        else if (number == CORROSION || number == 5 + CORROSION)
            selectedAttribute = CardAttribute.CORROSION;

        if (isPhysicalMode)
        {
            DisableAllAttributeSprites();

            if (PHYS_Sprite != null)
                PHYS_Sprite.SetActive(true);
        }
        else
        {
            ShowSelectedAttributeSprite();
        }
    }

    public override void ResetSelection()
    {
        ClearSpecialSlot();
        ClearAttributeSlot();
        ClearSecondSlot();
        ClearThirdSlot();
        UpdateCalculateAttackButton();

        UpdateAccess();
    }

    private void DisableAllAttributeSprites()
    {
        if (ARTS_Sprite != null)
            ARTS_Sprite.SetActive(false);

        if (BURN_Sprite != null)
            BURN_Sprite.SetActive(false);

        if (NERVOUS_Sprite != null)
            NERVOUS_Sprite.SetActive(false);

        if (NECROSIS_Sprite != null)
            NECROSIS_Sprite.SetActive(false);

        if (CORROSION_Sprite != null)
            CORROSION_Sprite.SetActive(false);
    }

    private void ClearTexts()
    {
        if (specialCardText != null)
        {
            specialCardText.text = "";
            specialCardText.gameObject.SetActive(false);
        }

        if (secondNumberText != null)
            secondNumberText.text = "";

        if (thirdNumberText != null)
            thirdNumberText.text = "";
        UpdateCalculateAttackButton();
    }

    public int CalculateAttack()
    {
        if (attackCalculator == null)
            return 0;

        if (attributeCard == null ||
            secondCard == null ||
            thirdCard == null)
            return 0;

        int result = attackCalculator.Calculate(
            selectedAttribute,
            isPhysicalMode,
            attributeCard.IsSpecialCard,
            secondNumber,
            thirdNumber);

        return result;
    }

    private void UpdateCalculateAttackButton()
    {
        bool canCalculate =
            attributeCard != null &&
            secondCard != null &&
            thirdCard != null;

        if (calculateAttackButton != null)
            calculateAttackButton.SetActive(canCalculate);
    }

    private void UpdateAccess()
    {
        IsAccess =
            attributeCard != null &&
            secondCard != null &&
            thirdCard != null;

        if (accessText != null)
            accessText.text = IsAccess ? "승인됨" : "승인 대기중";

        BattleAccessManager.Instance.Refresh();
    }
}
