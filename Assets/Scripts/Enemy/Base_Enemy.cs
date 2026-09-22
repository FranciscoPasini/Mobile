using UnityEngine;
using UnityEngine.AI;

public class Base_Enemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target; // usually the player (¿o la base?)

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 1f;

    [Header("Combat")]
    [SerializeField] private float damage = 5;
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    private float nextAttackTime = 0f;

    [SerializeField] private NavMeshAgent agent;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;

        if (target == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    private void Update()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= detectionRange)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            agent.ResetPath();
        }

        if (distance <= attackRange)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (Time.time >= nextAttackTime)
        {
            TownManager.Instance.TakeDamage(damage);
            nextAttackTime = Time.time + timeBetweenAttacks;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, attackRange);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}