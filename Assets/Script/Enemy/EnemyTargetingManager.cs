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

        ClearTargets();

        // 타깃이 필요 없는 캐릭터는 적 선택 UI를 열지 않는다.
        if (!op.RequiresEnemyTarget)
        {
            selectedTargets.Remove(op);

            if (BattleAccessManager.Instance != null)
            {
                BattleAccessManager.Instance.Refresh();
            }

            return;
        }

        FindTargets();
        RestoreSelectedTarget();

        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance.Refresh();
        }
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

    public void SelectTarget(EnemyTargetable target)
    {
        if (currentOperator == null)
            return;

        if (!currentOperator.RequiresEnemyTarget)
            return;

        if (target == null)
            return;

        if (!availableTargets.Contains(target))
            return;

        EnemyTargetable previousTarget =
            SelectedTarget;

        if (previousTarget != null &&
            previousTarget != target)
        {
            previousTarget.ShowClickable();
        }

        // 같은 적을 다시 클릭하면 선택 해제
        if (previousTarget == target)
        {
            selectedTargets.Remove(
                currentOperator);

            target.ShowClickable();

            Debug.Log(
                $"[타깃 해제] {currentOperator.gameObject.name}");

            if (BattleAccessManager.Instance != null)
            {
                BattleAccessManager.Instance.Refresh();
            }

            return;
        }

        selectedTargets[currentOperator] =
            target;

        target.ShowSelected();

        Debug.Log(
            $"[타깃 선택] {currentOperator.gameObject.name} → " +
            $"{target.gameObject.name}");

        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance.Refresh();
        }
    }


    // =========================================================
    // 타겟 선택일껄?
    // =========================================================

    public EnemyTargetable GetSelectedTarget(Operator op)
    {
        if (op == null)
            return null;

        if (!selectedTargets.TryGetValue(
            op,
            out EnemyTargetable target))
        {
            return null;
        }

        if (target == null)
        {
            selectedTargets.Remove(op);
            return null;
        }

        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null || enemy.CurrentHP <= 0)
        {
            selectedTargets.Remove(op);
            return null;
        }

        return target;
    }


    public bool IsTargetRequirementMet(Operator op)
    {
        if (op == null)
            return false;

        // 적 타깃이 필요 없는 캐릭터는 자동 통과
        if (!op.RequiresEnemyTarget)
            return true;

        return GetSelectedTarget(op) != null;
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

        if (!op.RequiresEnemyTarget)
            return false;

        switch (op.Position)
        {
            case OperatorPosition.Front:
                return
                    enemy.Position == EnemyPosition.FrontLeft ||
                    enemy.Position == EnemyPosition.FrontRight;

            case OperatorPosition.Middle:
            case OperatorPosition.Back:
                return true;
        }

        return false;
    }
}