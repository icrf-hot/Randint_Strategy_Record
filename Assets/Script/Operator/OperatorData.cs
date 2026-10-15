using System;
using Randint.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "NewOperatorData", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject
{
    [Header("JSON Definition")]
    [SerializeField, GameDefinitionId("operator")] private string dataId;

    // 기존 프리팹은 이 Asset의 ID를 유지하며, 실제 정의는 JSON에서 조회합니다.
    public string DataId => dataId;
    private OperatorDefinition Definition => GameData.Catalog.Operator(dataId);
    public string OperatorName => GameData.Text(Definition.nameKey);
    public string Info => GameData.Text(Definition.infoKey);
    public OperatorPosition Position => (OperatorPosition)Enum.Parse(typeof(OperatorPosition), Definition.position);
    public OperatorClass DetailedClass => (OperatorClass)Enum.Parse(typeof(OperatorClass), Definition.classId);
    public bool RequiresEnemyTarget => Definition.requiresEnemyTarget;
    public int MaxHP => Definition.maxHP;
    public int Attack => Definition.attack;
    public int Defense => Definition.defense;
    public int ArtsResistance => Definition.artsResistance;
    public float AttackSpeed => Definition.attackSpeed;
    public string[] SkillIds => Definition.skillIds;
}
