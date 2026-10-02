using UnityEngine;
using Randint.Data;

public class OperatorSkill
{
    private readonly SkillDefinition data;

    private int currentSP;

    public SkillDefinition Data => data;

    public int CurrentSP => currentSP;
    public int MaxSP => data.maxSP;

    public bool IsReady =>
        currentSP >= data.maxSP;

    // 기본 정의는 JSON에서 조회하고 현재 SP는 이 전투 인스턴스에만 저장합니다.
    public OperatorSkill(string skillId)
    {
        data = GameData.Catalog.Skill(skillId);
        currentSP = 0;
    }

    public OperatorSkill(SkillData skillData) : this(skillData.DataId) { }

    public void AddSP(int amount)
    {
        if (IsReady)
            return;

        currentSP += amount;

        currentSP =
            Mathf.Min(currentSP, data.maxSP);
    }

    // 자연회복
    public void OnNaturalRecovery(int attackCount)
    {
        if (data.chargeType != "Natural")
            return;

        AddSP(
            attackCount * data.chargeAmount);
    }

    // 공격회복
    public void OnAttack()
    {
        if (data.chargeType != "Attack")
            return;

        AddSP(data.chargeAmount);
    }

    // 피격회복
    public void OnDamageTaken()
    {
        if (data.chargeType != "DamageTaken")
            return;

        AddSP(data.chargeAmount);
    }

    public void ResetSP()
    {
        currentSP = 0;
    }

    public void Activate(Operator owner)
    {
        if (!IsReady)
            return;

        Debug.Log(
            $"{owner.name} : {GameData.Text(data.nameKey)} 발동");

        // 실제 SkillEffect는 나중에 구현

        ResetSP();
    }
}
