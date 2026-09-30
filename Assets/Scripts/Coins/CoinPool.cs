using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
public class CoinWeight
{
    public CoinType coinType;
    [Tooltip("First wave this coin can drop on.")]
    [Min(1)] public int unlockWave = 1;
    [Tooltip("Weight on the unlock wave.")]
    [Min(0f)] public float baseWeight = 10f;
    [Tooltip("Added every wave after unlocking. Negative makes the coin rarer over time.")]
    public float weightPerWave = 0f;
    [Min(0f)] public float minWeight = 0f;
    [Min(0f)] public float maxWeight = 100f;

    public CoinWeight(CoinType type, int unlock, float baseValue, float perWave, float min, float max)
    {
        coinType = type;
        unlockWave = unlock;
        baseWeight = baseValue;
        weightPerWave = perWave;
        minWeight = min;
        maxWeight = max;
    }

    public float GetWeight(int wave)
    {
        if (wave < unlockWave) return 0f;
        float weight = baseWeight + weightPerWave * (wave - unlockWave);
        return Mathf.Clamp(weight, minWeight, Mathf.Max(minWeight, maxWeight));
    }
}

public class CoinPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private Base_Coin coinPrefab;
    [SerializeField] private int poolSize = 40;

    [Header("Dropping")]
    [Tooltip("Height above the enemy's feet the coin rests at.")]
    [SerializeField] private float dropHeightOffset = 0.3f;
    [Tooltip("Random horizontal spread so coins from nearby kills don't stack.")]
    [SerializeField] private float dropScatter = 0.4f;
    [Tooltip("When a drop leaves this many coins or fewer available, the oldest coins on the ground disappear to free up the pool.")]
    [SerializeField] private int lowPoolThreshold = 1;
    [UnityEngine.Serialization.FormerlySerializedAs("autoCollectCount")]
    [SerializeField] private int recycleCount = 5;

    [Header("Pickup Range")]
    [Tooltip("Source of the pickup range upgrades. Found automatically if left empty.")]
    [SerializeField] private Player_ExperienceAndStats playerStats;

    [Header("Coin Weights")]
    [Tooltip("Read for the current wave, so valuable coins get more common as waves go up.")]
    [SerializeField] private TownManager townManager;
    [SerializeField] private List<CoinWeight> coinWeights = new List<CoinWeight>
    {
        new CoinWeight(CoinType.Copper,  1, 70f, -3f,   20f, 70f),
        new CoinWeight(CoinType.Silver,  1, 25f,  1.5f,  0f, 50f),
        new CoinWeight(CoinType.Gold,    1,  5f,  1f,    0f, 35f),
        new CoinWeight(CoinType.Diamond, 5,  2f,  0.75f, 0f, 20f),
    };

    private readonly Queue<Base_Coin> available = new Queue<Base_Coin>();
    // Kept in drop order, so index 0 is always the oldest coin on the map.
    private readonly List<Base_Coin> active = new List<Base_Coin>();
    // Every coin the pool owns, pooled or not, so upgrades reach coins already on the map.
    private readonly List<Base_Coin> allCoins = new List<Base_Coin>();
    private Transform player;

    public int AvailableCount => available.Count;
    public int ActiveCount => active.Count;

    private void Awake()
    {
        if (townManager == null)
        {
            townManager = FindFirstObjectByType<TownManager>();
        }

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: no GameObject tagged Player, coins will be awarded instantly instead of flying.", this);
        }

        if (coinPrefab == null)
        {
            Debug.LogError($"{name}: no coin prefab assigned.", this);
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            Base_Coin coin = Instantiate(coinPrefab, transform);
            coin.gameObject.SetActive(false);
            coin.SetPlayer(player);
            coin.Despawned += ReturnToPool;
            available.Enqueue(coin);
            allCoins.Add(coin);
        }

        ApplyPickupRange();
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.onPlayerStatsUpgraded += ApplyPickupRange;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.onPlayerStatsUpgraded -= ApplyPickupRange;
        }
    }

    private void ApplyPickupRange()
    {
        float multiplier = playerStats != null ? playerStats.GetPickupRangeMultiplier() : 1f;

        for (int i = 0; i < allCoins.Count; i++)
        {
            allCoins[i].SetPickupRangeMultiplier(multiplier);
        }
    }

    /// <summary>
    /// Drops a coin of a wave-weighted random type. Returns null if every coin is still flying
    /// to the player, in which case nothing drops.
    /// </summary>
    public Base_Coin DropCoin(Vector3 position)
    {
        if (available.Count <= lowPoolThreshold)
        {
            RecycleOldest(recycleCount);
        }

        if (available.Count == 0)
        {
            return null;
        }

        CoinType type = RollCoinType(GetWave());

        Vector2 scatter = Random.insideUnitCircle * dropScatter;
        Vector3 dropPosition = position + new Vector3(scatter.x, dropHeightOffset, scatter.y);

        Base_Coin coin = available.Dequeue();
        active.Add(coin);
        coin.SetCoinValue(type);
        coin.Spawn(dropPosition);
        return coin;
    }

    public CoinType RollCoinType(int wave)
    {
        float total = 0f;
        for (int i = 0; i < coinWeights.Count; i++)
        {
            total += coinWeights[i].GetWeight(wave);
        }

        if (total <= 0f) return CoinType.Copper;

        float roll = Random.value * total;
        for (int i = 0; i < coinWeights.Count; i++)
        {
            float weight = coinWeights[i].GetWeight(wave);
            if (roll < weight) return coinWeights[i].coinType;
            roll -= weight;
        }

        // Only reached through float rounding on the last entry.
        return coinWeights[coinWeights.Count - 1].coinType;
    }

    /// <summary>
    /// Pulls coins inside a ground-radius around origin. They fly to flyTarget, or the player if that is empty.
    /// </summary>
    public void CollectInRadius(Vector3 origin, float radius, Transform flyTarget = null)
    {
        if (radius <= 0f || active.Count == 0) return;

        float sqrRadius = radius * radius;
        for (int i = 0; i < active.Count; i++)
        {
            Base_Coin coin = active[i];
            if (coin == null || coin.IsCollecting) continue;

            Vector3 delta = coin.transform.position - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude <= sqrRadius)
            {
                if (flyTarget != null) coin.CollectTo(flyTarget);
                else coin.Collect();
            }
        }
    }

    /// <summary>
    /// Pulls every coin currently on the map to the player, at any distance.
    /// </summary>
    [Button("Collect All Coins")]
    public void CollectAll()
    {
        // Copied first: a coin with no player awards and despawns instantly, editing the list.
        Base_Coin[] snapshot = active.ToArray();
        foreach (Base_Coin coin in snapshot)
        {
            coin.Collect();
        }
    }

    /// <summary>
    /// Removes the oldest coins from the map without awarding them.
    /// </summary>
    private void RecycleOldest(int count)
    {
        // Coins flying to the player were earned, so leave them alone.
        int removed = 0;
        for (int i = 0; i < active.Count && removed < count;)
        {
            if (active[i].IsCollecting)
            {
                i++;
                continue;
            }

            // Despawn removes it from the list, so the next coin slides into index i.
            Base_Coin coin = active[i];
            coin.Despawn();
            if (i < active.Count && active[i] == coin) i++;
            removed++;
        }
    }

    private void ReturnToPool(Base_Coin coin)
    {
        if (!active.Remove(coin)) return;
        available.Enqueue(coin);
    }

    private int GetWave()
    {
        return townManager != null ? Mathf.Max(1, townManager.Wave) : 1;
    }

    [Button("Log Weights Preview")]
    private void LogWeightsPreview()
    {
        int[] waves = { 1, 3, 5, 10, 15, 20, 30 };
        var log = new System.Text.StringBuilder($"{name} coin odds:\n");

        foreach (int wave in waves)
        {
            float total = 0f;
            foreach (CoinWeight entry in coinWeights) total += entry.GetWeight(wave);
            if (total <= 0f) continue;

            float averageValue = 0f;
            log.Append($"Wave {wave}:");
            foreach (CoinWeight entry in coinWeights)
            {
                float chance = entry.GetWeight(wave) / total;
                averageValue += chance * (int)entry.coinType;
                log.Append($"  {entry.coinType} {chance * 100f:0}%");
            }
            log.AppendLine($"  | average value {averageValue:0.0}");
        }

        Debug.Log(log.ToString(), this);
    }
}
