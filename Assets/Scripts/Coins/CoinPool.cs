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
    [Tooltip("When a drop leaves this many coins or fewer available, the oldest coins fly to the player to free up the pool.")]
    [SerializeField] private int lowPoolThreshold = 1;
    [SerializeField] private int autoCollectCount = 5;

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
    private Transform player;

    public int AvailableCount => available.Count;
    public int ActiveCount => active.Count;

    private void Awake()
    {
        if (townManager == null)
        {
            townManager = FindFirstObjectByType<TownManager>();
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
        }
    }

    /// <summary>
    /// Drops a coin of a wave-weighted random type. Returns null if the pool was empty,
    /// in which case the value is awarded straight away so the kill isn't wasted.
    /// </summary>
    public Base_Coin DropCoin(Vector3 position)
    {
        if (available.Count <= lowPoolThreshold)
        {
            CollectOldest(autoCollectCount);
        }

        CoinType type = RollCoinType(GetWave());

        if (available.Count == 0)
        {
            TownManager.AddCoins((int)type);
            return null;
        }

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

    [Button("Collect All Coins")]
    public void CollectAll()
    {
        CollectOldest(active.Count);
    }

    private void CollectOldest(int count)
    {
        // Coins already flying still count as active until they land, so skip them.
        int started = 0;
        for (int i = 0; i < active.Count && started < count; i++)
        {
            if (active[i].IsCollecting) continue;

            active[i].Collect();
            started++;
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
