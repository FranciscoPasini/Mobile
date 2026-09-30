using UnityEngine;
using UnityEngine.AI;
using System;
using NaughtyAttributes;

public class Base_Enemy : MonoBehaviour, IPoolable, IDamageable
{
    public event Action<Base_Enemy> Despawned;

    [Header("References")]
    [SerializeField] private Transform target; // the town building to attack
    [SerializeField] private LayerMask attackLayerMask = ~0;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 100f;
    [SerializeField] private float attackRange = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;

    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float currentHealth = 10f;
    private bool isDead;

    public bool IsAlive => !isDead && gameObject.activeInHierarchy;

    [Header("Combat")]
    [SerializeField] private float damage = 5;
    [SerializeField] private float timeToAttack = 1.5f;
    private float currentTimeToAttack = 0f;
    private Building_Town townBuilding;






    [Header("Pathing")]
    [Tooltip("How often (in seconds) to recalculate the path to the target.")]
    [SerializeField] private float pathUpdateInterval = 0.2f;
    [Tooltip("Minimum target movement (meters) required to force a path update before the interval elapses.")]
    [SerializeField] private float pathUpdateMinTargetDelta = 0.5f;
    private float nextPathUpdateTime = 0f;
    private Vector3 lastTargetPosition;
    private float stunTimer;

    [SerializeField] private NavMeshAgent agent;

    [Header("Loot")]
    [Tooltip("Pool the death coin comes from. Found in the scene if left empty.")]
    [SerializeField] private CoinPool coinPool;
    [Tooltip("Weapons dropped on death. Found in the scene if left empty.")]
    [SerializeField] private WeaponPickupPool weaponPickupPool;
    [Tooltip("Town heals and other one-shot pickups. Found in the scene if left empty.")]
    [SerializeField] private OneTimePickupPool oneTimePickupPool;
    [Tooltip("Experience the player gets for the kill.")]
    [SerializeField, Min(0)] private int experienceReward = 20;
    [Tooltip("Receives the kill experience. Found in the scene if left empty.")]
    [SerializeField] private Player_ExperienceAndStats playerStats;

    [Header("Difficulty Scaling")]
    [Tooltip("How this prefab's stats grow. A fast enemy and a tank should use different curves.")]
    [SerializeField] private EnemyScaling scaling = new EnemyScaling();

    private float baseHealth;
    private float baseDamage;
    private float baseSpeed;
    private float baseTimeToAttack;
    private int baseExperience;
    private int difficultyLevel;

    public int DifficultyLevel => difficultyLevel;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (coinPool == null)
        {
            coinPool = FindFirstObjectByType<CoinPool>();
        }

        if (weaponPickupPool == null)
        {
            weaponPickupPool = FindFirstObjectByType<WeaponPickupPool>();
        }

