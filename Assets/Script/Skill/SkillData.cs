using System;
using Randint.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [SerializeField, GameDefinitionId("skill")] private string dataId;
    public string DataId => dataId;
    private SkillDefinition Definition => GameData.Catalog.Skill(dataId);
    public string SkillName => GameData.Text(Definition.nameKey);
    public string Description => GameData.Text(Definition.descriptionKey);
    public int MaxSP => Definition.maxSP;
    public SkillChargeType ChargeType => (SkillChargeType)Enum.Parse(typeof(SkillChargeType), Definition.chargeType);
    public int ChargeAmount => Definition.chargeAmount;
}
