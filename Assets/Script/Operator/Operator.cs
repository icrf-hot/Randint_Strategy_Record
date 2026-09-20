using UnityEngine;

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

    // 현재 전투에서 사용되는 실제 공격력
    public int Attack =>
        currentAttack + battleAttackBonus;

    // 현재 전투에서 사용되는 실제 방어력
    public int Defense =>
        currentDefense + battleDefenseBonus;

    // 현재 전투에서 사용되는 실제 아츠 저항
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
    // 실시간 HP
    // =========================================================


    public float HPPercent =>
    MaxHP > 0
        ? (float)CurrentHP / MaxHP
        : 0f;

    // =========================================================
    // HP 배율
    // =========================================================


    public int CurrentHP =>
        currentHP;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
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
            $"{gameObject.name} 물리 피해 : {finalDamage}");

        if (currentHP <= 0)
            Die();
    }

    public void TakeArtsDamage(int damage)
    {
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
            $"{gameObject.name} 아츠 피해 : {finalDamage}");

        if (currentHP <= 0)
            Die();
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

        if (currentHP <= 0)
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
            $"{gameObject.name} 회복 : {actualHeal}");
    }

    // =========================================================
    // 사망
    // =========================================================

    private void Die()
    {
        Debug.Log(
            $"{gameObject.name} 전투 불능");

        Destroy(gameObject);
    }


    // =========================================================
    // 디버그 전용 피해 코드
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
            $"[DEBUG] {gameObject.name} HP 감소 : " +
            $"-{damage} " +
            $"({currentHP}/{MaxHP})");

        if (currentHP <= 0)
            Die();
    }
}