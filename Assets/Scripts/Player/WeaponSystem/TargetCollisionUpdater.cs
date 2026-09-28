using UnityEngine;

public class TargetCollisionUpdater : MonoBehaviour
{
    [SerializeField] private PlayerWeaponSystem playerWeaponSystem;
    [SerializeField] private SphereCollider attackRangeSphere;
    [SerializeField] private LayerMask enemyLayer;

    // Trigger callbacks are not raised between a sleeping dynamic Rigidbody (the player)
    // and kinematic ones moved by NavMeshAgent (enemies), so targets are found by querying.
    private readonly Collider[] overlapResults = new Collider[32];
    private GameObject currentTarget;

    void Awake()
    {
        if (playerWeaponSystem == null)
        {
            playerWeaponSystem = GetComponentInParent<PlayerWeaponSystem>();
        }

        if (attackRangeSphere == null)
        {
            attackRangeSphere = GetComponent<SphereCollider>();
        }
    }

    private void OnEnable()
    {
        if (playerWeaponSystem != null)
        {
            playerWeaponSystem.OnActiveWeaponChanged += UpdateAttackRange;
        }
    }

    private void OnDisable()
    {
        if (playerWeaponSystem != null)
        {
            playerWeaponSystem.OnActiveWeaponChanged -= UpdateAttackRange;
        }

        currentTarget = null;
    }

    void Start()
    {
        UpdateAttackRange();
    }

    void Update()
    {
        UpdateClosestTarget();
    }

    private void UpdateClosestTarget()
    {
        if (playerWeaponSystem == null)
        {
            return;
        }

        GetRangeSphere(out Vector3 center, out float radius);
        int hitCount = Physics.OverlapSphereNonAlloc(
            center, radius, overlapResults, enemyLayer, QueryTriggerInteraction.Ignore);

        GameObject closestTarget = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            GameObject target = overlapResults[i].gameObject;
            if (!IsValidTarget(target))
            {
                continue;
            }

            float distanceSqr = (target.transform.position - center).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = target;
            }
        }

        // Always push the result, including null, so the weapon stops firing once
        // the current target dies and no replacement is in range.
        currentTarget = closestTarget;
        playerWeaponSystem.UpdateTarget(currentTarget);
    }

    private void GetRangeSphere(out Vector3 center, out float radius)
    {
        if (attackRangeSphere == null)
        {
            center = transform.position;
            radius = GetWeaponRange();
            return;
        }

        Transform sphereTransform = attackRangeSphere.transform;
        Vector3 scale = sphereTransform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        center = sphereTransform.TransformPoint(attackRangeSphere.center);
        radius = attackRangeSphere.radius * maxScale;
    }

    private float GetWeaponRange()
    {
        Base_Weapon activeWeapon = playerWeaponSystem != null ? playerWeaponSystem.GetActiveWeapon() : null;
        return activeWeapon != null ? activeWeapon.GetWeaponCalculatedBaseRange() : 0f;
    }

    private bool IsValidTarget(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
        {
            return false;
        }

        // Pooled enemies are deactivated rather than destroyed, and the Enemy component
        // may live on a parent of the collider.
        Base_Enemy enemy = target.GetComponentInParent<Base_Enemy>();
        return enemy == null || enemy.IsAlive;
    }

    private void UpdateAttackRange()
    {
        if (attackRangeSphere == null)
        {
            return;
        }

        float range = GetWeaponRange();
        if (range > 0f)
        {
            attackRangeSphere.radius = range;
        }
    }
}
