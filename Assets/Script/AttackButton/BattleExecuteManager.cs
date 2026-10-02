using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BattleExecuteManager : MonoBehaviour
{
    public static BattleExecuteManager Instance { get; private set; }

    //[Header("Operators")]
    private Operator frontOperator;
    private Operator middleOperator;
    private Operator backOperator;

    [Header("Battle Timing")]
    [SerializeField] private float focusBeforeActionDelay = 0.6f;
    [SerializeField] private float actionInterval = 0.5f;

    [Header("Back Action")]
    [SerializeField] private int backHealAmount = 50;

    [Header("Battle Result")]
    [SerializeField]
    private BattleToMapSceneLoader mapSceneLoader;
    [SerializeField]
    private BattleResultPresenter resultPresenter;

    private bool isFinishingBattle;

    private bool isReturningToMap;

    private Coroutine battleRoutine;

    private sealed class TurnAction
    {
        public string Name;
        public float Speed;
        public int TiePriority;
        public Operator Operator;
        public Enemy Enemy;
        public Action Execute;

        public bool CanAct
        {
            get
            {
                if (Operator != null)
                    return Operator.IsCombatActive;

                if (Enemy != null)
                    return Enemy.CurrentHP > 0;

                return false;
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ExecuteBattle()
    {
        if (battleRoutine != null)
        {
            Debug.Log("[전투] 이미 전투가 실행 중입니다.");
            return;
        }

        if (!LoadOperators())
        {
            Debug.LogError(
                "[전투] CharacterSet에서 모든 Operator를 " +
                "불러오지 못했습니다.");

            return;
        }

        if (BattleAccessManager.Instance != null &&
            !BattleAccessManager.Instance.CanBattle)
        {
            Debug.LogWarning("[전투] 카드 선택이 완료되지 않았습니다.");
            return;
        }


        battleRoutine = StartCoroutine(ExecuteBattleRoutine());
    }

    private IEnumerator ExecuteBattleRoutine()
    {
        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance.SetBattleRunning(true);
        }

        // 기다리지 않고 카드 연출 코루틴만 시작함
        // 이후 전투 순서 계산과 행동 처리가 바로 이어짐
        if (CardSpawner.Instance != null)
        {
            CardSpawner.Instance.BeginDiscardAnimation();
        }
        else
        {
            Debug.LogWarning(
                "[카드 회수 실패] CardSpawner를 찾을 수 없습니다.");
        }

        List<TurnAction> turnOrder = CreateTurnOrder();

        turnOrder.Sort(CompareTurnAction);

        Debug.Log("========== 전투 순서 ==========");

        for (int i = 0; i < turnOrder.Count; i++)
        {
            TurnAction action = turnOrder[i];

            Debug.Log(
                $"[전투 순서] {i + 1}. {action.Name} " +
                $"| 공격속도: {action.Speed:0.##}");
        }

        Debug.Log("==============================");

        for (int i = 0; i < turnOrder.Count; i++)
        {
            TurnAction action = turnOrder[i];

            if (!action.CanAct)
            {
                Debug.Log(
                    $"[행동 취소] {action.Name}은 행동할 수 없습니다.");

                continue;
            }

            // 오퍼레이터 행동 전 자동 Focus
            if (action.Operator != null)
            {
                if (OperatorFocusManager.Instance != null)
                {
                    Debug.Log(
                        $"[행동 Focus] {action.Name}에게 Focus를 이동합니다.");

                    OperatorFocusManager.Instance.EnterBattleFocus(
                        action.Operator);

                    if (focusBeforeActionDelay > 0f)
                    {
                        yield return new WaitForSecondsRealtime(
                            focusBeforeActionDelay);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "[행동 Focus 실패] " +
                        "OperatorFocusManager가 없습니다.");
                }
            }

            Debug.Log(
                $"[행동 시작] {i + 1}. {action.Name} " +
                $"| 공격속도: {action.Speed:0.##}");

            action.Execute?.Invoke();

            Debug.Log($"[행동 종료] {action.Name}");


            // Destroy 예약과 체력 변경이 반영될 때까지 한 프레임 기다립니다.
            yield return null;

            if (AreAllEnemiesDefeated())
            {
                yield return FinishBattle(
                    BattleResultType.Success
                );

                yield break;
            }

            if (actionInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    actionInterval);
            }
            else
            {
                yield return null;
            }
        }

        if (OperatorFocusManager.Instance != null)
        {
            OperatorFocusManager.Instance.ExitFocus();
        }

        // 예약된 재배치를 먼저 처리한 다음 패배 여부를 판정합니다.
        ProcessOperatorRedeployments();

        bool isDefeated =
            AreAllOperatorsRetreated();

        // 다음 라운드 또는 씬 전환 전에 임시 보정을 한 번만 제거합니다.

        if (isDefeated)
        {
            yield return FinishBattle(
                BattleResultType.Failure
            );

            yield break;
        }

        // 패배하지 않았을 때만 카드 회수 완료를 기다립니다.
        if (CardSpawner.Instance != null)
        {
            yield return CardSpawner.Instance
                .WaitForDiscardAnimation();
        }

        ResetCardSelections();

        if (CardSpawner.Instance != null)
        {
            CardSpawner.Instance.ReplaceCards();
        }
        else
        {
            Debug.LogWarning(
                "[카드 배분 실패] CardSpawner를 찾을 수 없습니다.");
        }

        Debug.Log("========== 전투 종료 ==========");

        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance
                .SetBattleRunning(false);
        }

        battleRoutine = null;
    }

    private IEnumerator FinishBattle(
    BattleResultType resultType)
    {
        if (isFinishingBattle)
            yield break;

        isFinishingBattle = true;

        if (OperatorFocusManager.Instance != null)
        {
            OperatorFocusManager.Instance.ExitFocus();
        }

        ResetOperatorBonuses();

        switch (resultType)
        {
            case BattleResultType.Success:
                Debug.Log("[Battle Result] Victory");
                break;

            case BattleResultType.Failure:
                Debug.Log("[Battle Result] Defeat");
                break;
        }

        if (resultPresenter == null)
        {
            resultPresenter =
                FindFirstObjectByType<BattleResultPresenter>();
        }

        if (resultPresenter != null)
        {
            yield return resultPresenter
                .ShowResult(resultType);
        }
        else
        {
            Debug.LogWarning(
                "[Battle Result] BattleResultPresenter를 " +
                "찾을 수 없어 결과 화면을 생략합니다.",
                this
            );
        }

        ReturnToMap();
    }

    private List<TurnAction> CreateTurnOrder()
    {
        List<TurnAction> result = new List<TurnAction>();

        AddOperatorAction(
            result,
            frontOperator,
            "Front",
            0,
            ExecuteFrontAction);

        AddOperatorAction(
            result,
            middleOperator,
            "Middle",
            1,
            ExecuteMiddleAction);

        AddOperatorAction(
            result,
            backOperator,
            "Back",
            2,
            ExecuteBackAction);

        Enemy[] enemies = FindObjectsByType<Enemy>(
            FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.CurrentHP <= 0)
                continue;

            Enemy capturedEnemy = enemy;

            EnemyTargetable targetable =
                enemy.GetComponent<EnemyTargetable>();

            int enemyPriority = 100;

            if (targetable != null)
            {
                enemyPriority += (int)targetable.Position;
            }

            result.Add(new TurnAction
            {
                Name = $"Enemy - {enemy.gameObject.name}",
                Speed = enemy.AttackSpeed,
                TiePriority = enemyPriority,
                Enemy = enemy,
                Execute = () => ExecuteEnemyAction(capturedEnemy)
            });
        }

        return result;
    }

    private void ProcessOperatorRedeployments()
    {
        Operator[] operators =
            FindObjectsByType<Operator>(
                FindObjectsSortMode.None
            );

        foreach (Operator op in operators)
        {
            if (op == null)
                continue;

            op.ProcessRedeploymentTurn();
        }
    }

    private bool AreAllOperatorsRetreated()
    {
        Operator[] operators =
            FindObjectsByType<Operator>(
                FindObjectsSortMode.None
            );

        if (operators.Length == 0)
            return true;

        foreach (Operator op in operators)
        {
            if (op != null &&
                op.IsCombatActive)
            {
                return false;
            }
        }

        return true;
    }

    private void AddOperatorAction(
        List<TurnAction> list,
        Operator op,
        string positionName,
        int tiePriority,
        Action execute)
    {
        if (op == null || !op.IsCombatActive)
            return;

        string operatorName = op.gameObject.name;

        if (op.Data != null &&
            !string.IsNullOrWhiteSpace(op.Data.OperatorName))
        {
            operatorName = op.Data.OperatorName;
        }

        list.Add(new TurnAction
        {
            Name = $"{positionName} - {operatorName}",
            Speed = op.AttackSpeed,
            TiePriority = tiePriority,
            Operator = op,
            Execute = execute
        });
    }

    private int CompareTurnAction(
        TurnAction a,
        TurnAction b)
    {
        int speedResult =
            b.Speed.CompareTo(a.Speed);

        if (speedResult != 0)
            return speedResult;

        return a.TiePriority.CompareTo(b.TiePriority);
    }

    private void ExecuteFrontAction()
    {
        FrontCardSelectionManager front =
            FrontCardSelectionManager.Instance;

        if (front == null || frontOperator == null)
            return;

        frontOperator.ApplyBattleBonus(
            front.AttackBonus,
            front.DefenseBonus,
            front.ArtsResistanceBonus);

        if (front.ActionType == FrontActionType.Defense)
        {
            Debug.Log(
                $"[Front 방어] {frontOperator.gameObject.name} " +
                $"| 방어 +{front.DefenseBonus} " +
                $"| 마법 저항 +{front.ArtsResistanceBonus}");

            return;
        }

        Enemy target = GetSelectedEnemy(frontOperator);

        if (target == null)
        {
            Debug.LogWarning(
                "[Front 공격 취소] 선택된 적이 없습니다.");

            return;
        }

        Debug.Log(
            $"[Front 공격] {frontOperator.gameObject.name} → " +
            $"{target.gameObject.name} " +
            $"| 물리 공격력: {frontOperator.Attack}");

        target.TakePhysicalDamage(frontOperator.Attack);
    }

    private void ExecuteMiddleAction()
    {
        MiddleCardSelectionManager middle =
            MiddleCardSelectionManager.Instance;

        if (middle == null || middleOperator == null)
            return;

        int attackBonus = middle.CalculateAttack();

        middleOperator.ApplyBattleBonus(
            attackBonus,
            0,
            0);

        Enemy target = GetSelectedEnemy(middleOperator);

        if (target == null)
        {
            Debug.LogWarning(
                "[Middle 공격 취소] 선택된 적이 없습니다.");

            return;
        }

        string damageType =
            middle.IsPhysicalMode ? "물리" : "아츠";

        Debug.Log(
            $"[Middle 공격] {middleOperator.gameObject.name} → " +
            $"{target.gameObject.name} " +
            $"| {damageType} 공격력: {middleOperator.Attack}");

        if (middle.IsPhysicalMode)
        {
            target.TakePhysicalDamage(middleOperator.Attack);
        }
        else
        {
            target.TakeArtsDamage(middleOperator.Attack);
        }
    }

    private void ExecuteBackAction()
    {
        if (backOperator == null)
            return;

        if (AllyTargetingManager.Instance == null)
        {
            Debug.LogWarning(
                "[Back 회복 취소] AllyTargetingManager가 없습니다.");

            return;
        }

        List<Operator> targets =
            AllyTargetingManager.Instance.GetHealTargets(
                backOperator);

        if (targets.Count == 0)
        {
            Debug.Log("[Back 회복] 회복할 대상이 없습니다.");
            return;
        }

        foreach (Operator target in targets)
        {
            if (target == null || target.CurrentHP <= 0)
                continue;

            Debug.Log(
                $"[Back 회복] {backOperator.gameObject.name} → " +
                $"{target.gameObject.name} " +
                $"| 회복량: {backHealAmount}");

            target.Heal(backHealAmount);
        }
    }

    private void ExecuteEnemyAction(Enemy enemy)
    {
        if (enemy == null || enemy.CurrentHP <= 0)
            return;

        Operator target = GetEnemyTarget();

        if (target == null)
        {
            Debug.LogWarning(
                $"[Enemy 공격 취소] {enemy.gameObject.name}의 " +
                "공격 대상이 없습니다.");

            return;
        }

        Debug.Log(
            $"[Enemy 공격] {enemy.gameObject.name} → " +
            $"{target.gameObject.name} " +
            $"| 물리 공격력: {enemy.Attack}");

        target.TakePhysicalDamage(enemy.Attack);
    }

    private Enemy GetSelectedEnemy(Operator op)
    {
        if (EnemyTargetingManager.Instance == null)
            return null;

        EnemyTargetable targetable =
            EnemyTargetingManager.Instance
                .GetSelectedTarget(op);

        if (targetable == null)
            return null;

        Enemy enemy =
            targetable.GetComponent<Enemy>();

        if (enemy == null || enemy.CurrentHP <= 0)
            return null;

        return enemy;
    }

    private Operator GetEnemyTarget()
    {
        if (frontOperator != null &&
            frontOperator.IsCombatActive)
        {
            return frontOperator;
        }

        if (middleOperator != null &&
            middleOperator.IsCombatActive)
        {
            return middleOperator;
        }

        if (backOperator != null &&
            backOperator.IsCombatActive)
        {
            return backOperator;
        }

        return null;
    }

    private void ResetOperatorBonuses()
    {
        if (frontOperator != null)
            frontOperator.ResetBattleBonus();

        if (middleOperator != null)
            middleOperator.ResetBattleBonus();

        if (backOperator != null)
            backOperator.ResetBattleBonus();
    }

    private void ResetCardSelections()
    {
        if (FrontCardSelectionManager.Instance != null)
        {
            FrontCardSelectionManager.Instance.ResetSelection();
        }

        if (MiddleCardSelectionManager.Instance != null)
        {
            MiddleCardSelectionManager.Instance.ResetSelection();
        }

        BackCardSelectionManager backManager =
            FindFirstObjectByType<BackCardSelectionManager>();

        if (backManager != null)
        {
            backManager.ResetSelection();
        }

        Debug.Log("[카드] 모든 카드 선택을 초기화했습니다.");
    }

    private bool LoadOperators()
    {
        BattleAccessManager accessManager =
            BattleAccessManager.Instance;

        if (accessManager == null)
        {
            accessManager =
                FindFirstObjectByType<BattleAccessManager>();
        }

        if (accessManager == null)
            return false;

        frontOperator =
            accessManager.FrontOperator;

        middleOperator =
            accessManager.MiddleOperator;

        backOperator =
            accessManager.BackOperator;

        // 아직 검색되지 않았다면 다시 검색
        if (frontOperator == null ||
            middleOperator == null ||
            backOperator == null)
        {
            accessManager.ReloadOperatorsFromParent();

            frontOperator =
                accessManager.FrontOperator;

            middleOperator =
                accessManager.MiddleOperator;

            backOperator =
                accessManager.BackOperator;
        }

        return
            frontOperator != null &&
            middleOperator != null &&
            backOperator != null;
    }

    private bool AreAllEnemiesDefeated()
    {
        Enemy[] enemies =
            FindObjectsByType<Enemy>(
                FindObjectsSortMode.None
            );

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null &&
                enemy.CurrentHP > 0)
            {
                return false;
            }
        }

        return true;
    }

    private void ReturnToMap()
    {
        if (isReturningToMap)
            return;

        if (mapSceneLoader == null)
        {
            mapSceneLoader =
                FindFirstObjectByType<BattleToMapSceneLoader>();
        }

        if (mapSceneLoader == null)
        {
            Debug.LogError(
                "[전투 종료] BattleToMapSceneLoader를 " +
                "찾을 수 없습니다.",
                this
            );

            return;
        }

        isReturningToMap = true;

        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance
                .SetBattleRunning(false);
        }

        if (OperatorFocusManager.Instance != null)
        {
            OperatorFocusManager.Instance.ExitFocus();
        }

        mapSceneLoader.LoadMap();
    }
}