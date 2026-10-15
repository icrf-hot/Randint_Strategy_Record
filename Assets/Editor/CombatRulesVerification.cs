using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CombatRulesVerification
{
    public static void Run()
    {
        List<GameObject> objects = new List<GameObject>();
        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        OperatorData data = ScriptableObject.CreateInstance<OperatorData>();
        try
        {
            Require(BackCardSelectionManager.CalculateHealing(1, 9) == 1000 &&
                BackCardSelectionManager.CalculateHealing(9, 9) == 1800, "Healer formula is incorrect.");
            SerializedObject skillData = new SerializedObject(skill);
            skillData.FindProperty("maxSP").intValue = 100;
            skillData.FindProperty("chargeType").intValue = (int)SkillChargeType.Natural;
            skillData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject source = new SerializedObject(data);
            source.FindProperty("maxHP").intValue = 10000;
            source.FindProperty("skills").arraySize = 1;
            source.FindProperty("skills").GetArrayElementAtIndex(0).objectReferenceValue = skill;
            source.ApplyModifiedPropertiesWithoutUndo();
            BattleExecuteManager manager = Create<BattleExecuteManager>(objects);
            Operator[] allies = new Operator[3];
            string[] fields = { "frontOperator", "middleOperator", "backOperator" };
            for (int i = 0; i < allies.Length; i++)
            {
                allies[i] = Create<Operator>(objects);
                Set(allies[i], "data", data);
                Invoke(allies[i], "Awake");
                Set(manager, fields[i], allies[i]);
            }
            for (int i = 0; i < 3; i++) Invoke(manager, "NotifyAttackPerformed");
            Enemy enemy = Create<Enemy>(objects);
            Invoke(enemy, "Awake");
            for (int i = 0; i < 4; i++) Invoke(manager, "ExecuteEnemyAction", enemy);
            foreach (Operator op in allies)
                Require(op.Skills[0].CurrentSP == 7, "Seven attacks must grant each ally seven natural SP.");
            Invoke(manager, "ExecuteEnemyAction", new object[] { null });
            Require(allies[0].Skills[0].CurrentSP == 7, "Cancelled attack granted SP.");
            allies[0].DebugDamage(int.MaxValue);
            Invoke(manager, "NotifyAttackPerformed");
            Require(allies[0].Skills[0].CurrentSP == 7 && allies[1].Skills[0].CurrentSP == 8,
                "Retreated operator gained SP or active operator did not.");
            allies[1].Skills[0].OnNaturalRecovery(int.MaxValue);
            Require(allies[1].Skills[0].CurrentSP == 100, "Natural SP did not cap safely.");
        }
        finally
        {
            foreach (GameObject obj in objects) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(skill);
        }
        Debug.Log("[Combat Rules Verification] Healer card formula, global natural SP, cancellation, retreat and SP cap passed.");
    }

    private static T Create<T>(List<GameObject> objects) where T : Component
    {
        GameObject obj = new GameObject(typeof(T).Name);
        obj.SetActive(false);
        objects.Add(obj);
        return obj.AddComponent<T>();
    }

    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Invoke(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
