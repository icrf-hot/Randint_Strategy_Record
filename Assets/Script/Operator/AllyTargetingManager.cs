using System.Collections.Generic;
using UnityEngine;

public enum HealPriorityType
{
    SelfPriority,       // 자신 우선
    UniversalPriority   // 전체 HP가 낮은 아군 우선
}

public class AllyTargetingManager : MonoBehaviour
{
    public static AllyTargetingManager Instance { get; private set; }

    [Header("Heal Setting")]
    [SerializeField] private int maxHealTargets = 2;

    [SerializeField]
    private HealPriorityType healPriority =
        HealPriorityType.UniversalPriority;

    [Header("Self Heal")]
    [SerializeField]
    [Range(0f, 1f)]
    private float selfHealThreshold = 0.5f;

    private void Awake()
    {
        Instance = this;
    }

    public List<Operator> GetHealTargets(
        Operator healer)
    {
        List<Operator> result =
            new List<Operator>();

        if (healer == null)
            return result;

        Operator[] operators =
            FindObjectsByType<Operator>(
                FindObjectsSortMode.None);

        List<Operator> candidates =
            new List<Operator>();

        foreach (Operator op in operators)
        {
            if (op == null)
                continue;

            if (op == healer)
                continue;

            if (op.CurrentHP <= 0)
                continue;

            if (op.CurrentHP >= op.MaxHP)
                continue;

            candidates.Add(op);
        }

        // HP가 낮은 순
        candidates.Sort(
            (a, b) =>
                a.HPPercent.CompareTo(b.HPPercent));

        // =================================================
        // 자신 우선
        // =================================================

        if (healPriority ==
            HealPriorityType.SelfPriority)
        {
            if (healer.HPPercent <=
                selfHealThreshold)
            {
                result.Add(healer);
            }
        }

        // =================================================
        // 나머지 힐 대상
        // =================================================

        foreach (Operator target in candidates)
        {
            if (result.Count >= maxHealTargets)
                break;

            result.Add(target);
        }

        LogHealPriority(healer, result);

        return result;
    }

    private void LogHealPriority(
        Operator healer,
        List<Operator> targets)
    {
        Debug.Log(
            $"========== 힐 대상 ==========\n" +
            $"힐러 : {healer.name}\n" +
            $"최대 힐 인원 : {maxHealTargets}");

        if (targets.Count == 0)
        {
            Debug.Log("힐 대상 없음");
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Operator target = targets[i];

            Debug.Log(
                $"{i + 1}순위 : " +
                $"{target.name} " +
                $"HP {target.CurrentHP}/{target.MaxHP} " +
                $"({target.HPPercent * 100f:F1}%)");
        }

        Debug.Log("==============================");
    }
}