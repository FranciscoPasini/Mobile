using UnityEngine;

public class Bullet_Shotgun : Base_Bullet
{
    [Tooltip("How far a hit enemy is shoved on the NavMesh.")]
    [SerializeField, Min(0f)] private float knockbackStrength = 0.6f;
    [Tooltip("Seconds the enemy stays still after a pellet hits.")]
    [SerializeField, Min(0f)] private float stunDuration = 1f;

    protected override bool ShouldIgnoreCollider(Collider other)
    {
        if (other == null || other.CompareTag("Player")) return true;
        if (other.isTrigger && !other.CompareTag("Enemy")) return true;
        return false;
    }

    protected override void OnHit(Collider other)
    {
        if (!TryGetEnemy(other, out Base_Enemy enemy)) return;

        DealDamage(other);
        enemy.Knockback(transform.position, knockbackStrength, stunDuration);
        DestroyBullet();
    }
}
