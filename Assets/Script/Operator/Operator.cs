using UnityEngine;
using System;

public class Operator : MonoBehaviour
{
    [Header("Operator Data")]
    [SerializeField] private OperatorData data;

    // =========================================================
    // 기본/현재 스탯
    // =========================================================

    private int currentHP;
    private int currentAttack;
    private int currentDefense;
    private int currentArtsResistance;
    private float currentAttackSpeed;

    // =========================================================
    // 이번 전투의 임시 보정값
    // =========================================================

    private int battleAttackBonus;
    private int battleDefenseBonus;
    private int battleArtsResistanceBonus;

    // =========================================================
    // Skill
    // =========================================================

    private OperatorSkill[] skills;

    public OperatorSkill[] Skills => skills;

    // =========================================================
    // Class / Targeting
    // =========================================================

    public OperatorPosition Position
    {
        get
        {
            if (data == null)
                return OperatorPosition.Front;

            return data.Position;
        }
    }

    public OperatorClass Class
    {
        get
        {
            if (data == null)
                return OperatorClass.Defender;

            return data.DetailedClass;
        }
    }

    public bool RequiresEnemyTarget
    {
        get
        {
            return
                data != null &&
                data.RequiresEnemyTarget;
        }
    }

    // =========================================================
    // Data
    // =========================================================

    public OperatorData Data => data;

    // =========================================================
    // 기본 스탯
    // =========================================================

    public int MaxHP => data.MaxHP;

    // 현재 전투에서 사용하는 실제 공격력
    public int Attack =>
        currentAttack + battleAttackBonus;

    // 현재 전투에서 사용하는 실제 방어력
    public int Defense =>
        currentDefense + battleDefenseBonus;

    // 현재 전투에서 사용하는 실제 아츠 저항
    public int ArtsResistance =>
        currentArtsResistance +
        battleArtsResistanceBonus;

    public float AttackSpeed =>
        currentAttackSpeed;

    // =========================================================
    // 원래 스탯
    // =========================================================

    public int BaseAttack =>
        currentAttack;

    public int BaseDefense =>
        currentDefense;

    public int BaseArtsResistance =>
        currentArtsResistance;

    // =========================================================
    // 전투 보정값
    // =========================================================

    public int AttackBonus =>
        battleAttackBonus;

    public int DefenseBonus =>
        battleDefenseBonus;

    public int ArtsResistanceBonus =>
        battleArtsResistanceBonus;

    // =========================================================
    // HP 비율
    // =========================================================

    public float HPPercent =>
        MaxHP > 0
            ? (float)CurrentHP / MaxHP
            : 0f;

    // =========================================================
    // 실시간 HP
    // =========================================================

    public int CurrentHP =>
        currentHP;

    // =========================================================
    // 퇴각 판정
    // =========================================================

    [Header("Retreat")]
    [SerializeField]
    [Range(0f, 1f)]
    private float retreatedBrightness = 0.25f;

    private OperatorBattleState battleState =
        OperatorBattleState.Active;

    private int redeployTurnsRemaining = -1;

    private SpriteRenderer[] visualRenderers;
    private Color[] originalRendererColors;

    public OperatorBattleState BattleState =>
        battleState;

    public bool IsCombatActive =>
        battleState == OperatorBattleState.Active &&
        currentHP > 0;

    public bool IsRetreated =>
        battleState == OperatorBattleState.Retreated;

    public bool HasScheduledRedeployment =>
        IsRetreated &&
        redeployTurnsRemaining > 0;

    public int RedeployTurnsRemaining =>
        redeployTurnsRemaining;

    // 추후 스킬 또는 별도 시스템에서 구독할 수 있는 기본 훅입니다.
    // 승리·패배 이벤트와는 별개입니다.
    public event Action<Operator> Retreated;
    public event Action<Operator> Revived;
    public event Action<Operator> Redeployed;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // 시작시 초기화
        // -----------------------------------------------------
        CacheVisualRenderers();

        battleState = OperatorBattleState.Active;
        redeployTurnsRemaining = -1;

        if (data == null)
        {
            Debug.LogError(
                $"{gameObject.name}에 OperatorData가 연결되지 않았습니다.",
                this);

            return;
        }

