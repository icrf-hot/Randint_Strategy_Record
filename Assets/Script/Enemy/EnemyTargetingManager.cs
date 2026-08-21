using System.Collections.Generic;
using UnityEngine;

public class EnemyTargetingManager : MonoBehaviour
{
    public static EnemyTargetingManager Instance { get; private set; }

    private Operator currentOperator;

    private readonly List<EnemyTargetable>
        availableTargets =
        new List<EnemyTargetable>();

    // 오퍼레이터별 선택된 타겟
    private readonly Dictionary<
        Operator,
        EnemyTargetable>
        selectedTargets =
        new Dictionary<
            Operator,
            EnemyTargetable>();

    public EnemyTargetable SelectedTarget
    {
        get
        {
            if (currentOperator == null)
                return null;

            if (selectedTargets.TryGetValue(
                currentOperator,
                out EnemyTargetable target))
            {
                return target;
            }

            return null;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    // =========================================================
    // 타겟팅 시작
    // =========================================================

    public void BeginTargeting(Operator op)
    {
        if (op == null)
            return;

        currentOperator = op;

        FindTargets();

        // 기존에 선택했던 타겟 복구
        RestoreSelectedTarget();
    }

    // =========================================================
    // 타겟팅 종료
    // =========================================================

    public void EndTargeting()
    {
        currentOperator = null;

        ClearTargets();
    }

    // =========================================================
    // 타겟 검색
    // =========================================================

    private void FindTargets()
    {
        ClearTargets();

        EnemyTargetable[] enemies =
            FindObjectsByType<EnemyTargetable>(
                FindObjectsSortMode.None);

        foreach (EnemyTargetable enemy in enemies)
        {
            if (enemy == null)
                continue;

            // 타겟 자체가 불가능한 적
            if (!enemy.CanBeTargeted)
            {
                enemy.ShowUntargetable();
                continue;
            }

            // 현재 오퍼레이터가 공격할 수 없는 적
            if (!CanTargetEnemy(
                currentOperator,
                enemy))
            {
                enemy.ShowUntargetable();
                continue;
            }

            // 현재 오퍼레이터가 공격할 수 있는 적
            availableTargets.Add(enemy);

            enemy.ShowClickable();
        }
    }

    // =========================================================
    // 타겟 선택
    // =========================================================

    public void SelectTarget(
        EnemyTargetable target)
    {
        if (currentOperator == null)
            return;

        if (target == null)
            return;

        if (!availableTargets.Contains(target))
            return;

        // 기존 선택 해제
        EnemyTargetable previousTarget =
            SelectedTarget;

        if (previousTarget != null &&
            previousTarget != target)
        {
            previousTarget.ShowClickable();
        }

        // 같은 타겟을 다시 클릭하면 선택 취소
        if (previousTarget == target)
        {
            selectedTargets.Remove(
                currentOperator);

            target.ShowClickable();

            return;
        }

        // 새로운 타겟 저장
        selectedTargets[currentOperator] =
            target;

        target.ShowSelected();

        Debug.Log(
            $"{currentOperator.name} → " +
            $"{target.name}");
    }

    // =========================================================
    // 기존 타겟 복구
    // =========================================================

    private void RestoreSelectedTarget()
    {
        EnemyTargetable target =
            SelectedTarget;

        if (target == null)
            return;

        // 적이 이미 죽었거나
        // 더 이상 타겟 불가능한 경우
        if (!availableTargets.Contains(target))
        {
            selectedTargets.Remove(
                currentOperator);

            return;
        }

        target.ShowSelected();
    }

    // =========================================================
    // 타겟 전체 제거
    // =========================================================

    private void ClearTargets()
    {
        EnemyTargetable[] enemies =
            FindObjectsByType<EnemyTargetable>(
                FindObjectsSortMode.None);

        foreach (EnemyTargetable enemy in enemies)
        {
            if (enemy != null)
                enemy.HideAllIndicators();
        }

        availableTargets.Clear();
    }

    // =========================================================
    // 적 타겟 가능 여부
    // =========================================================

    private bool CanTargetEnemy(
        Operator op,
        EnemyTargetable enemy)
    {
        if (op == null || enemy == null)
            return false;

        switch (op.Class)
        {
            case OperatorClass.Front:

                return
                    enemy.Position ==
                        EnemyPosition.FrontLeft ||

                    enemy.Position ==
                        EnemyPosition.FrontRight;

            case OperatorClass.Middle:
                return true;

            case OperatorClass.AttackHealer:
                return true;

            case OperatorClass.Healer:
                return false;

            case OperatorClass.Supporter:
                return true;
        }

        return false;
    }
}