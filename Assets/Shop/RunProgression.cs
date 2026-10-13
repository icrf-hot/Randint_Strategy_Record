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

    public bool TryGrantVictory(out int packetsGained, out int layersGained)
    {
        packetsGained = 0;
        layersGained = 0;
        if (settings == null || !settings.IsValid)
            return false;

        int nextLayer = Layer;
        long remainingExperience = (long)Experience + settings.VictoryExperience;
        while (remainingExperience >= settings.RequiredExperience(nextLayer))
        {
            if (nextLayer == int.MaxValue)
                return false;

            remainingExperience -= settings.RequiredExperience(nextLayer);
            nextLayer++;
        }

        long reward = settings.VictoryPackets + (long)(nextLayer - Layer) * settings.LevelUpPackets;
        // Validate the whole reward before changing the wallet or progression.
        if (reward > int.MaxValue || !Packets.TryAdd((int)reward))
            return false;

        packetsGained = (int)reward;
        layersGained = nextLayer - Layer;
        Layer = nextLayer;
        Experience = (int)remainingExperience;
        return true;
    }

    public void Clear()
    {
        Packets.Clear();
        settings = null;
        Layer = 1;
        Experience = 0;
    }
}
