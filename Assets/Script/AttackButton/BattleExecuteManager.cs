using System.Collections.Generic;
using UnityEngine;

public class BattleExecuteManager : MonoBehaviour
{
    public static BattleExecuteManager Instance { get; private set; }

    [Header("Operators")]
    [SerializeField] private Operator frontOperator;
    [SerializeField] private Operator middleOperator;
    [SerializeField] private Operator backOperator;

    private void Awake()
    {
        Instance = this;
    }

    public void ExecuteBattle()
    {
        CalculateAllActions();
    }

    private void CalculateAllActions()
    {
        // ==========================================
        // Front
        // ==========================================

        FrontCardSelectionManager front =
            FrontCardSelectionManager.Instance;

        int frontAttack = 0;
        int frontDefense = 0;
        int frontArtsResistance = 0;

        if (front != null)
        {
            frontAttack = front.AttackBonus;
            frontDefense = front.DefenseBonus;
            frontArtsResistance =
                front.ArtsResistanceBonus;
        }

        // ==========================================
        // Middle
        // ==========================================

        MiddleCardSelectionManager middle =
            MiddleCardSelectionManager.Instance;

        int middleAttack = 0;

        if (middle != null)
        {
            middleAttack =
                middle.CalculateAttack();
        }

        // ==========================================
        // 결과 출력
        // ==========================================

        Debug.Log(
            $"[전투 시행]\n" +
            $"Front AT : {frontAttack}\n" +
            $"Front DF : {frontDefense}\n" +
            $"Front RES : {frontArtsResistance}\n" +
            $"Middle AT : {middleAttack}"
        );

        // ==========================================
        // Operator 스탯 적용
        // ==========================================

        ApplyOperatorStats(
            frontAttack,
            frontDefense,
            frontArtsResistance,
            middleAttack);

        // ==========================================
        // Back 힐 실행
        // ==========================================

        ExecuteHealing();
    }

    private void ExecuteHealing()
    {
        if (backOperator == null)
            return;

        if (AllyTargetingManager.Instance == null)
            return;

        List<Operator> healTargets =
            AllyTargetingManager.Instance.GetHealTargets(
                backOperator);

        if (healTargets.Count == 0)
        {
            Debug.Log(
                $"[힐] {backOperator.name} : 힐 대상 없음");

            return;
        }

        // 현재는 테스트용 고정 힐량
        int healAmount = 50;

        Debug.Log(
            $"[힐 실행] {backOperator.name} " +
            $"→ {healTargets.Count}명 " +
            $"힐량 : {healAmount}");

        foreach (Operator target in healTargets)
        {
            if (target == null)
                continue;

            target.Heal(healAmount);
        }
    }

    private void ApplyOperatorStats(
        int frontAttack,
        int frontDefense,
        int frontArtsResistance,
        int middleAttack)
    {
        // ==========================================
        // Front
        // ==========================================

        if (frontOperator != null)
        {
            frontOperator.ApplyBattleBonus(
                frontAttack,
                frontDefense,
                frontArtsResistance);
        }

        // ==========================================
        // Middle
        // ==========================================

        if (middleOperator != null)
        {
            middleOperator.ApplyBattleBonus(
                middleAttack,
                0,
                0);
        }
    }
}