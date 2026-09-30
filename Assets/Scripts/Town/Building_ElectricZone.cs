using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Purchasable floor hazard. Enemies standing in the volume take damage every pulse.
/// They keep pathing through it. Pay zone is a separate PurchaseArea.
/// </summary>
public class Building_ElectricZone : Base_PurchaseableBuilding
{
    [Header("Electric Zone")]
    [Tooltip("Volume enemies must be inside to get zapped. Box or Sphere, trigger so it doesn't block.")]
    [SerializeField] private Collider zapVolume;
    [SerializeField] private LayerMask enemyMask = ~0;
    [Tooltip("Optional flash enabled for a moment on each pulse.")]
    [SerializeField] private GameObject zapFlash;
    [SerializeField] private float flashDuration = 0.12f;

    private readonly Collider[] hits = new Collider[32];
    private readonly List<Base_Enemy> hitEnemies = new List<Base_Enemy>(16);
    private float pulseTimer;
    private float flashTimer;
    private float damage;
    private float electrifyInterval = 2f;

    private Building_ElectricZone_Data ZoneData => purchaseData as Building_ElectricZone_Data;

    protected override void Awake()
    {
        if (zapVolume == null) zapVolume = GetComponent<Collider>();
        if (zapVolume != null) zapVolume.isTrigger = true;
        if (zapFlash != null) zapFlash.SetActive(false);

        base.Awake();
        RefreshZapStats();
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && zapFlash != null) zapFlash.SetActive(false);
        }

        if (!IsPurchased || zapVolume == null || !zapVolume.enabled) return;

        pulseTimer += Time.deltaTime;
        if (pulseTimer < electrifyInterval) return;

        pulseTimer = 0f;
        Pulse();
    }

    protected override void OnLevelReached(int newLevel, bool firstPurchase)
    {
        base.OnLevelReached(newLevel, firstPurchase);
        RefreshZapStats();
        pulseTimer = 0f;
    }

    private void RefreshZapStats()
    {
        Building_ElectricZone_Data data = ZoneData;
        int upgrades = Mathf.Max(0, Level - 1);

        if (data == null)
        {
            damage = 5f;
            electrifyInterval = 2f;
            if (purchaseData != null)
            {
                Debug.LogWarning($"{name}: assign a Building_ElectricZone_Data asset, not a generic purchasable data.", this);
            }
            return;
        }

        damage = data.GetDamage(upgrades);
        electrifyInterval = data.GetElectrifyInterval(upgrades);
    }

    private void Pulse()
    {
        int count = OverlapZapVolume();
        hitEnemies.Clear();

        for (int i = 0; i < count; i++)
        {
            Base_Enemy enemy = hits[i] != null ? hits[i].GetComponentInParent<Base_Enemy>() : null;
            if (enemy == null || !enemy.IsAlive || hitEnemies.Contains(enemy)) continue;
            hitEnemies.Add(enemy);
        }

        if (hitEnemies.Count == 0) return;

        for (int i = 0; i < hitEnemies.Count; i++)
        {
            hitEnemies[i].TakeDamage(damage);
        }

        if (zapFlash != null)
        {
            zapFlash.SetActive(true);
            flashTimer = flashDuration;
        }
    }

    private int OverlapZapVolume()
    {
        QueryTriggerInteraction query = QueryTriggerInteraction.Collide;

        if (zapVolume is BoxCollider box)
        {
            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            return Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, hits, box.transform.rotation, enemyMask, query);
        }

        if (zapVolume is SphereCollider sphere)
        {
            Vector3 scale = sphere.transform.lossyScale;
            float radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return Physics.OverlapSphereNonAlloc(sphere.transform.TransformPoint(sphere.center), radius, hits, enemyMask, query);
        }

        Bounds bounds = zapVolume.bounds;
        return Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, hits, Quaternion.identity, enemyMask, query);
    }

    private void OnDrawGizmosSelected()
    {
        Collider volume = zapVolume != null ? zapVolume : GetComponent<Collider>();
        if (volume == null) return;

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
        Gizmos.matrix = volume.transform.localToWorldMatrix;

        if (volume is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (volume is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}
