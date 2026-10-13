using UnityEngine;

[CreateAssetMenu(fileName = "NewCache", menuName = "Game/Shop/Cache")]
public class CacheData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string cacheId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Packet Price")]
    [Min(0)]
    [SerializeField] private int packetPrice;

    [Header("Flat Operator Stat Bonuses")]
    [Min(0)]
    [SerializeField] private int maxHPBonus;
    [Min(0)]
    [SerializeField] private int attackBonus;
    [Min(0)]
    [SerializeField] private int defenseBonus;
    [Min(0)]
    [SerializeField] private int artsResistanceBonus;
    [Min(0f)]
    [SerializeField] private float attackSpeedBonus;

    public string CacheId => cacheId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int PacketPrice => packetPrice;
    public int MaxHPBonus => maxHPBonus;
    public int AttackBonus => attackBonus;
    public int DefenseBonus => defenseBonus;
    public int ArtsResistanceBonus => artsResistanceBonus;
    public float AttackSpeedBonus => attackSpeedBonus;
}
