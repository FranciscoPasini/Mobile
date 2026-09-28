using UnityEngine;
using NaughtyAttributes;
using UnityEngine.AI;
using System;

public class Building_Turret : base_TownBuilding
{
    [Header("Data")]
    [SerializeField] private Building_Turret_Data turretData;
    [SerializeField] private Base_Bullet_Data bulletData;

    [Header("State")]
    [SerializeField] private bool purchased = false;
    [Tooltip("0 before purchase, 1 once bought, +1 per upgrade.")]
    [SerializeField] private int level = 0;
    [SerializeField] private int paidTowardNext = 0;

    [Header("Purchase Zone")]
    [Tooltip("Area the player stands in to buy and upgrade. Box, Sphere, Capsule or convex Mesh. Mark it as a trigger so it doesn't block the player.")]
    [SerializeField] private Collider purchaseZone;
    [Tooltip("Seconds the player must stand in the zone before coins start draining.")]
    [SerializeField] private float paymentStartDelay = 0.3f;
    [Tooltip("Roughly how long a full payment takes, whatever the cost.")]
    [SerializeField] private float paymentDuration = 2f;
    [SerializeField] private float minCoinsPerSecond = 5f;
    [Tooltip("After buying or upgrading, the player has to leave the zone before the next upgrade starts charging.")]
    [SerializeField] private bool requireReentryAfterPurchase = true;

    [Header("Combat")]
    [SerializeField] private Transform firePoint;
    [Tooltip("Optional part that rotates to face the target.")]
    [SerializeField] private Transform turretHead;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float targetScanInterval = 0.2f;
    [SerializeField] private AudioClip fireSound;

    [Header("Visuals")]
    [SerializeField] private GameObject unbuiltVisual;
    [SerializeField] private GameObject builtVisual;
    [Tooltip("Optional. Enabled on purchase so enemies path around the turret.")]
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    public event Action<float> OnPaymentProgress;
    public event Action OnPurchased;
    public event Action<int> OnUpgraded;

    private Transform player;
    private float timeInZone;
    private float coinAccumulator;
    private bool waitingForExit;

    private int bulletDamage;
    private float bulletSpeed;
    private float range;
    private float fireRate;

    private float fireTimer;
    private float nextScanTime;
    private Collider currentTarget;
    private readonly Collider[] scanResults = new Collider[32];
    private int nextCost;

    public bool IsPurchased => purchased;
    public int Level => level;
    public bool IsMaxLevel => turretData == null || !turretData.HasLevel(level);
    public int NextCost => IsMaxLevel ? 0 : nextCost;
    public float PaymentProgress => NextCost > 0 ? (float)paidTowardNext / NextCost : 0f;

    private void Start()
    {
        if (purchaseZone == null)
        {
            Debug.LogError($"{name}: no purchase zone collider assigned, the turret can't be bought.", this);
        }
        if (firePoint == null) firePoint = transform;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError($"{name}: no GameObject tagged Player, the purchase zone won't work.", this);
        }

        if (turretData == null)
        {
            Debug.LogError($"{name}: no Building_Turret_Data assigned.", this);
        }

        // Keep the two inspector fields consistent if a turret is pre-placed as bought.
        if (purchased && level == 0) level = 1;
        if (!purchased) level = 0;

