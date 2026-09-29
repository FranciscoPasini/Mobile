using UnityEngine;
using NaughtyAttributes;
using System;
using System.Collections.Generic;

/// <summary>
/// Buyable / upgradable town piece whose pay zone lives on a separate PurchaseArea object.
/// Turrets keep their own built-in zone and do not use this.
/// </summary>
public class Base_PurchaseableBuilding : base_TownBuilding, IPurchasable
{
    [Header("Data")]
    [SerializeField] private Building_Purchaseable_Data purchaseData;

    [Header("State")]
    [SerializeField] private bool purchased;
    [Tooltip("0 before purchase, 1 once bought, +1 per upgrade.")]
    [SerializeField] private int level;
    [SerializeField] private int paidTowardNext;

    [Header("Default Purchase Effects")]
    [Tooltip("Turned on the first time this is bought. Children can do extra work in OnLevelReached.")]
    [SerializeField] private List<GameObject> objectsToEnable = new List<GameObject>();
    [Tooltip("Turned off the first time this is bought. Use this to clear a path.")]
    [SerializeField] private List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("Visuals")]
    [SerializeField] private GameObject unbuiltVisual;
    [SerializeField] private GameObject builtVisual;

    public event Action<float> OnPaymentProgress;
    public event Action OnPurchased;
    public event Action<int> OnUpgraded;

    private int nextCost;

    public bool IsPurchased => purchased;
    public int Level => level;
    public bool IsMaxLevel => purchaseData == null || !purchaseData.HasLevel(level);
    public int NextCost => IsMaxLevel ? 0 : nextCost;
    public int RemainingCost => IsMaxLevel ? 0 : Mathf.Max(0, nextCost - paidTowardNext);
    public float PaymentProgress => NextCost > 0 ? (float)paidTowardNext / NextCost : 0f;

    protected virtual void Awake()
    {
        if (purchased && level == 0) level = 1;
        if (!purchased) level = 0;

        RefreshNextCost();
        RefreshVisuals();

        if (purchased)
        {
            OnLevelReached(level, true);
        }
        else
        {
            ApplyDefaultObjectToggles(false);
        }
    }

    public int ApplyPayment(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return 0;

        int room = NextCost - paidTowardNext;
        int taken = Mathf.Min(amount, room);
        if (taken <= 0) return 0;

        paidTowardNext += taken;
        OnPaymentProgress?.Invoke(PaymentProgress);

        if (paidTowardNext >= NextCost)
        {
            CompleteLevel();
        }

        return taken;
    }

    [Button("Complete Next Level")]
    private void DebugCompleteLevel() => CompleteLevel();

    private void CompleteLevel()
    {
        bool firstPurchase = !purchased;

        paidTowardNext = 0;
        level++;
        purchased = true;
        RefreshNextCost();
        RefreshVisuals();
        OnLevelReached(level, firstPurchase);

        if (firstPurchase)
        {
            OnPurchased?.Invoke();
        }
        else
        {
            OnUpgraded?.Invoke(level);
        }
    }

    private void RefreshNextCost()
    {
        nextCost = IsMaxLevel || purchaseData == null ? 0 : purchaseData.GetCost(level);
    }

    private void RefreshVisuals()
    {
        if (unbuiltVisual != null) unbuiltVisual.SetActive(!purchased);
        if (builtVisual != null) builtVisual.SetActive(purchased);
    }

    /// <summary>
    /// Called after a successful buy or upgrade. Override in children for extra behaviour
    /// (enable a NavMeshObstacle, disable blockers, spawn props, and so on).
    /// Default: enable <see cref="objectsToEnable"/> and disable <see cref="objectsToDisable"/> on first purchase.
    /// </summary>
    protected virtual void OnLevelReached(int newLevel, bool firstPurchase)
    {
        if (firstPurchase)
        {
            ApplyDefaultObjectToggles(true);
        }
    }

    private void ApplyDefaultObjectToggles(bool bought)
    {
        SetActive(objectsToEnable, bought);
        SetActive(objectsToDisable, !bought);
    }

    private static void SetActive(List<GameObject> objects, bool active)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null) objects[i].SetActive(active);
        }
    }
}
