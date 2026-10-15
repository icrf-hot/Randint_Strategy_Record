using System;
using System.Collections.Generic;

public enum CachePurchaseResult
{
    Success,
    RunNotStarted,
    InvalidCatalog,
    ItemNotFound,
    NotConfigured,
    AlreadyPurchased,
    InsufficientPackets
}

public sealed class RunCacheInventory
{
    private readonly List<CacheData> owned = new List<CacheData>();
    private readonly HashSet<string> purchasedIds = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlyList<CacheData> Owned { get; }

    public RunCacheInventory()
    {
        Owned = owned.AsReadOnly();
    }

    public bool HasPurchased(string cacheId)
    {
        return !string.IsNullOrWhiteSpace(cacheId) && purchasedIds.Contains(cacheId);
    }

    internal CachePurchaseResult TryPurchase(PacketWallet wallet, ShopCatalogData catalog, string cacheId)
    {
        if (catalog == null || catalog.Currency == null ||
            catalog.Currency.CurrencyId != "packet" || catalog.Items == null)
            return CachePurchaseResult.InvalidCatalog;
        if (string.IsNullOrWhiteSpace(cacheId))
            return CachePurchaseResult.ItemNotFound;

        CacheData item = null;
        HashSet<string> catalogIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CacheData candidate in catalog.Items)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.CacheId) ||
                !catalogIds.Add(candidate.CacheId))
                return CachePurchaseResult.InvalidCatalog;
            if (candidate.CacheId == cacheId)
                item = candidate;
        }

        if (item == null)
            return CachePurchaseResult.ItemNotFound;
        if (HasPurchased(cacheId))
            return CachePurchaseResult.AlreadyPurchased;
        if (!item.IsConfigured)
            return CachePurchaseResult.NotConfigured;
        if (!wallet.TrySpend(item.PacketPrice))
            return CachePurchaseResult.InsufficientPackets;

        // No callbacks between validation, payment and ownership publication.
        purchasedIds.Add(cacheId);
        owned.Add(item);
        return CachePurchaseResult.Success;
    }

    internal void Clear()
    {
        owned.Clear();
        purchasedIds.Clear();
    }
}