        if (purchased) ApplyLevelStats();
        RefreshNextCost();
        RefreshVisuals();
    }

    private void Update()
    {
        HandlePurchaseZone();

        if (purchased)
        {
            HandleCombat();
        }
    }

    #region Purchasing

    private void HandlePurchaseZone()
    {
        if (player == null || purchaseZone == null || IsMaxLevel) return;

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

        int cost = NextCost;
        float coinsPerSecond = Mathf.Max(minCoinsPerSecond, cost / Mathf.Max(paymentDuration, 0.01f));
        coinAccumulator += coinsPerSecond * Time.deltaTime;

        int wanted = Mathf.FloorToInt(coinAccumulator);
        if (wanted <= 0) return;

        // Drain the accumulator every tick so it can't bank up while the player is broke.
        coinAccumulator -= wanted;

        int payment = Mathf.Min(wanted, cost - paidTowardNext, TownManager.Coins);
        if (payment <= 0 || !TownManager.TrySpendCoins(payment)) return;

        paidTowardNext += payment;
        OnPaymentProgress?.Invoke(PaymentProgress);

        if (paidTowardNext >= cost)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        bool firstPurchase = !purchased;

        paidTowardNext = 0;
        coinAccumulator = 0f;
        timeInZone = 0f;
        waitingForExit = requireReentryAfterPurchase;

        level++;
        purchased = true;
        ApplyLevelStats();
        RefreshNextCost();
        RefreshVisuals();

        if (firstPurchase)
        {
            OnPurchased?.Invoke();
        }
        else
        {
            OnUpgraded?.Invoke(level);
        }
    }

    private bool IsPlayerInZone()
    {
        if (!purchaseZone.enabled || !purchaseZone.gameObject.activeInHierarchy) return false;

        // Test at the zone's own height so a flat zone on the ground still catches a player
        // whose pivot sits above or below it. ClosestPoint returns the point itself when it's inside.
        Vector3 point = player.position;
        point.y = purchaseZone.bounds.center.y;
        return (purchaseZone.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
    }

    private void ApplyLevelStats()
    {
        if (turretData == null) return;

        TurretLevel stats = turretData.GetLevel(level - 1);
        if (stats == null) return;

        bulletDamage = stats.bulletDamage;
        bulletSpeed = stats.bulletSpeed;
        range = stats.range;
        fireRate = stats.fireRate;
    }

    private void RefreshNextCost()
    {
        nextCost = IsMaxLevel ? 0 : turretData.GetLevel(level).cost;
    }

    private void RefreshVisuals()
    {
        if (unbuiltVisual != null) unbuiltVisual.SetActive(!purchased);
        if (builtVisual != null) builtVisual.SetActive(purchased);
        if (navMeshObstacle != null) navMeshObstacle.enabled = purchased;
    }

    [Button("Upgrade")]
    private void UpgradeTurret()
    {
        CompleteLevel();
    }

    #endregion

    #region Combat

    private void HandleCombat()
    {
        bool lostTarget = currentTarget != null && (!IsValidTarget(currentTarget) || !IsInRange(currentTarget));
        if (lostTarget)
        {
            currentTarget = null;
        }

        // Rescan on a timer, or straight away when the current target just died or left range.
        if (lostTarget || Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + targetScanInterval;
            currentTarget = FindClosestTarget();
        }

        // Capped so an idle turret doesn't fire the instant something walks into range.
        fireTimer = Mathf.Min(fireTimer + Time.deltaTime, fireRate);

        if (currentTarget == null) return;

        Vector3 aimPoint = currentTarget.bounds.center;
        AimAt(aimPoint);

        if (fireTimer >= fireRate)
        {
            fireTimer = 0f;
            Fire(aimPoint);
        }
    }

    private void Fire(Vector3 aimPoint)
    {
        if (bulletData == null) return;

        // The bullet only reacts to the collider it was aimed at, so pass that collider's GameObject.
        bulletData.Spawn(firePoint.position, aimPoint, currentTarget.gameObject, bulletSpeed, bulletDamage);
        if (fireSound != null) AudioSource.PlayClipAtPoint(fireSound, firePoint.position);
    }

    private Collider FindClosestTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, range, scanResults, enemyLayer, QueryTriggerInteraction.Collide);

        Collider closest = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider candidate = scanResults[i];
            if (!IsValidTarget(candidate)) continue;

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = candidate;
            }
        }

        return closest;
    }

    private bool IsValidTarget(Collider target)
    {
        if (target == null || !target.enabled || !target.gameObject.activeInHierarchy) return false;

        Base_Enemy enemy = target.GetComponentInParent<Base_Enemy>();
        return enemy != null && enemy.IsAlive;
    }

    private bool IsInRange(Collider target)
    {
        return (target.transform.position - transform.position).sqrMagnitude <= range * range;
    }

    private void AimAt(Vector3 point)
    {
        if (turretHead == null) return;

        Vector3 direction = point - turretHead.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        turretHead.rotation = Quaternion.RotateTowards(
            turretHead.rotation,
            Quaternion.LookRotation(direction),
            turnSpeed * Time.deltaTime);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        float previewRange = range;
        if (previewRange <= 0f && turretData != null && turretData.levels != null && turretData.levels.Count > 0)
        {
            previewRange = turretData.levels[0].range;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, previewRange);
    }
}
