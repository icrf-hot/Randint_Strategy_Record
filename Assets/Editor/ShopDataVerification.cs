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

        if (catalog.Items == null || catalog.Items.Count != 7)
            throw new InvalidOperationException("Prototype catalog must contain seven Cache assets.");

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int statItems = 0;
        int rewardItems = 0;
        foreach (CacheData item in catalog.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.CacheId) ||
                !ids.Add(item.CacheId) || string.IsNullOrWhiteSpace(item.DisplayName))
                throw new InvalidOperationException("Cache reference, ID, or display name is invalid.");

            if (item.PacketPrice < 0 || item.MaxHPBonus < 0 || item.AttackBonus < 0 ||
                item.DefenseBonus < 0 || item.ArtsResistanceBonus < 0 || item.AttackSpeedBonus < 0f)
                throw new InvalidOperationException("Prices and enhancement values must be nonnegative.");

            bool hasStats = item.MaxHPBonus != 0 || item.AttackBonus != 0 ||
                item.DefenseBonus != 0 || item.ArtsResistanceBonus != 0 || item.AttackSpeedBonus != 0f;
            switch (item.EffectKind)
            {
                case CacheEffectKind.GlobalOperatorStats:
                    if (!item.IsConfigured || !hasStats)
                        throw new InvalidOperationException("Stat Cache must contain a configured enhancement.");
                    statItems++;
                    break;
                case CacheEffectKind.GlobalPacketAndExperience:
                    if (item.IsConfigured || item.PacketPrice != 0 || hasStats)
                        throw new InvalidOperationException("Reward Cache must remain unconfigured pending Q-C06.");
                    SerializedObject serialized = new SerializedObject(item);
                    if (!serialized.FindProperty("configurationPending").boolValue)
                        throw new InvalidOperationException("Reward Cache must explicitly declare pending configuration.");
                    rewardItems++;
                    break;
                default:
                    throw new InvalidOperationException("Unknown Cache effect kind.");
            }
        }

        if (statItems != 6 || rewardItems != 1)
            throw new InvalidOperationException("Expected six global stat Caches and one pending reward Cache.");

        Debug.Log("[Shop Data Verification] Six global stat Caches, one pending reward Cache and Packet references passed.");
    }
}
