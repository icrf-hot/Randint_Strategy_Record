using UnityEngine;

[CreateAssetMenu(
    fileName = "NewOperatorData",
    menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string operatorName;

    [TextArea]
    [SerializeField] private string info;

    [Header("배치 위치")]
    [SerializeField]
    private OperatorPosition position =
        OperatorPosition.Front;

    [Header("세부 직군")]
    [SerializeField]
    private OperatorClass detailedClass =
        OperatorClass.Defender;

    [Header("타겟팅")]
    [Tooltip("전투 시작 전에 적 목표를 선택해야 하는지 결정합니다.")]
    [SerializeField]
    private bool requiresEnemyTarget;

    [Header("기본 능력치")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int attack = 20;
    [SerializeField] private int defense = 10;
    [SerializeField] private int artsResistance = 15;
    [SerializeField] private float attackSpeed = 1f;

    [Header("스킬")]
    [SerializeField] private SkillData[] skills;

    public string OperatorName => operatorName;
    public string Info => info;

    public OperatorPosition Position => position;
    public OperatorClass DetailedClass => detailedClass;
    public bool RequiresEnemyTarget => requiresEnemyTarget;

    public int MaxHP => maxHP;
    public int Attack => attack;
    public int Defense => defense;
    public int ArtsResistance => artsResistance;
    public float AttackSpeed => attackSpeed;

    public SkillData[] Skills => skills;

    private void OnValidate()
    {
        if (IsClassAllowed(position, detailedClass))
            return;

        detailedClass =
            GetDefaultClass(position);

        Debug.LogWarning(
            $"{name}: {position} 위치에서 사용할 수 없는 " +
            $"세부 직군이므로 {detailedClass}(으)로 변경했습니다.",
            this);
    }

    private bool IsClassAllowed(
        OperatorPosition targetPosition,
        OperatorClass targetClass)
    {
        switch (targetPosition)
        {
            case OperatorPosition.Front:
                return
                    targetClass == OperatorClass.Defender ||
                    targetClass == OperatorClass.Guard ||
                    targetClass == OperatorClass.Vanguard;

            case OperatorPosition.Middle:
                return
                    targetClass == OperatorClass.Caster ||
                    targetClass == OperatorClass.Sniper;

            case OperatorPosition.Back:
                return
                    targetClass == OperatorClass.AttackHealer ||
                    targetClass == OperatorClass.Healer ||
                    targetClass == OperatorClass.Supporter;
        }

        return false;
    }

    private OperatorClass GetDefaultClass(
        OperatorPosition targetPosition)
    {
        switch (targetPosition)
        {
            case OperatorPosition.Front:
                return OperatorClass.Defender;

            case OperatorPosition.Middle:
                return OperatorClass.Caster;

            case OperatorPosition.Back:
                return OperatorClass.Healer;
        }

        return OperatorClass.Defender;
    }
}