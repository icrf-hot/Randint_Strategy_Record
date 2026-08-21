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

    [Header("기본 스탯")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int attack = 20;
    [SerializeField] private int defense = 10;
    [SerializeField] private int artsResistance = 15;
    [SerializeField] private float attackSpeed = 1.0f;

    [Header("스킬")]
    [SerializeField] private SkillData[] skills;

    public SkillData[] Skills => skills;

    public string OperatorName => operatorName;
    public string Info => info;

    public int MaxHP => maxHP;
    public int Attack => attack;
    public int Defense => defense;
    public int ArtsResistance => artsResistance;
    public float AttackSpeed => attackSpeed;
}