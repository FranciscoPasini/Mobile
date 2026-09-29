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


    private void Start()
    {
        currentHealth = maxHealth;
    }


    public void TakeDamage(float damage)
    {
        currentHealth -= (int)damage;
        if (currentHealth <= 0)
        {
            Die();

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
        onRespawned?.Invoke();
    }


    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
    }
}
