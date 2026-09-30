using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

/// <summary>
/// Standalone pay zone. Link it to a Base_PurchaseableBuilding. With no target, the zone disables itself.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PurchaseArea : MonoBehaviour
{
    [Tooltip("Building this zone buys. Must implement IPurchasable. The area disables if this is empty or not purchasable.")]
    [SerializeField] private Base_PurchaseableBuilding purchasable;
    [Tooltip("Found on this object if left empty.")]
    [SerializeField] private Collider purchaseZone;
    [Tooltip("Seconds the player must stand in the zone before coins start draining.")]
    [SerializeField] private float paymentStartDelay = 0.3f;
    [Tooltip("Roughly how long a full payment takes, whatever the cost.")]
    [SerializeField] private float paymentDuration = 2f;
    [SerializeField] private float minCoinsPerSecond = 5f;
    [Tooltip("After buying or upgrading, the player has to leave the zone before the next upgrade starts charging.")]
    [SerializeField] private bool requireReentryAfterPurchase = true;

    [Header("After Purchase")]
    [Tooltip("Size of this zone after the first buy, as a fraction of the original, if the building still has upgrades. 1 keeps the same size.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float scaleAfterPurchase = 0.7f;

    [Header("Unlock Other Areas")]
    [Tooltip("These zones stay off until this building is bought, then they turn on. Use this for a door after a wall, a second turret after the first, and so on.")]
    [SerializeField] private List<PurchaseArea> areasToUnlockOnPurchase = new List<PurchaseArea>();
    [Tooltip("If on, listed areas are hidden at start until this one is bought. Turn off if you already disabled them in the scene.")]
    [SerializeField] private bool hideUnlockAreasUntilPurchased = true;
    [Tooltip("Hides this zone in Awake. Use this on a later zone in a chain so it still hides even if the zone that would hide it is already off.")]
    [SerializeField] private bool hideOnSpawn;

    private Transform player;
    private float timeInZone;
    private float coinAccumulator;
    private bool waitingForExit;
    private IPurchasable target;
    private bool hidingAsLocked;
    private Vector3 unpurchasedScale;
    private bool capturedUnpurchasedScale;

    public IPurchasable Target => target;
    public float PaymentProgress => target != null ? target.PaymentProgress : 0f;
    public int RemainingCost => target != null ? target.RemainingCost : 0;
    public bool IsMaxLevel => target == null || target.IsMaxLevel;

    public event System.Action<float> OnPaymentProgress;
    public event System.Action OnPurchased;
    public event System.Action<int> OnUpgraded;

    private void Awake()
    {
        if (purchaseZone == null) purchaseZone = GetComponent<Collider>();
        if (purchaseZone != null) purchaseZone.isTrigger = true;

        BindTarget(purchasable);
        CaptureUnpurchasedScale();

        if (target == null)
        {
            Debug.LogWarning($"{name}: no purchasable building linked, disabling the purchase area.", this);
            gameObject.SetActive(false);
            return;
        }

        EnsureCostLabel();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError($"{name}: no GameObject tagged Player, the purchase zone won't work.", this);
        }

        if (hideOnSpawn && (target == null || !target.IsPurchased))
        {
            HideAsLocked();
        }
    }

    private void Start()
    {
        if (target == null) return;

        if (target.IsPurchased)
        {
            ActivateUnlockAreas();
            ApplyPurchaseScale();
        }
        else if (hideUnlockAreasUntilPurchased)
        {
            HideUnlockAreas();
        }
    }

    private void OnEnable()
    {
        Subscribe(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    private void OnValidate()
    {
        if (purchaseZone == null) purchaseZone = GetComponent<Collider>();
    }

    private void Update()
    {
        HandlePurchaseZone();
    }

    public void SetPurchasable(Base_PurchaseableBuilding building)
    {
        Subscribe(false);
        purchasable = building;
        BindTarget(building);
        Subscribe(true);

        if (target == null)
        {
            gameObject.SetActive(false);
        }
        else if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void BindTarget(Base_PurchaseableBuilding building)
    {
        target = building;
    }

    private void Subscribe(bool subscribe)
    {
        if (target == null) return;

        if (subscribe)
        {
            target.OnPaymentProgress += HandleProgress;
            target.OnPurchased += HandlePurchased;
            target.OnUpgraded += HandleUpgraded;
        }
        else
        {
            target.OnPaymentProgress -= HandleProgress;
            target.OnPurchased -= HandlePurchased;
            target.OnUpgraded -= HandleUpgraded;
        }
    }

    private void HandlePurchaseZone()
    {
        if (player == null || purchaseZone == null || target == null) return;

        if (target.IsMaxLevel)
        {
            DisableArea();
            return;
        }

        if (!IsPlayerInZone())
        {
            timeInZone = 0f;
            coinAccumulator = 0f;
            waitingForExit = false;
            return;
        }

        if (waitingForExit) return;

        timeInZone += Time.deltaTime;
        if (timeInZone < paymentStartDelay) return;

        int cost = target.NextCost;
        if (cost <= 0) return;

        float coinsPerSecond = Mathf.Max(minCoinsPerSecond, cost / Mathf.Max(paymentDuration, 0.01f));
        coinAccumulator += coinsPerSecond * Time.deltaTime;

        int wanted = Mathf.FloorToInt(coinAccumulator);
        if (wanted <= 0) return;

        coinAccumulator -= wanted;

        int remaining = target.RemainingCost;
        int payment = Mathf.Min(wanted, remaining, TownManager.Coins);
        if (payment <= 0 || !TownManager.TrySpendCoins(payment)) return;

        target.ApplyPayment(payment);
    }

    private bool IsPlayerInZone()
    {
        if (!purchaseZone.enabled || !purchaseZone.gameObject.activeInHierarchy) return false;

        Vector3 point = player.position;
        point.y = purchaseZone.bounds.center.y;
        return (purchaseZone.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
    }

    private void HandleProgress(float progress)
    {
        OnPaymentProgress?.Invoke(progress);
    }

    private void HandlePurchased()
    {
        ResetAfterPurchase();
        ActivateUnlockAreas();
        ApplyPurchaseScale();
        OnPurchased?.Invoke();
    }

    private void CaptureUnpurchasedScale()
    {
        if (capturedUnpurchasedScale) return;

        unpurchasedScale = transform.localScale;
        capturedUnpurchasedScale = true;
    }

    private void ApplyPurchaseScale()
    {
        if (target == null || !target.IsPurchased || target.IsMaxLevel) return;
        if (scaleAfterPurchase >= 1f) return;

        CaptureUnpurchasedScale();
        transform.localScale = unpurchasedScale * scaleAfterPurchase;
        RefreshCostLabelScale();
    }

    private void RefreshCostLabelScale()
    {
        Transform labelTransform = transform.Find("CostLabel");
        if (labelTransform == null) return;

        Vector3 parentScale = transform.lossyScale;
        labelTransform.localScale = new Vector3(
            1f / Mathf.Max(0.01f, parentScale.x),
            1f / Mathf.Max(0.01f, parentScale.y),
            1f / Mathf.Max(0.01f, parentScale.z));
    }

    private void HandleUpgraded(int newLevel)
    {
        ResetAfterPurchase();
        OnUpgraded?.Invoke(newLevel);
    }

    private void ResetAfterPurchase()
    {
        coinAccumulator = 0f;
        timeInZone = 0f;
        waitingForExit = requireReentryAfterPurchase;
    }

    private void HideUnlockAreas()
    {
        for (int i = 0; i < areasToUnlockOnPurchase.Count; i++)
        {
            PurchaseArea area = areasToUnlockOnPurchase[i];
            if (area == null || area == this) continue;
            area.HideAsLocked();
        }
    }

    /// <summary>
    /// Hides this zone and any zones it would unlock, while this object is still active.
    /// </summary>
    public void HideAsLocked()
    {
        if (hidingAsLocked) return;

        hidingAsLocked = true;
        HideUnlockAreas();
        gameObject.SetActive(false);
        hidingAsLocked = false;
    }

    private void ActivateUnlockAreas()
    {
        for (int i = 0; i < areasToUnlockOnPurchase.Count; i++)
        {
            PurchaseArea area = areasToUnlockOnPurchase[i];
            if (area == null || area == this) continue;
            area.Unlock();
        }
    }

    /// <summary>
    /// Turns this zone on, used when a prerequisite purchase area is bought.
    /// </summary>
    public void Unlock()
    {
        hideOnSpawn = false;
        gameObject.SetActive(true);
        enabled = true;
        if (purchaseZone != null) purchaseZone.enabled = true;

        BuyZoneVisual visual = GetComponent<BuyZoneVisual>();
        if (visual != null)
        {
            visual.enabled = true;
            Renderer zoneRenderer = visual.GetComponent<Renderer>();
            if (zoneRenderer != null) zoneRenderer.enabled = true;
        }

        PurchaseCostLabel costLabel = GetComponent<PurchaseCostLabel>();
        if (costLabel != null) costLabel.enabled = true;
    }

    private void EnsureCostLabel()
    {
        if (GetComponent<PurchaseCostLabel>() == null)
        {
            gameObject.AddComponent<PurchaseCostLabel>();
        }
    }

    private void DisableArea()
    {
        if (purchaseZone != null) purchaseZone.enabled = false;
        enabled = false;
    }

    [Button("Log Linked Cost")]
    private void LogLinkedCost()
    {
        if (purchasable == null)
        {
            Debug.Log($"{name}: no purchasable linked.", this);
            return;
        }

        Debug.Log($"{name} -> {purchasable.name}: level {purchasable.Level}, next {purchasable.NextCost}, max {purchasable.IsMaxLevel}", this);
    }
}
