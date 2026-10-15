using UnityEngine;

public class OperatorSkill
{
    private SkillData data;

    private int currentSP;

    public SkillData Data => data;

    public int CurrentSP => currentSP;
    public int MaxSP => data.MaxSP;

    public bool IsReady =>
        currentSP >= data.MaxSP;

    public OperatorSkill(SkillData skillData)
    {
        data = skillData;
        currentSP = 0;
    }

    public void AddSP(int amount)
    {
        if (IsReady || amount <= 0)
            return;

        currentSP = (int)System.Math.Min(data.MaxSP, (long)currentSP + amount);
    }

    // 자연회복
    public void OnNaturalRecovery(int attackCount)
    {
        if (data.ChargeType != SkillChargeType.Natural)
            return;

        if (attackCount > 0 && data.ChargeAmount > 0)
            AddSP((int)System.Math.Min(int.MaxValue, (long)attackCount * data.ChargeAmount));
    }

    // 공격회복
    public void OnAttack()
    {
        if (data.ChargeType != SkillChargeType.Attack)
            return;

        AddSP(data.ChargeAmount);
    }

    // 피격회복
    public void OnDamageTaken()
    {
        if (data.ChargeType != SkillChargeType.DamageTaken)
            return;

        AddSP(data.ChargeAmount);
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
            $"{owner.name} : {data.SkillName} 발동");

        // 실제 SkillEffect는 나중에 구현

        ResetSP();
    }
}
