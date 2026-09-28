using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using NaughtyAttributes;

public class EnemyPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private Base_Enemy enemyPrefab;
    [SerializeField] private int initialSize = 10;
    [Tooltip("Allow the pool to instantiate extra enemies when all pooled ones are in use.")]
    [SerializeField] private bool canGrow = true;
    [Tooltip("Hard ceiling on how many enemies can exist, even when Can Grow is on.")]
    [SerializeField] private int maxSize = 30;

    [Header("Spawn Areas")]
    [Tooltip("Colliders defining where enemies can spawn. Box, Sphere, Capsule or convex Mesh colliders.")]
    [SerializeField] private List<Collider> spawnAreas = new List<Collider>();
    [SerializeField] private int maxSpawnAttempts = 30;
    [Tooltip("How far a sampled NavMesh point may be from the collider and still count as inside it.")]
    [SerializeField] private float insideTolerance = 0.5f;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

    [Header("Auto Spawn")]
    [SerializeField] private int spawnOnStart = 0;

    private readonly Queue<Base_Enemy> available = new Queue<Base_Enemy>();
    private readonly List<Base_Enemy> active = new List<Base_Enemy>();
    private Transform inactiveContainer;
    private int totalCreated;

    public IReadOnlyList<Base_Enemy> ActiveEnemies => active;
    public int ActiveCount => active.Count;
    public int AvailableCount => available.Count;
    public int TotalCreated => totalCreated;

    private void Awake()
    {
        maxSize = Mathf.Max(maxSize, initialSize);

        // Instances are created under an inactive parent so their Awake (and NavMeshAgent)
        // doesn't run until they are placed on the NavMesh.
        inactiveContainer = new GameObject("InactiveEnemies").transform;
        inactiveContainer.SetParent(transform, false);
        inactiveContainer.gameObject.SetActive(false);

        for (int i = 0; i < initialSize; i++)
        {
            available.Enqueue(CreateInstance());
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
        Base_Enemy enemy;
        if (available.Count > 0)
        {
            enemy = available.Dequeue();
        }
        else if (canGrow && totalCreated < maxSize)
        {
            enemy = CreateInstance();
        }
        else
        {
            // Pool exhausted: either growth is off, or we hit the max size ceiling.
            return null;
        }

        enemy.transform.SetParent(transform, true);
        active.Add(enemy);
        enemy.Spawn(position);
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

    private Base_Enemy CreateInstance()
    {
        Base_Enemy enemy = Instantiate(enemyPrefab, inactiveContainer);
        enemy.gameObject.SetActive(false);
        enemy.Despawned += ReturnToPool;
        totalCreated++;
        return enemy;
    }

    private void ReturnToPool(Base_Enemy enemy)
    {
        if (!active.Remove(enemy)) return;

        enemy.transform.SetParent(inactiveContainer, false);
        available.Enqueue(enemy);
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
