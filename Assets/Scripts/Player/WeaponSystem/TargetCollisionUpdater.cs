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
        CleanupDestroyedTargets();
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
            float distanceSqr = (target.transform.position - origin).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = target;
            }
        }

        if (currentTarget == closestTarget)
        {
            if (currentTarget != null)
            {
                playerWeaponSystem.UpdateTarget(currentTarget);
            }

            return;
        }

        currentTarget = closestTarget;
        playerWeaponSystem.UpdateTarget(currentTarget);
    }

    private void CleanupDestroyedTargets()
    {
        for (int i = potentialTargets.Count - 1; i >= 0; i--)
        {
            if (potentialTargets[i] == null)
            {
                potentialTargets.RemoveAt(i);
            }
        }

        if (currentTarget == null)
        {
            currentTarget = null;
        }
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