        // -----------------------------------------------------
        // Skill 초기화
        // -----------------------------------------------------

        if (data.Skills != null)
        {
            skills =
                new OperatorSkill[data.Skills.Length];

            for (int i = 0;
                 i < data.Skills.Length;
                 i++)
            {
                if (data.Skills[i] == null)
                {
                    Debug.LogWarning(
                        $"{data.OperatorName}의 Skill {i}가 비어 있습니다.",
                        this);

                    continue;
                }

                skills[i] =
                    new OperatorSkill(
                        data.Skills[i]);
            }
        }

        // -----------------------------------------------------
        // 기본 스탯 초기화
        // -----------------------------------------------------

        currentHP =
            data.MaxHP;

        currentAttack =
            data.Attack;

        currentDefense =
            data.Defense;

        currentArtsResistance =
            data.ArtsResistance;

        currentAttackSpeed =
            data.AttackSpeed;

        // -----------------------------------------------------
        // 전투 보정 초기화
        // -----------------------------------------------------

        ResetBattleBonus();
    }

    // =========================================================
    // 피해
    // =========================================================

    public void TakePhysicalDamage(int damage)
    {
        if (!IsCombatActive)
            return;

        if (damage <= 0)
            return;


        int finalDamage =
            Mathf.Max(
                1,
                damage - Defense);

        currentHP -= finalDamage;

        currentHP =
            Mathf.Max(
                currentHP,
                0);

        Debug.Log(
            $"{gameObject.name} 물리 피해: {finalDamage}");

        if (currentHP <= 0)
            Retreat();
    }

    public void TakeArtsDamage(int damage)
    {
        if (!IsCombatActive)
            return;

        if (damage <= 0)
            return;

        int finalDamage =
            Mathf.Max(
                1,
                damage - ArtsResistance);

        currentHP -= finalDamage;

        currentHP =
            Mathf.Max(
                currentHP,
                0);

        Debug.Log(
            $"{gameObject.name} 아츠 피해: {finalDamage}");

        if (currentHP <= 0)
            Retreat();
    }

    // =========================================================
    // 전투 보정 적용
    // =========================================================

    public void ApplyBattleBonus(
        int attackBonus,
        int defenseBonus,
        int artsResistanceBonus)
    {
        battleAttackBonus =
            attackBonus;

        battleDefenseBonus =
            defenseBonus;

        battleArtsResistanceBonus =
            artsResistanceBonus;

        Debug.Log(
            $"{gameObject.name} 전투 보정 적용\n" +
            $"AT : +{battleAttackBonus}\n" +
            $"DF : +{battleDefenseBonus}\n" +
            $"RES : +{battleArtsResistanceBonus}");
    }

    // =========================================================
    // 전투 보정 제거
    // =========================================================

    public void ResetBattleBonus()
    {
        battleAttackBonus = 0;
        battleDefenseBonus = 0;
        battleArtsResistanceBonus = 0;
    }

    // =========================================================
    // 기본 스탯 변경
    // =========================================================

    public void SetAttack(int value)
    {
        currentAttack = value;
    }

    public void SetDefense(int value)
    {
        currentDefense = value;
    }

    public void SetArtsResistance(int value)
    {
        currentArtsResistance = value;
    }

    public void SetAttackSpeed(float value)
    {
        currentAttackSpeed = value;
    }

    // =========================================================
    // 치유
    // =========================================================

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        if (!IsCombatActive)
            return;

        int previousHP = currentHP;

        currentHP += amount;

        currentHP =
            Mathf.Min(
                currentHP,
                MaxHP);

        int actualHeal =
            currentHP - previousHP;

        Debug.Log(
            $"{gameObject.name} 회복: {actualHeal}");
    }

    // =========================================================
    // 사망
    // =========================================================

    private void Retreat()
    {
        if (IsRetreated)
            return;

        currentHP = 0;
        battleState = OperatorBattleState.Retreated;

        // 아직 재배치 예약은 없습니다.
        // 추후 Retreated 이벤트 구독자가 예약할 수 있습니다.
        redeployTurnsRemaining = -1;

        ApplyRetreatedVisual(true);

        AllyTargetable targetable =
            GetComponent<AllyTargetable>();

        if (targetable != null)
        {
            targetable.HideAllIndicators();
        }

        if (OperatorFocusManager.Instance != null)
        {
            OperatorFocusManager.Instance
                .ExitFocusIfOperator(this);
        }

        Debug.Log(
            $"[Operator Retreat] {gameObject.name} 퇴각");

        Retreated?.Invoke(this);

        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance.Refresh();
        }
    }

    // 추후 부활 스킬에서 호출할 메서드입니다.
    public bool Revive(int restoredHP)
    {
        if (!IsRetreated)
            return false;

        RestoreToBattle(restoredHP);

        Debug.Log(
            $"[Operator Revive] {gameObject.name} 부활 " +
            $"| HP {currentHP}/{MaxHP}");

        Revived?.Invoke(this);
        return true;
    }

    // 추후 퇴각 이벤트 또는 패시브 효과에서 호출합니다.
    public bool ScheduleRedeployment(int turns)
    {
        if (!IsRetreated)
            return false;

        if (turns <= 0)
            return false;

        redeployTurnsRemaining = turns;

        Debug.Log(
            $"[Operator Redeploy] {gameObject.name} " +
            $"{turns}턴 후 재배치 예약");

        return true;
    }

    public void CancelRedeployment()
    {
        redeployTurnsRemaining = -1;
    }

    // 라운드 종료 시 BattleExecuteManager가 먼저 호출합니다.
    public bool ProcessRedeploymentTurn()
    {
        if (!HasScheduledRedeployment)
            return false;

        redeployTurnsRemaining--;

        if (redeployTurnsRemaining > 0)
        {
            Debug.Log(
                $"[Operator Redeploy] {gameObject.name} " +
                $"남은 턴: {redeployTurnsRemaining}");

            return false;
        }

        Redeploy();
        return true;
    }

    private void Redeploy()
    {
        if (!IsRetreated)
            return;

        // 기본 베이스에서는 최대 HP로 복귀합니다.
        // 실제 재배치 HP 규칙이 정해지면 이 값만 변경할 수 있습니다.
        RestoreToBattle(MaxHP);

        Debug.Log(
            $"[Operator Redeploy] {gameObject.name} 재배치 완료");

        Redeployed?.Invoke(this);
    }

    private void RestoreToBattle(int restoredHP)
    {
        currentHP =
            Mathf.Clamp(
                restoredHP,
                1,
                MaxHP
            );

        battleState = OperatorBattleState.Active;
        redeployTurnsRemaining = -1;

        ApplyRetreatedVisual(false);

        if (BattleAccessManager.Instance != null)
        {
            BattleAccessManager.Instance.Refresh();
        }
    }

    private void CacheVisualRenderers()
    {
        visualRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        originalRendererColors =
            new Color[visualRenderers.Length];

        for (int i = 0;
             i < visualRenderers.Length;
             i++)
        {
            originalRendererColors[i] =
                visualRenderers[i].color;
        }
    }

    private void ApplyRetreatedVisual(bool retreated)
    {
        if (visualRenderers == null ||
            originalRendererColors == null)
        {
            return;
        }

        for (int i = 0;
             i < visualRenderers.Length;
             i++)
        {
            SpriteRenderer target =
                visualRenderers[i];

            if (target == null)
                continue;

            Color original =
                originalRendererColors[i];

            if (!retreated)
            {
                target.color = original;
                continue;
            }

            target.color = new Color(
                original.r * retreatedBrightness,
                original.g * retreatedBrightness,
                original.b * retreatedBrightness,
                original.a
            );
        }
    }

    // =========================================================
    // 디버그용 피해 코드
    // =========================================================

    public void DebugDamage(int damage)
    {
        if (damage <= 0)
            return;

        if (currentHP <= 0)
            return;

        currentHP -= damage;

        currentHP =
            Mathf.Max(
                currentHP,
                0);

        Debug.Log(
            $"[DEBUG] {gameObject.name} HP 감소: " +
            $"-{damage} " +
            $"({currentHP}/{MaxHP})");

        if (currentHP <= 0)
            Retreat();
    }
}