using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RunProgression", menuName = "Game/Run/Progression Settings")]
public class RunProgressionSettings : ScriptableObject
{
    [Min(0)] [SerializeField] private int initialPackets = 30;
    [Min(0)] [SerializeField] private int victoryPackets = 8;
    [Min(0)] [SerializeField] private int victoryExperience = 12;
    [Min(1)] [SerializeField] private int firstLayerExperience = 16;
    [Min(1.01f)] [SerializeField] private float experienceGrowth = 1.5f;
    [Min(0)] [SerializeField] private int levelUpPackets = 10;

    public int InitialPackets => initialPackets;
    public int VictoryPackets => victoryPackets;
    public int VictoryExperience => victoryExperience;
    public int LevelUpPackets => levelUpPackets;
    public bool IsValid => initialPackets >= 0 && victoryPackets >= 0 && victoryExperience >= 0 &&
                           firstLayerExperience > 0 && experienceGrowth > 1f &&
                           !float.IsNaN(experienceGrowth) && !float.IsInfinity(experienceGrowth) && levelUpPackets >= 0;

    public int RequiredExperience(int layer)
    {
        if (layer < 1 || !IsValid)
            throw new InvalidOperationException("Invalid Layer or progression settings.");

        double required = firstLayerExperience * Math.Pow(experienceGrowth, layer - 1);
        return required >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(required);
    }
}
