using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
public class OneTimePickupDropWeight
{
    public Base_OneTimePickup_Data pickup;
    [Min(1)] public int unlockWave = 1;
    [Min(0f)] public float baseWeight = 10f;
    public float weightPerWave = 0f;
    [Min(0f)] public float minWeight = 0f;
    [Min(0f)] public float maxWeight = 100f;
    [Tooltip("How many of this prefab to keep in the pool.")]
    [Min(1)] public int poolSize = 4;

    public float GetWeight(int wave)
    {
        if (pickup == null || wave < unlockWave) return 0f;
        float weight = baseWeight + weightPerWave * (wave - unlockWave);
        return Mathf.Clamp(weight, minWeight, Mathf.Max(minWeight, maxWeight));
    }
}

/// <summary>
/// Rolls a drop chance on enemy death, then a weighted one-time pickup.
/// Player upgrade adds up to +15% to the drop chance.
/// </summary>
public class OneTimePickupPool : MonoBehaviour
{
    [Header("Dropping")]
    [Tooltip("Chance an enemy death drops a one-time pickup, before the player upgrade.")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.06f;
    [SerializeField] private float dropHeightOffset = 0.4f;
    [SerializeField] private float dropScatter = 0.35f;
    [SerializeField] private int lowPoolThreshold = 1;
    [SerializeField] private int recycleCount = 2;

    [Header("Pickup Weights")]
    [SerializeField] private TownManager townManager;
    [SerializeField] private Building_Town townBuilding;
    [SerializeField] private List<OneTimePickupDropWeight> pickupWeights = new List<OneTimePickupDropWeight>();

    [Header("Pickup Range")]
    [SerializeField] private Player_ExperienceAndStats playerStats;

    private readonly Dictionary<Base_OneTimePickup_Data, Queue<Base_OneTimePickup>> available
        = new Dictionary<Base_OneTimePickup_Data, Queue<Base_OneTimePickup>>();
    private readonly List<Base_OneTimePickup> active = new List<Base_OneTimePickup>();
    private readonly List<Base_OneTimePickup> allPickups = new List<Base_OneTimePickup>();
    private Transform player;

    public int AvailableCount
    {
        get
        {
            int count = 0;
            foreach (Queue<Base_OneTimePickup> queue in available.Values) count += queue.Count;
            return count;
        }
    }

    public int ActiveCount => active.Count;

    private void Awake()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
        if (townBuilding == null) townBuilding = FindFirstObjectByType<Building_Town>();
        if (playerStats == null) playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        Prewarm();
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
    /// Roll drop chance (including the player bonus) and spawn a weighted pickup.
    /// </summary>
    public Base_OneTimePickup TryDrop(Vector3 position)
    {
        float chance = GetEffectiveDropChance();
        if (chance <= 0f || Random.value > chance) return null;
        return DropPickup(position);
    }

    public Base_OneTimePickup DropPickup(Vector3 position)
    {
        Base_OneTimePickup_Data data = RollPickup(GetWave());
        if (data == null) return null;

        if (!available.TryGetValue(data, out Queue<Base_OneTimePickup> queue)) return null;

        if (queue.Count <= lowPoolThreshold)
        {
            RecycleOldest(recycleCount, data);
        }

        if (queue.Count == 0) return null;

        Vector2 scatter = Random.insideUnitCircle * dropScatter;
        Vector3 dropPosition = position + new Vector3(scatter.x, dropHeightOffset, scatter.y);

        Base_OneTimePickup pickup = queue.Dequeue();
        active.Add(pickup);
        pickup.BindData(data);
        pickup.Spawn(dropPosition);
        return pickup;
    }

    public float GetEffectiveDropChance()
    {
        float bonus = playerStats != null ? playerStats.GetPickupDropChanceBonus() : 0f;
        return Mathf.Clamp01(dropChance + bonus);
    }

    private void Prewarm()
    {
        for (int i = 0; i < pickupWeights.Count; i++)
        {
            OneTimePickupDropWeight entry = pickupWeights[i];
            if (entry == null || entry.pickup == null) continue;
            if (entry.pickup.pickupPrefab == null)
            {
                Debug.LogError($"{name}: {entry.pickup.name} has no pickup prefab assigned.", this);
                continue;
            }

            if (!available.ContainsKey(entry.pickup))
            {
                available[entry.pickup] = new Queue<Base_OneTimePickup>();
            }

            Queue<Base_OneTimePickup> queue = available[entry.pickup];
            int needed = Mathf.Max(1, entry.poolSize) - queue.Count;
            for (int n = 0; n < needed; n++)
            {
                CreateInstance(entry.pickup, queue);
            }
        }
    }

    private void CreateInstance(Base_OneTimePickup_Data data, Queue<Base_OneTimePickup> queue)
    {
        GameObject instance = Instantiate(data.pickupPrefab, transform);
        Base_OneTimePickup pickup = instance.GetComponent<Base_OneTimePickup>()
            ?? instance.GetComponentInChildren<Base_OneTimePickup>();
        if (pickup == null)
        {
            Debug.LogError($"{name}: prefab '{data.pickupPrefab.name}' is missing a Base_OneTimePickup component.", this);
            Destroy(instance);
            return;
        }

        pickup.gameObject.SetActive(false);
        pickup.SetPlayer(player);
        pickup.SetTown(townBuilding);
        pickup.SetPlayerStats(playerStats);
        pickup.BindData(data);
        pickup.Despawned += ReturnToPool;
        queue.Enqueue(pickup);
        allPickups.Add(pickup);
    }

    private Base_OneTimePickup_Data RollPickup(int wave)
    {
        float total = 0f;
        for (int i = 0; i < pickupWeights.Count; i++)
        {
            total += pickupWeights[i].GetWeight(wave);
        }

        if (total <= 0f) return null;

        float roll = Random.value * total;
        for (int i = 0; i < pickupWeights.Count; i++)
        {
            float weight = pickupWeights[i].GetWeight(wave);
            if (roll < weight) return pickupWeights[i].pickup;
            roll -= weight;
        }

        return pickupWeights[pickupWeights.Count - 1].pickup;
    }

    private void RecycleOldest(int count, Base_OneTimePickup_Data preferred)
    {
        int removed = 0;
        for (int i = 0; i < active.Count && removed < count;)
        {
            Base_OneTimePickup candidate = active[i];
            if (candidate.IsCollecting || (preferred != null && candidate.PickupData != preferred))
            {
                i++;
                continue;
            }

            candidate.Despawn();
            if (i < active.Count && active[i] == candidate) i++;
            removed++;
        }
    }

    private void ReturnToPool(Base_OneTimePickup pickup)
    {
        if (!active.Remove(pickup)) return;

        Base_OneTimePickup_Data data = pickup.PickupData;
        if (data != null && available.TryGetValue(data, out Queue<Base_OneTimePickup> queue))
        {
            queue.Enqueue(pickup);
        }
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
        DropPickup(transform.position + Vector3.up);
    }

    [Button("Log Drop Chance")]
    private void LogDropChance()
    {
        Debug.Log($"{name}: base {dropChance * 100f:0.#}%, effective {GetEffectiveDropChance() * 100f:0.#}%", this);
    }
}
