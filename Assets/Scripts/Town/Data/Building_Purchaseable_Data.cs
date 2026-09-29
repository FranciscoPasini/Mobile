using UnityEngine;
using System;
using System.Collections.Generic;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "New Purchasable Building", menuName = "Purchasable Building")]
public class Building_Purchaseable_Data : ScriptableObject
{
    [Tooltip("Entry 0 is the purchase. Every entry after that is one upgrade.")]
    public List<int> costs = new List<int> { 50 };

    [Tooltip("Keep charging past the last cost, growing with the multiplier below.")]
    public bool infiniteLevels = false;

    [ShowIf(nameof(infiniteLevels))]
    [Min(1f)] public float costGrowth = 1.15f;

    public bool HasLevel(int index)
    {
        if (costs == null || costs.Count == 0 || index < 0) return false;
        return infiniteLevels || index < costs.Count;
    }

    public int GetCost(int index)
    {
        if (costs == null || costs.Count == 0) return 0;

        int last = costs.Count - 1;
        if (index <= last) return Mathf.Max(0, costs[Mathf.Max(index, 0)]);
        if (!infiniteLevels) return costs[last];

        double cost = Math.Max(costs[last], 1) * Math.Pow(costGrowth, index - last);
        return (int)Math.Round(Math.Min(cost, int.MaxValue));
    }

    [Button("Log Cost Preview")]
    private void LogCostPreview()
    {
        if (costs == null || costs.Count == 0)
        {
            Debug.LogWarning($"{name}: no costs to preview.", this);
            return;
        }

        int count = infiniteLevels ? costs.Count + 20 : costs.Count;
        var log = new System.Text.StringBuilder($"{name} costs:\n");
        for (int i = 0; i < count; i++)
        {
            log.AppendLine($"Lv {i + 1}: {GetCost(i)}");
        }
        Debug.Log(log.ToString(), this);
    }
}
