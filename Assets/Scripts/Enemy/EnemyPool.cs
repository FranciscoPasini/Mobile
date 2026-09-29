using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
public class EnemySpawnEntry
{
    public Base_Enemy prefab;
    [Tooltip("Relative chance this type is picked. 0 never spawns.")]
    [Min(0f)] public float spawnWeight = 10f;
    [Tooltip("First wave this type can appear on.")]
    [Min(1)] public int unlockWave = 1;
    [Tooltip("How many of this type are created up front.")]
    [Min(0)] public int initialSize = 5;

    [System.NonSerialized] public Queue<Base_Enemy> available;
    [System.NonSerialized] public int created;
}

public class EnemyPool : MonoBehaviour
{
    [Header("Pool")]
    [Tooltip("Fallback if the spawn list is empty. Existing scenes keep working.")]
    [SerializeField] private Base_Enemy enemyPrefab;
    [SerializeField] private int initialSize = 10;
    [Tooltip("Allow the pool to instantiate extra enemies when all pooled ones are in use.")]
    [SerializeField] private bool canGrow = true;
    [Tooltip("Hard ceiling on how many enemies can exist, even when Can Grow is on.")]
    [SerializeField] private int maxSize = 30;

    [Header("Enemy Types")]
    [Tooltip("Weighted list of enemy prefabs. Each prefab keeps its own stat curves.")]
    [SerializeField] private List<EnemySpawnEntry> enemyTypes = new List<EnemySpawnEntry>();

    [Header("Difficulty")]
    [Tooltip("Waves between each difficulty step. 1 = every wave, 2 = every other wave, and so on.")]
    [SerializeField, Min(1)] private int wavesPerDifficultyIncrease = 2;
    [Tooltip("Read for the current wave. Found in the scene if left empty.")]
    [SerializeField] private TownManager townManager;

    [Header("Spawn Areas")]
    [Tooltip("Colliders defining where enemies can spawn. Box, Sphere, Capsule or convex Mesh colliders.")]
    [SerializeField] private List<Collider> spawnAreas = new List<Collider>();
    [SerializeField] private int maxSpawnAttempts = 30;
    [Tooltip("How far a sampled NavMesh point may be from the collider and still count as inside it.")]
    [SerializeField] private float insideTolerance = 0.5f;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

    [Header("Auto Spawn")]
    [SerializeField] private int spawnOnStart = 0;

    private readonly List<Base_Enemy> active = new List<Base_Enemy>();
    private readonly Dictionary<Base_Enemy, EnemySpawnEntry> owners = new Dictionary<Base_Enemy, EnemySpawnEntry>();
    private Transform inactiveContainer;
    private int totalCreated;

