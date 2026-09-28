using UnityEngine;

public interface IDamageable 
{
    void TakeDamage(float damage);
    void Heal(float amount);
    void Die();
    void Respawn();
}
