using UnityEngine;
using UnityEngine.AI;

public class Building_Turret : Base_PurchaseableBuilding
{
    [Header("Turret")]
    [SerializeField] private Building_Turret_Data turretData;
    [SerializeField] private Base_Bullet_Data bulletData;

    [Header("Combat")]
    [SerializeField] private Transform firePoint;
    [Tooltip("Optional part that rotates to face the target.")]
    [SerializeField] private Transform turretHead;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float targetScanInterval = 0.2f;
    [SerializeField] private AudioClip fireSound;

    [Header("Pathing")]
    [Tooltip("Optional. Enabled on purchase so enemies path around the turret.")]
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    private int bulletDamage;
    private float bulletSpeed;
    private float range;
    private float fireRate;

    private float fireTimer;
    private float nextScanTime;
    private Collider currentTarget;
    private readonly Collider[] scanResults = new Collider[32];

    protected override void Awake()
    {
        if (firePoint == null) firePoint = transform;

        if (turretData == null)
        {
            Debug.LogError($"{name}: no Building_Turret_Data assigned.", this);
        }

        base.Awake();

        if (navMeshObstacle != null && !IsPurchased)
        {
            navMeshObstacle.enabled = false;
        }
    }

    private void Update()
    {
        if (IsPurchased)
        {
            HandleCombat();
        }
    }

    protected override bool HasPurchasableLevel(int index)
    {
        return turretData != null && turretData.HasLevel(index);
    }

    protected override int GetPurchasableCost(int index)
    {
        if (turretData == null) return 0;
        TurretLevel stats = turretData.GetLevel(index);
        return stats != null ? stats.cost : 0;
    }

    protected override void OnLevelReached(int newLevel, bool firstPurchase)
    {
        base.OnLevelReached(newLevel, firstPurchase);
        ApplyLevelStats();
        if (navMeshObstacle != null) navMeshObstacle.enabled = true;
    }

    private void ApplyLevelStats()
    {
        if (turretData == null) return;

        TurretLevel stats = turretData.GetLevel(Level - 1);
        if (stats == null) return;

        bulletDamage = stats.bulletDamage;
        bulletSpeed = stats.bulletSpeed;
        range = stats.range;
        fireRate = stats.fireRate;
    }

    #region Combat

    private void HandleCombat()
    {
        bool lostTarget = currentTarget != null && (!IsValidTarget(currentTarget) || !IsInRange(currentTarget));
        if (lostTarget)
        {
            currentTarget = null;
        }

        if (lostTarget || Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + targetScanInterval;
            currentTarget = FindClosestTarget();
        }

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
