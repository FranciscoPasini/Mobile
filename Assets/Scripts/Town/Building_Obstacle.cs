using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Purchasable that blocks enemy pathing. Put wall meshes in Objects To Enable
/// and optional NavMeshObstacles here so they start carving after the buy.
/// </summary>
public class Building_Obstacle : Base_PurchaseableBuilding
{
    [Header("Obstacle")]
    [Tooltip("Enabled on first purchase so enemies path around the structure.")]
    [SerializeField] private List<NavMeshObstacle> obstacles = new List<NavMeshObstacle>();

    protected override void OnLevelReached(int newLevel, bool firstPurchase)
    {
        base.OnLevelReached(newLevel, firstPurchase);

        if (!firstPurchase) return;

        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i] != null) obstacles[i].enabled = true;
        }
    }

    protected override void Awake()
    {
        if (!IsPurchased)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i] != null) obstacles[i].enabled = false;
            }
        }

        base.Awake();
    }
}
