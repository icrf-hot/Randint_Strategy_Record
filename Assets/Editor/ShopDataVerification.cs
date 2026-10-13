using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ShopDataVerification
{
    public static void Run()
    {
        ShopCatalogData catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogData>(
            "Assets/Shop/Data/PrototypeShopCatalog.asset");
        if (catalog == null || catalog.Currency == null ||
            catalog.Currency.CurrencyId != "packet" || catalog.Currency.DisplayName != "Packet")
            throw new InvalidOperationException("Shop catalog or Packet currency reference is invalid.");

        if (catalog.Items == null || catalog.Items.Count != 6)
            throw new InvalidOperationException("Prototype catalog must contain six Cache assets.");

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (CacheData item in catalog.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.CacheId) ||
                !ids.Add(item.CacheId) || string.IsNullOrWhiteSpace(item.DisplayName))
                throw new InvalidOperationException("Cache reference, ID, or display name is invalid.");

            if (item.PacketPrice < 0 || item.MaxHPBonus < 0 || item.AttackBonus < 0 ||
                item.DefenseBonus < 0 || item.ArtsResistanceBonus < 0 || item.AttackSpeedBonus < 0f)
                throw new InvalidOperationException("Prices and enhancement values must be nonnegative.");

            if (item.MaxHPBonus == 0 && item.AttackBonus == 0 && item.DefenseBonus == 0 &&
                item.ArtsResistanceBonus == 0 && item.AttackSpeedBonus == 0f)
                throw new InvalidOperationException("Prototype Cache must contain an enhancement.");
        }

        Debug.Log("[Shop Data Verification] Six Cache assets and Packet catalog references passed.");
    }
}
