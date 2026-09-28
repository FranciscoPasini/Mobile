using UnityEngine;
using UnityEngine.AI;
using System;

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

    [SerializeField] private NavMeshAgent agent;

    [Header("Loot")]
    [Tooltip("Pool the death coin comes from. Found in the scene if left empty.")]
    [SerializeField] private CoinPool coinPool;

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
        gameObject.SetActive(true);
        transform.position = position;
        
        // Ensure the NavMeshAgent is enabled and on the NavMesh
        agent.enabled = true;
        agent.Warp(position);
        
        currentTimeToAttack = 0f;
        nextPathUpdateTime = 0f;
        lastTargetPosition = Vector3.positiveInfinity;
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

        Despawn();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
    }
}