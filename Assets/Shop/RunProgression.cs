using System;

public sealed class RunProgression
{
    private RunProgressionSettings settings;

    public PacketWallet Packets { get; } = new PacketWallet();
    public int Layer { get; private set; } = 1;
    public int Experience { get; private set; }
    public bool IsInitialized => settings != null;
    public int RequiredExperience => settings != null ? settings.RequiredExperience(Layer) : 0;

    public bool Start(RunProgressionSettings newSettings)
    {
        if (newSettings == null || !newSettings.IsValid)
            return false;

        Clear();
        settings = newSettings;
        return Packets.TryAdd(settings.InitialPackets);
    }

    public bool TryGrantVictory(out int packetsGained, out int layersGained,
        int packetGainPercent = 0, int experienceGainPercent = 0)
    {
        packetsGained = 0;
        layersGained = 0;
        if (settings == null || !settings.IsValid || packetGainPercent < 0 || experienceGainPercent < 0)
            return false;

        int nextLayer = Layer;
        long victoryExperience = RoundReward(settings.VictoryExperience, experienceGainPercent);
        long victoryPackets = RoundReward(settings.VictoryPackets, packetGainPercent);
        if (victoryExperience > int.MaxValue || victoryPackets > int.MaxValue)
            return false;
        long remainingExperience = Experience + victoryExperience;
        while (remainingExperience >= settings.RequiredExperience(nextLayer))
        {
            if (nextLayer == int.MaxValue)
                return false;

            remainingExperience -= settings.RequiredExperience(nextLayer);
            nextLayer++;
        }

        long reward = victoryPackets + (long)(nextLayer - Layer) * settings.LevelUpPackets;
        // Validate the whole reward before changing the wallet or progression.
        if (reward > int.MaxValue || !Packets.TryAdd((int)reward))
            return false;

        packetsGained = (int)reward;
        layersGained = nextLayer - Layer;
        Layer = nextLayer;
        Experience = (int)remainingExperience;
        return true;
    }

    private static long RoundReward(int amount, int percent)
    {
        return (long)Math.Round((decimal)amount * (100L + percent) / 100m, 0, MidpointRounding.AwayFromZero);
    }

    public void Clear()
    {
        Packets.Clear();
        settings = null;
        Layer = 1;
        Experience = 0;
    }
}
