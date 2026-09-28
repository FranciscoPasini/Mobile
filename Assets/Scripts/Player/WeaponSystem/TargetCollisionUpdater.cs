using System.Collections.Generic;
using UnityEngine;

public class TargetCollisionUpdater : MonoBehaviour
{
    [SerializeField] private PlayerWeaponSystem playerWeaponSystem;
    [SerializeField] private SphereCollider attackRangeSphere;
    [SerializeField] private LayerMask enemyLayer;

    private readonly List<GameObject> potentialTargets = new List<GameObject>();
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
    }

    void Start()
    {
        UpdateAttackRange();
    }

    void Update()
    {
        CleanupInvalidTargets();
        UpdateClosestTarget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsEnemy(other))
        {
            return;
        }

        if (!potentialTargets.Contains(other.gameObject))
        {
            potentialTargets.Add(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Pooled enemies can be reactivated inside the sphere without raising OnTriggerEnter,
        // so re-register anything that is overlapping and valid again.
        if (!IsEnemy(other) || !IsValidTarget(other.gameObject))
        {
            return;
        }

        if (!potentialTargets.Contains(other.gameObject))
        {
            potentialTargets.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsEnemy(other))
        {
            return;
        }

        potentialTargets.Remove(other.gameObject);

        if (currentTarget == other.gameObject)
        {
            currentTarget = null;
        }
    }

    private void UpdateClosestTarget()
    {
        if (playerWeaponSystem == null)
        {
            return;
        }

        GameObject closestTarget = null;
        float closestDistanceSqr = float.MaxValue;
        Vector3 origin = transform.position;

        for (int i = 0; i < potentialTargets.Count; i++)
        {
            GameObject target = potentialTargets[i];
            if (!IsValidTarget(target))
            {
                continue;
            }

            float distanceSqr = (target.transform.position - origin).sqrMagnitude;

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

    private void CleanupInvalidTargets()
    {
        // Pooled enemies are deactivated rather than destroyed, so a null check alone
        // would leave dead enemies in the list and keep the weapon locked onto them.
        for (int i = potentialTargets.Count - 1; i >= 0; i--)
        {
            if (!IsValidTarget(potentialTargets[i]))
            {
                potentialTargets.RemoveAt(i);
            }
        }

        if (!IsValidTarget(currentTarget))
        {
            currentTarget = null;
        }
    }

    private bool IsValidTarget(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
        {
            return false;
        }

        // Enemy component may live on a parent of the collider.
        Base_Enemy enemy = target.GetComponent<Base_Enemy>()
            ?? target.GetComponentInParent<Base_Enemy>();

        return enemy == null || enemy.IsAlive;
    }

    private bool IsEnemy(Collider other)
    {
        return ((1 << other.gameObject.layer) & enemyLayer) != 0;
    }

    private void UpdateAttackRange()
    {
        if (attackRangeSphere == null || playerWeaponSystem == null)
        {
            return;
        }

        Base_Weapon activeWeapon = playerWeaponSystem.GetActiveWeapon();
        if (activeWeapon == null)
        {
            return;
        }

        attackRangeSphere.radius = activeWeapon.GetWeaponCalculatedBaseRange();
    }
}
