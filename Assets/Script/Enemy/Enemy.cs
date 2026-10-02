using UnityEngine;
using Randint.Data;

public class Enemy : MonoBehaviour
{
    [Header("JSON Definition")]
    [SerializeField, GameDefinitionId("enemy")] private string dataId;
    public string DataId => dataId;
    // 기본값은 공유 데이터에서 조회하며, 현재 HP는 생성된 전투 객체에만 저장합니다.
    private EnemyDefinition Definition => GameData.Catalog.Enemy(dataId);
    private int maxHP => Definition.maxHP;
    private int attack => Definition.attack;
    private int defense => Definition.defense;
    private int artsResistance => Definition.artsResistance;
    private float attackSpeed => Definition.attackSpeed;

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
        Debug.Log($"{gameObject.name} 사망");

        Destroy(gameObject);
    }
}
