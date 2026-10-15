using System;
using UnityEngine;
using Randint.Data;

[CreateAssetMenu(fileName = "RunProgression", menuName = "Game/Run/Progression Settings")]
public class RunProgressionSettings : ScriptableObject
{
    [SerializeField, GameDefinitionId("run_rules")] private string definitionId;
    private RunRulesDefinition Definition => string.IsNullOrEmpty(definitionId) ? null : GameData.Catalog.Rules(definitionId);
    [Min(0)] [SerializeField] private int initialPackets = 30;
    [Min(0)] [SerializeField] private int victoryPackets = 8;
    [Min(0)] [SerializeField] private int victoryExperience = 12;
    [Min(1)] [SerializeField] private int firstLayerExperience = 16;
    [Min(1.01f)] [SerializeField] private float experienceGrowth = 1.5f;
    [Min(0)] [SerializeField] private int levelUpPackets = 10;

    public int InitialPackets => Definition?.initialPackets ?? initialPackets;
    public int VictoryPackets => Definition?.victoryPackets ?? victoryPackets;
    public int VictoryExperience => Definition?.victoryExperience ?? victoryExperience;
    public int LevelUpPackets => Definition?.levelUpPackets ?? levelUpPackets;
    private int FirstExperience => Definition?.firstLayerExperience ?? firstLayerExperience;
    private float Growth => Definition?.experienceGrowth ?? experienceGrowth;
    public bool IsValid => InitialPackets >= 0 && VictoryPackets >= 0 && VictoryExperience >= 0 &&
                           FirstExperience > 0 && Growth > 1f &&
                           !float.IsNaN(Growth) && !float.IsInfinity(Growth) && LevelUpPackets >= 0;

    public int RequiredExperience(int layer)
    {
        if (layer < 1 || !IsValid)
            throw new InvalidOperationException("Invalid Layer or progression settings.");

        double required = FirstExperience * Math.Pow(Growth, layer - 1);
        return required >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(required);
    }
}
