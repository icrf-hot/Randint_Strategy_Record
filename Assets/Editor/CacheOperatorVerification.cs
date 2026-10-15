using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CacheOperatorVerification
{
    public static void Run()
    {
        ShopCatalogData catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
        RunProgressionSettings settings = AssetDatabase.LoadAssetAtPath<RunProgressionSettings>("Assets/Shop/Data/RunProgression.asset");
        OperatorData data = ScriptableObject.CreateInstance<OperatorData>();
        try
        {
            Require(MapRunState.StartNewRun(settings) && MapRunState.Packets.TryAdd(70), "Fixture initialization failed.");
            foreach (CacheData item in catalog.Items)
                if (item.IsConfigured && item.EffectKind == CacheEffectKind.GlobalOperatorStats)
                    Require(MapRunState.TryPurchaseCache(catalog, item.CacheId) == CachePurchaseResult.Success, "Fixture purchase failed.");
            Require(MapRunState.Caches.Owned.Count == 6, "Expected six owned stat Caches.");
            foreach (OperatorPosition position in Enum.GetValues(typeof(OperatorPosition)))
            {
                SerializedObject source = new SerializedObject(data);
                source.FindProperty("position").intValue = (int)position;
                source.ApplyModifiedPropertiesWithoutUndo();
                VerifyOperator(data, true);
            }
            Require(data.MaxHP == 100 && data.Attack == 20 && data.Defense == 10 &&
                data.ArtsResistance == 15 && data.AttackSpeed == 1f, "Cache changed source OperatorData.");
            Require(MapRunState.StartNewRun(settings), "Restart failed.");
            VerifyOperator(data, false);
            Require(CacheStatBonuses.Add(int.MaxValue, 100, 100) == int.MaxValue, "Stat overflow wrapped.");
        }
        finally
        {
            MapRunState.Reset();
            UnityEngine.Object.DestroyImmediate(data);
        }
        Debug.Log("[Cache Operator Verification] Global stats, base/effective HP, heal cap, round reset, retreat/redeployment and new-battle reset passed.");
    }

    private static void VerifyOperator(OperatorData data, bool enhanced)
    {
        GameObject obj = new GameObject("CacheOperatorFixture");
        obj.SetActive(false);
        Operator op = obj.AddComponent<Operator>();
        try
        {
            SerializedObject serialized = new SerializedObject(op);
            serialized.FindProperty("data").objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            typeof(Operator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(op, null);
            Require(op.BaseMaxHP == 100 && op.MaxHP == (enhanced ? 250 : 100) && op.CurrentHP == op.MaxHP,
                "Base/effective HP separation or initial HP is wrong.");
            Require(op.Attack == (enhanced ? 35 : 20) && op.Defense == (enhanced ? 25 : 10) &&
                op.ArtsResistance == (enhanced ? 22 : 15) && op.AttackSpeed == (enhanced ? 2.5f : 1f), "Global bonuses are incorrect.");
            op.DebugDamage(10);
            op.Heal(100);
            Require(op.CurrentHP == op.MaxHP, "Heal did not respect effective maximum HP.");
            typeof(Operator).GetField("battleAttackBonus", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(op, 50);
            Require(op.Attack == (enhanced ? 85 : 70), "Round bonus did not combine with Cache.");
            op.ResetBattleBonus();
            Require(op.Attack == (enhanced ? 35 : 20) && op.MaxHP == (enhanced ? 250 : 100), "Round reset removed Cache bonuses.");
            op.DebugDamage(op.MaxHP);
            Require(op.IsRetreated && op.CurrentHP == 0, "Retreat was changed by Cache.");
            Require(op.ScheduleRedeployment(1) && op.ProcessRedeploymentTurn() && op.CurrentHP == op.MaxHP,
                "Redeployment ignored effective HP.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
