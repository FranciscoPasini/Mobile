using UnityEngine;

/// <summary>
/// Flies until it hits something, then damages and shoves enemies in a radius.
/// Drop this on the grenade prefab.
/// </summary>
public class Bullet_Grenade : Base_Bullet
{
    [SerializeField, Min(0.1f)] private float explosionRadius = 3.5f;
    [SerializeField, Min(0f)] private float knockbackStrength = 2.5f;
    [SerializeField, Min(0f)] private float stunDuration = 0.7f;
    [Tooltip("Arc. 0 flies straight.")]
    [SerializeField] private float gravity = 14f;
    [SerializeField] private LayerMask explosionMask = ~0;
    [SerializeField] private LayerMask collideMask = ~0;

    private Vector3 velocity;
    private bool exploded;
    private readonly Collider[] explosionHits = new Collider[32];

    protected override void OnInitialized()
    {
        exploded = false;
        Vector3 toTarget = targetPosition - transform.position;
        if (toTarget.sqrMagnitude < 0.0001f) toTarget = transform.forward;
        velocity = toTarget.normalized * speed;
    }

    protected override void MoveBullet()
    {
        velocity += Vector3.down * gravity * Time.deltaTime;
        Vector3 step = velocity * Time.deltaTime;
        float distance = step.magnitude;

        if (distance > 0f && Physics.SphereCast(transform.position, 0.15f, step.normalized, out RaycastHit hit, distance, collideMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            Explode();
            return;
        }

        transform.position += step;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }
    }

    protected override bool ShouldIgnoreCollider(Collider other)
    {
        if (other == null || other.CompareTag("Player")) return true;
        if (other.isTrigger && !other.CompareTag("Enemy")) return true;
        return false;
    }

    protected override void OnHit(Collider other)
    {
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector3 origin = transform.position;
        int count = Physics.OverlapSphereNonAlloc(origin, explosionRadius, explosionHits, explosionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!TryGetEnemy(explosionHits[i], out Base_Enemy enemy)) continue;

            DealDamage(explosionHits[i]);
            enemy.Knockback(origin, knockbackStrength, stunDuration);
        }

        DestroyBullet();
    }
}
