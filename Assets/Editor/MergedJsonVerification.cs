using System;
using System.IO;
using Randint.Data;
using UnityEditor;
using UnityEngine;

public static class MergedJsonVerification
{
    public static void Run()
    {
        GameDataValidationMenu.ValidateAll();
        CachePurchaseVerification.Run();
        RunProgressionVerification.Run();
        CacheOperatorVerification.Run();
        CombatRulesVerification.Run();
        CardSelectionLifecycleVerification.Run();
        MapNodeTypeVerification.Run();
        var rules = GameData.Catalog.Rules("default_run");
        if (rules.healerCardMultiplier != 100 || rules.naturalSpPerAttack != 1 || GameData.Catalog.Shop("prototype_shop").cacheIds.Length != 7)
            throw new InvalidOperationException("Merged JSON rules changed current gameplay.");
        Debug.Log("[Merged JSON Verification] Current Cache/progression/HP/SP/map enum behavior and JSON references passed.");
    }
}