        if (oneTimePickupPool == null)
        {
            oneTimePickupPool = FindFirstObjectByType<OneTimePickupPool>();
        }

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }

        if (scaling == null) scaling = new EnemyScaling();

        baseHealth = maxHealth;
        baseDamage = damage;
        baseSpeed = chaseSpeed;
        baseTimeToAttack = timeToAttack;
        baseExperience = experienceReward;

        agent.speed = chaseSpeed;
        // Make sure the agent can actually get within attack range before it stops moving.
        agent.stoppingDistance = Mathf.Min(stoppingDistance, attackRange);

        if (target == null)
        {
            townBuilding = FindFirstObjectByType<Building_Town>();
            if (townBuilding != null)
            {
                target = townBuilding.transform;
            }
            else
            {
                Debug.LogError("No Building_Town found in the scene!", this);
            }
        }
        else
        {
            // If a target was assigned in the inspector, try to grab its Building_Town up front.
            townBuilding = target.GetComponent<Building_Town>();
        }
    }

    private void Update()
    {
        if (target == null) 
        {
            Debug.LogError($"{gameObject.name}: Target is null", this);
            return;
        }

        if (UpdateStun())
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= detectionRange)
        {
            //UPDATE PATHING WITH INTERVALS AND NOT ON TICK
            bool intervalElapsed = Time.time >= nextPathUpdateTime;
            bool targetMovedEnough = Vector3.Distance(target.position, lastTargetPosition) >= pathUpdateMinTargetDelta;

            if (intervalElapsed || targetMovedEnough)
            {
                agent.SetDestination(target.position);
                lastTargetPosition = target.position;
                nextPathUpdateTime = Time.time + pathUpdateInterval;
            }
        }
        else
        {
            agent.ResetPath();
        }

        if (distance <= attackRange)
        {
            if (currentTimeToAttack <= timeToAttack) 
            {
                currentTimeToAttack += Time.deltaTime;
            }
            else
            {
                Attack();
                currentTimeToAttack = 0f;
            }
        }
    }

    private void Attack()
    {
        RaycastHit hit;
        Vector3 direction = target.position - transform.position;

        // Ignore triggers so coins on the ground or the player's range sphere can't absorb the attack.
        if (Physics.Raycast(transform.position, direction, out hit, attackRange, attackLayerMask, QueryTriggerInteraction.Ignore))
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
        else if (townBuilding != null)
        {
            // Fallback: raycast can miss (blocked, wrong angle, etc.) but we still know our target.
            townBuilding.TakeDamage(damage);
        }
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);
    }

    public void Spawn(Vector3 position)
    {
        Spawn(position, difficultyLevel);
    }

    public void Spawn(Vector3 position, int difficulty)
    {
        gameObject.SetActive(true);
        ApplyDifficultyLevel(difficulty);
        transform.position = position;
        
        // Ensure the NavMeshAgent is enabled and on the NavMesh
        agent.enabled = true;
        agent.Warp(position);
        
        currentTimeToAttack = 0f;
        nextPathUpdateTime = 0f;
        lastTargetPosition = Vector3.positiveInfinity;
        ClearStun();
        Respawn();
    }

    public void Despawn()
    {
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        agent.enabled = false;
        gameObject.SetActive(false);
        Despawned?.Invoke(this);
    }

    public void TakeDamage(float damage)
    {
        // Several bullets can land in the same frame; ignore hits once we're already dead.
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0f;

        // Only real deaths drop loot. Pool cleanup through Despawn() doesn't.
        if (coinPool != null)
        {
            coinPool.DropCoin(transform.position);
        }

        if (weaponPickupPool != null)
        {
            weaponPickupPool.TryDrop(transform.position);
        }

        if (oneTimePickupPool == null)
        {
            oneTimePickupPool = FindFirstObjectByType<OneTimePickupPool>();
        }

        if (oneTimePickupPool != null)
        {
            oneTimePickupPool.TryDrop(transform.position);
        }

        if (playerStats != null)
        {
            playerStats.AddExperience(experienceReward);
        }

        Despawn();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        ClearStun();
    }

    /// <summary>
    /// Stops the enemy from walking or attacking for <paramref name="duration"/> seconds.
    /// Calling again while stunned uses the longer remaining time.
    /// </summary>
    public void Stun(float duration)
    {
        if (isDead || duration <= 0f || agent == null) return;

        if (stunTimer <= 0f && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        stunTimer = Mathf.Max(stunTimer, duration);
    }

    /// <summary>
    /// Shoves the enemy away from <paramref name="origin"/> on the NavMesh and stuns it.
    /// </summary>
    public void Knockback(Vector3 origin, float distance, float stunDuration)
    {
        Stun(stunDuration);
        if (isDead || distance <= 0f || agent == null) return;

        Vector3 dir = transform.position - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        Vector3 candidate = transform.position + dir * distance;
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, distance + 0.5f, NavMesh.AllAreas))
        {
            if (agent.enabled) agent.Warp(hit.position);
            else transform.position = hit.position;
        }
        else if (agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(transform.position + dir * (distance * 0.5f));
        }
    }

    private bool UpdateStun()
    {
        if (stunTimer <= 0f) return false;

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            ClearStun();
            return false;
        }

        return true;
    }

    private void ClearStun()
    {
        stunTimer = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    #region Difficulty

    /// <summary>
    /// Recalculates every scaled stat from this prefab's base values and the given difficulty step.
    /// Safe to call more than once; levels never compound.
    /// </summary>
    public void ApplyDifficultyLevel(int level)
    {
        difficultyLevel = Mathf.Max(0, level);
        ApplyHealthScaling(difficultyLevel);
        ApplyDamageScaling(difficultyLevel);
        ApplySpeedScaling(difficultyLevel);
        ApplyAttackRateScaling(difficultyLevel);
        ApplyExperienceScaling(difficultyLevel);
    }

    public void ApplyHealthScaling(int level)
    {
        maxHealth = scaling.EvaluateHealth(baseHealth, level);
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public void ApplyDamageScaling(int level)
    {
        damage = scaling.EvaluateDamage(baseDamage, level);
    }

    public void ApplySpeedScaling(int level)
    {
        chaseSpeed = scaling.EvaluateSpeed(baseSpeed, level);
        if (agent != null) agent.speed = chaseSpeed;
    }

    public void ApplyAttackRateScaling(int level)
    {
        timeToAttack = scaling.EvaluateAttackRate(baseTimeToAttack, level);
    }

    public void ApplyExperienceScaling(int level)
    {
        experienceReward = Mathf.Max(0, Mathf.RoundToInt(scaling.EvaluateExperience(baseExperience, level)));
    }

    [NaughtyAttributes.Button("Log Difficulty Preview")]
    private void LogDifficultyPreview()
    {
        var log = new System.Text.StringBuilder($"{name} difficulty:\n");
        float previewHealth = Application.isPlaying ? baseHealth : maxHealth;
        float previewDamage = Application.isPlaying ? baseDamage : damage;
        float previewSpeed = Application.isPlaying ? baseSpeed : chaseSpeed;
        float previewAttack = Application.isPlaying ? baseTimeToAttack : timeToAttack;
        float previewXp = Application.isPlaying ? baseExperience : experienceReward;

        for (int level = 0; level <= 20; level++)
        {
            float hp = scaling.EvaluateHealth(previewHealth, level);
            float dmg = scaling.EvaluateDamage(previewDamage, level);
            float spd = scaling.EvaluateSpeed(previewSpeed, level);
            float atk = scaling.EvaluateAttackRate(previewAttack, level);
            int xp = Mathf.RoundToInt(scaling.EvaluateExperience(previewXp, level));
            log.AppendLine($"Lv {level}: hp {hp:0.#}, dmg {dmg:0.#}, speed {spd:0.##}, every {atk:0.##}s, xp {xp}");
        }

        Debug.Log(log.ToString(), this);
    }

    #endregion
}