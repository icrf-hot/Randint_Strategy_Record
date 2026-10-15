using UnityEngine;
using Randint.Data;

public enum CacheEffectKind
{
    GlobalOperatorStats = 0,
    GlobalPacketAndExperience = 1
}

[CreateAssetMenu(fileName = "NewCache", menuName = "Game/Shop/Cache")]
public class CacheData : ScriptableObject
{
    [SerializeField, GameDefinitionId("cache")] private string definitionId;
    private CacheDefinition Definition => string.IsNullOrEmpty(definitionId) ? null : GameData.Catalog.Cache(definitionId);
    [Header("Identity")]
    [SerializeField] private string cacheId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Global Effect")]
    [SerializeField] private CacheEffectKind effectKind;
    [Tooltip("Pending items are definitions only. Their price and effect must not be used for purchases.")]
    [SerializeField] private bool configurationPending;

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

    [Header("Victory Reward Bonus (%)")]
    [Min(0)] [SerializeField] private int packetGainPercent;
    [Min(0)] [SerializeField] private int experienceGainPercent;

    public string CacheId => Definition?.id ?? cacheId;
    public string DisplayName => Definition == null ? displayName : GameData.Text(Definition.nameKey);
    public string Description => Definition == null ? description : GameData.Text(Definition.descriptionKey);
    public Sprite Icon => icon;
    public CacheEffectKind EffectKind => Definition == null ? effectKind :
        (CacheEffectKind)System.Enum.Parse(typeof(CacheEffectKind), Definition.effectKind);
    public bool IsConfigured => !configurationPending &&
        !string.IsNullOrWhiteSpace(CacheId) && PacketPrice >= 0 &&
        MaxHPBonus >= 0 && AttackBonus >= 0 && DefenseBonus >= 0 &&
        ArtsResistanceBonus >= 0 && AttackSpeedBonus >= 0f && !float.IsInfinity(AttackSpeedBonus) &&
        ((EffectKind == CacheEffectKind.GlobalOperatorStats &&
          (MaxHPBonus > 0 || AttackBonus > 0 || DefenseBonus > 0 || ArtsResistanceBonus > 0 || AttackSpeedBonus > 0f)) ||
         (EffectKind == CacheEffectKind.GlobalPacketAndExperience && PacketGainPercent >= 0 &&
          ExperienceGainPercent >= 0 && (PacketGainPercent > 0 || ExperienceGainPercent > 0)));
    public int PacketGainPercent => Definition?.packetGainPercent ?? packetGainPercent;
    public int ExperienceGainPercent => Definition?.experienceGainPercent ?? experienceGainPercent;
    public int PacketPrice => Definition?.packetPrice ?? packetPrice;
    public int MaxHPBonus => Definition?.maxHPBonus ?? maxHPBonus;
    public int AttackBonus => Definition?.attackBonus ?? attackBonus;
    public int DefenseBonus => Definition?.defenseBonus ?? defenseBonus;
    public int ArtsResistanceBonus => Definition?.artsResistanceBonus ?? artsResistanceBonus;
    public float AttackSpeedBonus => Definition?.attackSpeedBonus ?? attackSpeedBonus;
}
