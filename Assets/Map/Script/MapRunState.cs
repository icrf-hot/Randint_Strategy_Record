
public static class MapRunState
{
    public static RunProgression Progression { get; } = new RunProgression();
    public static PacketWallet Packets => Progression.Packets;
    public static RunCacheInventory Caches { get; } = new RunCacheInventory();

    public static bool TryGrantVictory(out int packetsGained, out int layersGained)
    {
        long packets = 0, experience = 0;
        foreach (CacheData cache in Caches.Owned)
        {
            if (cache == null || !cache.IsConfigured || cache.EffectKind != CacheEffectKind.GlobalPacketAndExperience)
                continue;
            packets += cache.PacketGainPercent;
            experience += cache.ExperienceGainPercent;
        }
        packetsGained = 0;
        layersGained = 0;
        return packets <= int.MaxValue && experience <= int.MaxValue &&
            Progression.TryGrantVictory(out packetsGained, out layersGained, (int)packets, (int)experience);
    }

    public static CachePurchaseResult TryPurchaseCache(ShopCatalogData catalog, string cacheId)
    {
        if (!Progression.IsInitialized)
            return CachePurchaseResult.RunNotStarted;
        return Caches.TryPurchase(Packets, catalog, cacheId);
    }

    public static bool EnsureStarted(RunProgressionSettings settings)
    {
        return Progression.IsInitialized || Progression.Start(settings);
    }

    public static bool StartNewRun(RunProgressionSettings settings)
    {
        if (settings == null || !settings.IsValid)
            return false;

        Reset();
        return Progression.Start(settings);
    }

    public static bool HasCurrentNode { get; private set; }
    public static int CurrentNodeID { get; private set; }

    public static bool IsEnteringMapFromBattle
    {
        get;
        private set;
    }

    public static string CurrentNodeDisplayName
    {
        get;
        private set;
    }

    public static void SaveCurrentNode(int nodeID, string displayName)
    {
        CurrentNodeID = nodeID;
        CurrentNodeDisplayName = displayName;
        HasCurrentNode = true;
    }

    public static void BeginMapEntry()
    {
        IsEnteringMapFromBattle = true;
    }

    public static void CompleteMapEntry()
    {
        IsEnteringMapFromBattle = false;
    }

    public static void Reset()
    {
        Progression.Clear();
        Caches.Clear();
        CurrentNodeID = 0;
        HasCurrentNode = false;
        IsEnteringMapFromBattle = false;
        CurrentNodeDisplayName = string.Empty;
    }
}
