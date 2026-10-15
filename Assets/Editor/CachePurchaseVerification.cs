using System;
using UnityEditor;
using UnityEngine;

public static class CachePurchaseVerification
{
    public static void Run()
    {
        ShopDataVerification.Run();
        ShopCatalogData catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
        RunProgressionSettings settings = AssetDatabase.LoadAssetAtPath<RunProgressionSettings>("Assets/Shop/Data/RunProgression.asset");
        ShopCatalogData otherCatalog = UnityEngine.Object.Instantiate(catalog);
        CacheData invalidItem = UnityEngine.Object.Instantiate(catalog.Items[0]);
        try
        {
            MapRunState.Reset();
            Expect(CachePurchaseResult.RunNotStarted, catalog, "cache_hp", 0, 0);
            Require(MapRunState.StartNewRun(settings), "New run failed.");
            Expect(CachePurchaseResult.InvalidCatalog, null, "cache_hp", 30, 0);
            Expect(CachePurchaseResult.ItemNotFound, catalog, "unknown", 30, 0);
            Expect(CachePurchaseResult.Success, catalog, "cache_hp", 18, 1);
            Expect(CachePurchaseResult.AlreadyPurchased, catalog, "cache_hp", 18, 1);
            Expect(CachePurchaseResult.AlreadyPurchased, otherCatalog, "cache_hp", 18, 1);
            MapRunState.SaveCurrentNode(8, "Shop");
            MapRunState.BeginMapEntry();
            Require(MapRunState.EnsureStarted(settings) && MapRunState.Caches.HasPurchased("cache_hp") &&
                MapRunState.Caches.Owned.Count == 1 && MapRunState.Packets.Balance == 18,
                "Scene initialization cleared inventory or granted extra Packet.");
            Require(!MapRunState.StartNewRun(null) && MapRunState.Caches.Owned.Count == 1, "Invalid restart cleared ownership.");
            Expect(CachePurchaseResult.Success, catalog, "cache_attack", 2, 2);
            Require(MapRunState.Packets.TryAdd(10), "Fixture funding failed.");
            Expect(CachePurchaseResult.Success, catalog, "cache_defense", 0, 3);
            Expect(CachePurchaseResult.InsufficientPackets, catalog, "cache_all_stats", 0, 3);
            Require(!MapRunState.Caches.HasPurchased("cache_all_stats"), "Failed purchase left history.");
            Require(MapRunState.StartNewRun(settings) && !MapRunState.HasCurrentNode &&
                !MapRunState.Caches.HasPurchased("cache_hp") && MapRunState.Caches.Owned.Count == 0, "New game retained Cache state.");
            Expect(CachePurchaseResult.Success, catalog, "cache_hp", 18, 1);

            SerializedObject itemData = new SerializedObject(invalidItem);
            itemData.FindProperty("packetPrice").intValue = -1;
            itemData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject catalogData = new SerializedObject(otherCatalog);
            SerializedProperty items = catalogData.FindProperty("items");
            items.arraySize = 1;
            items.GetArrayElementAtIndex(0).objectReferenceValue = invalidItem;
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            Require(MapRunState.StartNewRun(settings), "Fixture reset failed.");
            Expect(CachePurchaseResult.NotConfigured, otherCatalog, "cache_hp", 30, 0);
            items.arraySize = 2;
            items.GetArrayElementAtIndex(1).objectReferenceValue = invalidItem;
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            Expect(CachePurchaseResult.InvalidCatalog, otherCatalog, "cache_hp", 30, 0);
            Expect(CachePurchaseResult.Success, catalog, "cache_rewards", 6, 1);
            Require(MapRunState.TryGrantVictory(out int gained, out int levels) && gained == 22 && levels == 1 &&
                MapRunState.Packets.Balance == 28 && MapRunState.Progression.Layer == 2 &&
                MapRunState.Progression.Experience == 2 && MapRunState.Progression.RequiredExperience == 24,
                "50% reward changed Layer bonus, threshold or experience rounding.");
        }
        finally
        {
            MapRunState.Reset();
            UnityEngine.Object.DestroyImmediate(otherCatalog);
            UnityEngine.Object.DestroyImmediate(invalidItem);
        }
        Debug.Log("[Cache Purchase Verification] Payment, rejection atomicity, run-wide duplicates, initialization and new-game reset passed.");
    }

    private static void Expect(CachePurchaseResult result, ShopCatalogData catalog, string id, int balance, int count)
    {
        Require(MapRunState.TryPurchaseCache(catalog, id) == result, "Unexpected purchase result for " + id + ": expected " + result);
        Require(MapRunState.Packets.Balance == balance && MapRunState.Caches.Owned.Count == count, "Purchase partially changed state.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
