using UnityEngine;

public class Bullet_Standard : Base_Bullet
{
    protected override void OnHit(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        // The collider may sit on a child of the enemy root, so search upwards too.
        IDamageable damageable = other.GetComponent<IDamageable>()
            ?? other.GetComponentInParent<IDamageable>();

        damageable?.TakeDamage(damage);
        DestroyBullet();
    }
}
