using UnityEngine;
using System;

public class Building_Town : base_TownBuilding, IDamageable
{

    [SerializeField] public int maxHealth;
    [SerializeField] public int currentHealth;

    public Action<float> onDamageTaken;
    public Action onHealed;
    public Action onDied;
    public Action onRespawned;

    private float leftoverDamage;


    private void Start()
    {
        currentHealth = maxHealth;
        leftoverDamage = 0f;
    }


    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0) return;

        leftoverDamage += damage;
        int applied = Mathf.FloorToInt(leftoverDamage);
        if (applied > 0)
        {
            leftoverDamage -= applied;
            currentHealth -= applied;
            if (currentHealth <= 0)
            {
                leftoverDamage = 0f;
                Die();
            }
        }

        onDamageTaken?.Invoke(damage);
    }

    public void Heal(float amount)
    {
        currentHealth += (int)amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        onHealed?.Invoke();
    }

    public void Die()
    {
        currentHealth = 0;
        onDied?.Invoke();
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        leftoverDamage = 0f;
        onRespawned?.Invoke();
    }


    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
    }
}
