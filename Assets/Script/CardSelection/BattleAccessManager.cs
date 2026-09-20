using UnityEngine;

public class BattleAccessManager : MonoBehaviour
{
    public static BattleAccessManager Instance { get; private set; }

    [Header("Selection Managers")]
    [SerializeField] private FrontCardSelectionManager frontManager;
    [SerializeField] private MiddleCardSelectionManager middleManager;
    [SerializeField] private BackCardSelectionManager backManager;

    [Header("Operator Parent")]
    [Tooltip("Front, Middle, Back 오퍼레이터가 들어 있는 상위 오브젝트")]
    [SerializeField] private Transform operatorParent;

    [Header("Battle Button")]
    [SerializeField] private GameObject battleButton;

    private Operator frontOperator;
    private Operator middleOperator;
    private Operator backOperator;

    private bool battleRunning;

    public bool CanBattle { get; private set; }

    public Operator FrontOperator => frontOperator;
    public Operator MiddleOperator => middleOperator;
    public Operator BackOperator => backOperator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ReloadOperatorsFromParent();
    }

    public void ReloadOperatorsFromParent()
    {
        frontOperator = null;
        middleOperator = null;
        backOperator = null;

        if (operatorParent == null)
        {
            Debug.LogError(
                "[Battle Access] Operator Parent가 없습니다. " +
                "CharacterSet을 연결해 주십시오.",
                this);

            Refresh();
            return;
        }

        Operator[] operators =
            operatorParent.GetComponentsInChildren<Operator>(true);

        foreach (Operator op in operators)
        {
            if (op == null)
                continue;

            switch (op.Position)
            {
                case OperatorPosition.Front:
                    RegisterOperator(
                        ref frontOperator,
                        op,
                        "Front");
                    break;

                case OperatorPosition.Middle:
                    RegisterOperator(
                        ref middleOperator,
                        op,
                        "Middle");
                    break;

                case OperatorPosition.Back:
                    RegisterOperator(
                        ref backOperator,
                        op,
                        "Back");
                    break;
            }
        }

        LogOperatorResult();
        Refresh();
    }

    private void RegisterOperator(
        ref Operator slot,
        Operator candidate,
        string positionName)
    {
        if (slot != null)
        {
            Debug.LogError(
                $"[Battle Access] {positionName} 위치의 " +
                "Operator가 두 명 이상입니다.\n" +
                $"기존: {slot.gameObject.name}\n" +
                $"추가 발견: {candidate.gameObject.name}",
                candidate);

            return;
        }

        slot = candidate;
    }

    private void LogOperatorResult()
    {
        Debug.Log(
            "[Battle Access] Operator 자동 검색 완료\n" +
            $"Front: {GetOperatorName(frontOperator)}\n" +
            $"Middle: {GetOperatorName(middleOperator)}\n" +
            $"Back: {GetOperatorName(backOperator)}");

        if (frontOperator == null)
        {
            Debug.LogError(
                "[Battle Access] Front Operator를 찾지 못했습니다.");
        }

        if (middleOperator == null)
        {
            Debug.LogError(
                "[Battle Access] Middle Operator를 찾지 못했습니다.");
        }

        if (backOperator == null)
        {
            Debug.LogError(
                "[Battle Access] Back Operator를 찾지 못했습니다.");
        }
    }

    private string GetOperatorName(Operator op)
    {
        if (op == null)
            return "없음";

        if (op.Data != null &&
            !string.IsNullOrWhiteSpace(
                op.Data.OperatorName))
        {
            return op.Data.OperatorName;
        }

        return op.gameObject.name;
    }

    public void SetBattleRunning(bool running)
    {
        battleRunning = running;
        Refresh();
    }

    public void Refresh()
    {
        bool managersReady =
            frontManager != null &&
            middleManager != null &&
            backManager != null;

        bool operatorsReady =
            frontOperator != null &&
            middleOperator != null &&
            backOperator != null;

        bool cardsReady =
            managersReady &&
            frontManager.IsAccess &&
            middleManager.IsAccess &&
            backManager.IsAccess;

        bool frontActionSelected =
            managersReady &&
            frontManager.ActionType !=
                FrontActionType.None;

        bool frontTargetReady =
            IsFrontTargetReady();

        bool middleTargetReady =
            IsTargetReady(middleOperator);

        bool backTargetReady =
            IsTargetReady(backOperator);

        CanBattle =
            !battleRunning &&
            managersReady &&
            operatorsReady &&
            cardsReady &&
            frontActionSelected &&
            frontTargetReady &&
            middleTargetReady &&
            backTargetReady;

        if (battleButton != null)
        {
            battleButton.SetActive(CanBattle);
        }
    }

    private bool IsFrontTargetReady()
    {
        if (frontManager == null ||
            frontOperator == null)
        {
            return false;
        }

        if (frontManager.ActionType ==
            FrontActionType.None)
        {
            return false;
        }

        if (frontManager.ActionType ==
            FrontActionType.Defense)
        {
            return true;
        }

        return IsTargetReady(frontOperator);
    }

    private bool IsTargetReady(Operator op)
    {
        if (op == null)
            return false;

        if (!op.RequiresEnemyTarget)
            return true;

        if (EnemyTargetingManager.Instance == null)
            return false;

        return EnemyTargetingManager.Instance
            .IsTargetRequirementMet(op);
    }
}