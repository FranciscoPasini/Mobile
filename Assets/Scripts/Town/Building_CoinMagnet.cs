using UnityEngine;

/// <summary>
/// Purchasable magnet. Coins dropped inside its radius fly to the built visual.
/// Range grows linearly for a capped number of upgrades. Pay zone is a separate PurchaseArea.
/// </summary>
public class Building_CoinMagnet : Base_PurchaseableBuilding
{
    [Header("Coin Magnet")]
    [Tooltip("Flat disc that is scaled to the current collection radius.")]
    [SerializeField] private Transform rangeVisual;
    [Tooltip("Coins fly here when sucked in. Defaults to this transform if empty.")]
    [SerializeField] private Transform collectTarget;
    [SerializeField] private CoinPool coinPool;

    private float range = 5f;
    private float rangeVisualHeight = 0.08f;

    private Building_CoinMagnet_Data MagnetData => purchaseData as Building_CoinMagnet_Data;

    protected override void Awake()
    {
        if (coinPool == null) coinPool = FindFirstObjectByType<CoinPool>();
        if (rangeVisual != null) rangeVisualHeight = rangeVisual.localScale.y;
        if (collectTarget == null) collectTarget = transform;

        base.Awake();
        RefreshRange();
    }

    private void Update()
    {
        if (!IsPurchased || range <= 0f) return;

        if (coinPool == null)
        {
            coinPool = FindFirstObjectByType<CoinPool>();
            if (coinPool == null) return;
        }

        coinPool.CollectInRadius(transform.position, range, collectTarget);
    }

    protected override void OnLevelReached(int newLevel, bool firstPurchase)
    {
        base.OnLevelReached(newLevel, firstPurchase);
        RefreshRange();
    }

    private void RefreshRange()
    {
        Building_CoinMagnet_Data data = MagnetData;
        int upgrades = Mathf.Max(0, Level - 1);

        if (data == null)
        {
            range = 5f;
            if (purchaseData != null)
            {
                Debug.LogWarning($"{name}: assign a Building_CoinMagnet_Data asset, not a generic purchasable data.", this);
            }
        }
        else
        {
            range = data.GetRange(upgrades);
        }

        if (rangeVisual != null)
        {
            float diameter = range * 2f;
            rangeVisual.localScale = new Vector3(diameter, rangeVisualHeight, diameter);
        }
    }

    private void OnDrawGizmosSelected()
    {
        float gizmoRange = range;
        if (!Application.isPlaying && MagnetData != null)
        {
            gizmoRange = MagnetData.GetRange(Mathf.Max(0, Level - 1));
        }

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.28f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.05f, gizmoRange);
    }
}
