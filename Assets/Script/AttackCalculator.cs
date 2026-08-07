using UnityEngine;

public class AttackCalculator : MonoBehaviour
{
    [Header("공식 매핑")]
    [SerializeField] private AttackFormulaSetting physicalFormula;
    [SerializeField] private AttackFormulaSetting artsFormula;
    [SerializeField] private AttackFormulaSetting burnFormula;
    [SerializeField] private AttackFormulaSetting nervousFormula;
    [SerializeField] private AttackFormulaSetting necrosisFormula;
    [SerializeField] private AttackFormulaSetting corrosionFormula;
    [SerializeField] private AttackFormulaSetting specialFormula;

    public int Calculate(
        CardAttribute attribute,
        bool isPhysicalMode,
        bool isSpecialCard,
        int secondNumber,
        int thirdNumber)
    {
        AttackFormulaType formulaType = GetFormulaType(
            attribute,
            isPhysicalMode,
            isSpecialCard);

        switch (formulaType)
        {
            case AttackFormulaType.Formula1:
                return Formula1(secondNumber, thirdNumber);

            case AttackFormulaType.Formula2:
                return Formula2(secondNumber, thirdNumber);

            case AttackFormulaType.Formula3:
                return Formula3(secondNumber, thirdNumber);

            case AttackFormulaType.Formula4:
                return Formula4(secondNumber, thirdNumber);

            case AttackFormulaType.Formula5:
                return Formula5(secondNumber, thirdNumber);

            case AttackFormulaType.Formula6:
                return Formula6(secondNumber, thirdNumber);

            case AttackFormulaType.Formula7:
                return Formula7(secondNumber, thirdNumber);

            default:
                return 0;
        }
    }

    private AttackFormulaType GetFormulaType(
        CardAttribute attribute,
        bool isPhysicalMode,
        bool isSpecialCard)
    {
        if (isSpecialCard)
            return specialFormula.formulaType;

        if (isPhysicalMode)
            return physicalFormula.formulaType;

        switch (attribute)
        {
            case CardAttribute.ARTS:
                return artsFormula.formulaType;

            case CardAttribute.BURN:
                return burnFormula.formulaType;

            case CardAttribute.NERVOUS:
                return nervousFormula.formulaType;

            case CardAttribute.NECROSIS:
                return necrosisFormula.formulaType;

            case CardAttribute.CORROSION:
                return corrosionFormula.formulaType;

            default:
                return artsFormula.formulaType;
        }
    }

    private int Formula1(int secondNumber, int thirdNumber) //물리 피해
    {
        int result = (secondNumber + thirdNumber) * 100; //수치 2개의 합에 가산치(여기서는 100)을 곱해 반환함
        return result;
    }

    private int Formula2(int secondNumber, int thirdNumber) //일반 아츠 피해
    {
        int result = (secondNumber + thirdNumber) * 80; //수치 2개의 합에 가산치(여기서는 80)을 곱해 반환함
        return result;
    }

    private int Formula3(int secondNumber, int thirdNumber) //소각 아츠 피해
    {
        int result = (secondNumber + thirdNumber) * 80; //수치 2개의 합에 가산치(여기서는 80)을 곱해 반환함
        return result;
    }

    private int Formula4(int secondNumber, int thirdNumber) //신경 아츠 피해
    {
        int result = (secondNumber + thirdNumber) * 80; //수치 2개의 합에 가산치(여기서는 80)을 곱해 반환함
        return result;
    }

    private int Formula5(int secondNumber, int thirdNumber) //쇠약 아츠 피해
    {
        int result = (secondNumber + thirdNumber) * 80; //수치 2개의 합에 가산치(여기서는 80)을 곱해 반환함
        return result;
    }

    private int Formula6(int secondNumber, int thirdNumber) //침식 아츠 피해
    {
        int result = (secondNumber + thirdNumber) * 80; //수치 2개의 합에 가산치(여기서는 80)을 곱해 반환함
        return result;
    }

    private int Formula7(int secondNumber, int thirdNumber) //트루 피해
    {
        int result = (secondNumber + thirdNumber) * 80; //수치 2개의 합에 가산치(여기서는 80)을 곱해 반환함
        return result;
    }
}