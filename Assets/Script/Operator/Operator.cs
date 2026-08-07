using UnityEngine;

public class Operator : MonoBehaviour
{
    [Header("기본 스탯")]

    [SerializeField] private int maxHP = 100;
    [SerializeField] private int attack = 20;
    [SerializeField] private int defense = 10;
    [SerializeField] private int artsResistance = 15;
    [SerializeField] private float attackSpeed = 1.0f;

    private int currentHP;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Attack => attack;
    public int Defense => defense;
    public int ArtsResistance => artsResistance;
    public float AttackSpeed => attackSpeed;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakePhysicalDamage(int damage)
    {
        int finalDamage = Mathf.Max(1, damage - defense);

        currentHP -= finalDamage;
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log($"{gameObject.name} 물리 피해 : {finalDamage}");

        if (currentHP <= 0)
            Die();
    }

    public void TakeArtsDamage(int damage)
    {
        int finalDamage = Mathf.Max(1, damage - artsResistance);

        currentHP -= finalDamage;
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log($"{gameObject.name} 아츠 피해 : {finalDamage}");

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 전투 불능");

        Destroy(gameObject);
    }
}