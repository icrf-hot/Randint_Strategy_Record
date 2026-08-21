using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSkillData",
    menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string skillName;

    [TextArea(3, 10)]
    [SerializeField] private string description;

    [Header("SP")]
    [SerializeField] private int maxSP = 10;

    [SerializeField] private SkillChargeType chargeType;

    [SerializeField] private int chargeAmount = 1;

    public string SkillName => skillName;
    public string Description => description;

    public int MaxSP => maxSP;
    public SkillChargeType ChargeType => chargeType;
    public int ChargeAmount => chargeAmount;
}