    public IReadOnlyList<Base_Enemy> ActiveEnemies => active;
    public int ActiveCount => active.Count;
    public int AvailableCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < enemyTypes.Count; i++) count += enemyTypes[i].available.Count;
            return count;
        }
    }
    public int TotalCreated => totalCreated;
    public int DifficultyLevel => GetDifficultyLevel();

    private void Awake()
    {
        maxSize = Mathf.Max(maxSize, initialSize);

        if (townManager == null)
        {
            townManager = FindFirstObjectByType<TownManager>();
        }

        EnsureFallbackType();

        // Instances are created under an inactive parent so their Awake (and NavMeshAgent)
        // doesn't run until they are placed on the NavMesh.
        inactiveContainer = new GameObject("InactiveEnemies").transform;
        inactiveContainer.SetParent(transform, false);
        inactiveContainer.gameObject.SetActive(false);

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry == null || entry.prefab == null) continue;
            if (entry.available == null) entry.available = new Queue<Base_Enemy>();

            int count = Mathf.Min(entry.initialSize, Mathf.Max(0, maxSize - totalCreated));
            for (int n = 0; n < count; n++)
            {
                entry.available.Enqueue(CreateInstance(entry));
            }
        }
    }

    private void Start()
    {
        SpawnMany(spawnOnStart);
    }

    [Button]
    public Base_Enemy Spawn()
    {
        if (!TryGetSpawnPoint(out Vector3 point))
        {
            Debug.LogWarning($"{name}: couldn't find a NavMesh point inside any spawn area.", this);
            return null;
        }
        return Spawn(point);
    }

    public Base_Enemy Spawn(Vector3 position)
    {
        EnemySpawnEntry entry = PickEntry(GetWave());
        if (entry == null)
        {
            Debug.LogWarning($"{name}: no enemy types to spawn.", this);
            return null;
        }

        Base_Enemy enemy = TakeOrCreate(entry);
        if (enemy == null)
        {
            enemy = TakeFromAnyAvailable();
        }
        if (enemy == null) return null;

        enemy.transform.SetParent(transform, true);
        active.Add(enemy);
        enemy.Spawn(position, GetDifficultyLevel());
        return enemy;
    }

    /// <summary>
    /// Spawns up to <paramref name="count"/> enemies. Returns how many actually spawned,
    /// which can be lower if the pool hit its max size or no spawn point was found.
    /// </summary>
    public int SpawnMany(int count)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            if (Spawn() != null) spawned++;
        }
        return spawned;
    }

    [Button]
    public void DespawnAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            active[i].Despawn();
        }
    }

    /// <summary>
    /// Reapplies the current difficulty to every living enemy. New spawns already use this.
    /// </summary>
    [Button("Apply Difficulty To Active")]
    public void ApplyStatsToActive()
    {
        int level = GetDifficultyLevel();
        for (int i = 0; i < active.Count; i++)
        {
            active[i].ApplyDifficultyLevel(level);
        }
    }

    private Base_Enemy CreateInstance(EnemySpawnEntry entry)
    {
        Base_Enemy enemy = Instantiate(entry.prefab, inactiveContainer);
        enemy.gameObject.SetActive(false);
        enemy.Despawned += ReturnToPool;
        owners[enemy] = entry;
        entry.created++;
        totalCreated++;
        return enemy;
    }

    private Base_Enemy TakeOrCreate(EnemySpawnEntry entry)
    {
        if (entry.available == null) entry.available = new Queue<Base_Enemy>();
        if (entry.available.Count > 0) return entry.available.Dequeue();
        if (canGrow && totalCreated < maxSize) return CreateInstance(entry);
        return null;
    }

    private Base_Enemy TakeFromAnyAvailable()
    {
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry != null && entry.available.Count > 0) return entry.available.Dequeue();
        }
        return null;
    }

    private void ReturnToPool(Base_Enemy enemy)
    {
        if (!active.Remove(enemy)) return;

        enemy.transform.SetParent(inactiveContainer, false);
        if (owners.TryGetValue(enemy, out EnemySpawnEntry entry))
        {
            if (entry.available == null) entry.available = new Queue<Base_Enemy>();
            entry.available.Enqueue(enemy);
        }
    }

    private EnemySpawnEntry PickEntry(int wave)
    {
        float total = 0f;
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            total += GetWeight(enemyTypes[i], wave);
        }

        if (total <= 0f)
        {
            for (int i = 0; i < enemyTypes.Count; i++)
            {
                if (enemyTypes[i] != null && enemyTypes[i].prefab != null) return enemyTypes[i];
            }
            return null;
        }

        float roll = Random.value * total;
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            float weight = GetWeight(enemyTypes[i], wave);
            if (roll < weight) return enemyTypes[i];
            roll -= weight;
        }

        return enemyTypes[enemyTypes.Count - 1];
    }

    private static float GetWeight(EnemySpawnEntry entry, int wave)
    {
        if (entry == null || entry.prefab == null || entry.spawnWeight <= 0f) return 0f;
        return wave >= entry.unlockWave ? entry.spawnWeight : 0f;
    }

    private int GetWave()
    {
        return townManager != null ? Mathf.Max(1, townManager.Wave) : 1;
    }

    public int GetDifficultyLevel()
    {
        // Wave 1 is difficulty 0. The first increase happens after wavesPerDifficultyIncrease waves.
        return (GetWave() - 1) / Mathf.Max(1, wavesPerDifficultyIncrease);
    }

    private void EnsureFallbackType()
    {
        bool hasPrefab = false;
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (enemyTypes[i] != null && enemyTypes[i].prefab != null)
            {
                hasPrefab = true;
                break;
            }
        }

        if (hasPrefab) return;

        if (enemyPrefab == null)
        {
            Debug.LogError($"{name}: no enemy types assigned.", this);
            return;
        }

        enemyTypes.Add(new EnemySpawnEntry
        {
            prefab = enemyPrefab,
            spawnWeight = 10f,
            unlockWave = 1,
            initialSize = initialSize
        });
    }

    private bool TryGetSpawnPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (spawnAreas.Count == 0) return false;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Collider area = spawnAreas[Random.Range(0, spawnAreas.Count)];
            if (area == null || !area.enabled || !area.gameObject.activeInHierarchy) continue;

            Bounds bounds = area.bounds;
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                Random.Range(bounds.min.z, bounds.max.z));

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, bounds.size.y + insideTolerance, navMeshAreaMask))
                continue;

            Vector3 closest = area.ClosestPoint(hit.position);
            if ((closest - hit.position).sqrMagnitude <= insideTolerance * insideTolerance)
            {
                point = hit.position;
                return true;
            }
        }
        return false;
    }

    [Button("Log Spawn Weights")]
    private void LogSpawnWeights()
    {
        int[] waves = { 1, 3, 5, 10, 15, 20 };
        var log = new System.Text.StringBuilder($"{name} spawn odds (difficulty every {wavesPerDifficultyIncrease} waves):\n");

        foreach (int wave in waves)
        {
            int difficulty = (wave - 1) / Mathf.Max(1, wavesPerDifficultyIncrease);
            float total = 0f;
            foreach (EnemySpawnEntry entry in enemyTypes) total += GetWeight(entry, wave);
            log.Append($"Wave {wave} (diff {difficulty}):");

            if (total <= 0f)
            {
                log.AppendLine("  none");
                continue;
            }

            foreach (EnemySpawnEntry entry in enemyTypes)
            {
                if (entry == null || entry.prefab == null) continue;
                float chance = GetWeight(entry, wave) / total;
                log.Append($"  {entry.prefab.name} {chance * 100f:0}%");
            }
            log.AppendLine();
        }

        Debug.Log(log.ToString(), this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        foreach (Collider area in spawnAreas)
        {
            if (area == null) continue;
            Bounds b = area.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
