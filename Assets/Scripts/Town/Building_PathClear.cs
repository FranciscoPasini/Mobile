using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Purchasable that opens a path. Put rocks, gates or other blockers in Objects To Disable
/// (on the base) or here. They stay in the way until the player buys this.
/// </summary>
public class Building_PathClear : Base_PurchaseableBuilding
{
    [Header("Path Clear")]
    [Tooltip("Also disabled on first purchase. Same idea as Objects To Disable on the base.")]
    [SerializeField] private List<GameObject> extraBlockers = new List<GameObject>();

    protected override void OnLevelReached(int newLevel, bool firstPurchase)
    {
        base.OnLevelReached(newLevel, firstPurchase);

        if (!firstPurchase) return;

        for (int i = 0; i < extraBlockers.Count; i++)
        {
            if (extraBlockers[i] != null) extraBlockers[i].SetActive(false);
        }
    }
}
