using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
public class WeaponDropWeight
{
    public Base_Weapon_Data weapon;
    [Min(1)] public int unlockWave = 1;
    [Min(0f)] public float baseWeight = 10f;
    public float weightPerWave = 0f;
    [Min(0f)] public float minWeight = 0f;
    [Min(0f)] public float maxWeight = 100f;

    public float GetWeight(int wave)
    {
        if (weapon == null || wave < unlockWave) return 0f;
        float weight = baseWeight + weightPerWave * (wave - unlockWave);
        return Mathf.Clamp(weight, minWeight, Mathf.Max(minWeight, maxWeight));
    }
}

public class WeaponPickupPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private WeaponPickup pickupPrefab;
    [SerializeField] private int poolSize = 8;

    [Header("Dropping")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.08f;
    [SerializeField] private float dropHeightOffset = 0.4f;
    [SerializeField] private float dropScatter = 0.35f;
    [SerializeField] private int lowPoolThreshold = 1;
    [SerializeField] private int recycleCount = 2;

    [Header("Weapon Weights")]
    [SerializeField] private TownManager townManager;
    [SerializeField] private List<WeaponDropWeight> weaponWeights = new List<WeaponDropWeight>();

    [Header("Pickup Range")]
    [SerializeField] private Player_ExperienceAndStats playerStats;

    private readonly Queue<WeaponPickup> available = new Queue<WeaponPickup>();
    private readonly List<WeaponPickup> active = new List<WeaponPickup>();
    private readonly List<WeaponPickup> allPickups = new List<WeaponPickup>();
    private Transform player;
    private PlayerWeaponSystem weaponSystem;

    public int AvailableCount => available.Count;
    public int ActiveCount => active.Count;

    private void Awake()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
        if (playerStats == null) playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            weaponSystem = playerObject.GetComponent<PlayerWeaponSystem>()
                ?? playerObject.GetComponentInChildren<PlayerWeaponSystem>()
                ?? playerObject.GetComponentInParent<PlayerWeaponSystem>();
        }

        if (weaponSystem == null)
        {
            weaponSystem = FindFirstObjectByType<PlayerWeaponSystem>();
        }

        if (pickupPrefab == null)
        {
            Debug.LogError($"{name}: no weapon pickup prefab assigned.", this);
            return;
        }

        if (weaponWeights.Count == 0)
        {
            Base_Weapon_Data[] loaded = Resources.LoadAll<Base_Weapon_Data>("Weapons");
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] == null || loaded[i].weaponName == "Pistol") continue;
                weaponWeights.Add(new WeaponDropWeight
                {
                    weapon = loaded[i],
                    unlockWave = 1,
                    baseWeight = 10f,
                    maxWeight = 100f
                });
            }
        }

        for (int i = 0; i < poolSize; i++)
        {
            WeaponPickup pickup = Instantiate(pickupPrefab, transform);
            pickup.gameObject.SetActive(false);
            pickup.SetPlayer(player, weaponSystem);
            pickup.Despawned += ReturnToPool;
            available.Enqueue(pickup);
            allPickups.Add(pickup);
        }

        ApplyPickupRange();
    }

    private void OnEnable()
    {
        if (playerStats != null) playerStats.onPlayerStatsUpgraded += ApplyPickupRange;
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.onPlayerStatsUpgraded -= ApplyPickupRange;
    }

    /// <summary>
    /// Rolls the drop chance and, on success, spawns a weighted random weapon pickup.
    /// </summary>
    public WeaponPickup TryDrop(Vector3 position)
    {
        if (dropChance <= 0f || Random.value > dropChance) return null;
        return DropWeapon(position);
    }

    public WeaponPickup DropWeapon(Vector3 position)
    {
        if (available.Count <= lowPoolThreshold)
        {
            RecycleOldest(recycleCount);
        }

        if (available.Count == 0) return null;

        Base_Weapon_Data weapon = RollWeapon(GetWave());
        if (weapon == null) return null;

        Vector2 scatter = Random.insideUnitCircle * dropScatter;
        Vector3 dropPosition = position + new Vector3(scatter.x, dropHeightOffset, scatter.y);

        WeaponPickup pickup = available.Dequeue();
        active.Add(pickup);
        pickup.SetWeapon(weapon);
        pickup.Spawn(dropPosition);
        return pickup;
    }

    private Base_Weapon_Data RollWeapon(int wave)
    {
        float total = 0f;
        for (int i = 0; i < weaponWeights.Count; i++)
        {
            total += weaponWeights[i].GetWeight(wave);
        }

        if (total <= 0f) return null;

        float roll = Random.value * total;
        for (int i = 0; i < weaponWeights.Count; i++)
        {
            float weight = weaponWeights[i].GetWeight(wave);
            if (roll < weight) return weaponWeights[i].weapon;
            roll -= weight;
        }

        return weaponWeights[weaponWeights.Count - 1].weapon;
    }

    private void RecycleOldest(int count)
    {
        int removed = 0;
        for (int i = 0; i < active.Count && removed < count;)
        {
            if (active[i].IsCollecting)
            {
                i++;
                continue;
            }

            WeaponPickup pickup = active[i];
            pickup.Despawn();
            if (i < active.Count && active[i] == pickup) i++;
            removed++;
        }
    }

    private void ReturnToPool(WeaponPickup pickup)
    {
        if (!active.Remove(pickup)) return;
        available.Enqueue(pickup);
    }

    private void ApplyPickupRange()
    {
        float multiplier = playerStats != null ? playerStats.GetPickupRangeMultiplier() : 1f;
        for (int i = 0; i < allPickups.Count; i++)
        {
            allPickups[i].SetPickupRangeMultiplier(multiplier);
        }
    }

    private int GetWave()
    {
        return townManager != null ? Mathf.Max(1, townManager.Wave) : 1;
    }

    [Button("Drop Random Near Origin")]
    private void DebugDrop()
    {
        DropWeapon(transform.position + Vector3.up);
    }
}
