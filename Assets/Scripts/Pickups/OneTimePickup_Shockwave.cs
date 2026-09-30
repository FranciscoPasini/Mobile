using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Damages, stuns, and knocks back living enemies around the player.
/// </summary>
public class OneTimePickup_Shockwave : Base_OneTimePickup
{
    private readonly Collider[] hits = new Collider[32];
    private readonly List<Base_Enemy> hitEnemies = new List<Base_Enemy>(16);

    protected override void ApplyEffect()
    {
        OneTimePickup_Shockwave_Data blastData = pickupData as OneTimePickup_Shockwave_Data;
        float radius = blastData != null ? blastData.Radius : 8f;
        float damage = blastData != null ? blastData.Damage : 8f;
        float stun = blastData != null ? blastData.StunDuration : 1.5f;
        float knockback = blastData != null ? blastData.Knockback : 1.2f;
        LayerMask mask = blastData != null ? blastData.HitMask : (LayerMask)~0;

        Vector3 origin = PlayerTransform != null ? PlayerTransform.position : transform.position;
        int count = Physics.OverlapSphereNonAlloc(origin, radius, hits, mask, QueryTriggerInteraction.Collide);

        hitEnemies.Clear();
        for (int i = 0; i < count; i++)
        {
            Base_Enemy enemy = hits[i] != null ? hits[i].GetComponentInParent<Base_Enemy>() : null;
            if (enemy == null || !enemy.IsAlive || hitEnemies.Contains(enemy)) continue;
            hitEnemies.Add(enemy);
        }

        for (int i = 0; i < hitEnemies.Count; i++)
        {
            Base_Enemy enemy = hitEnemies[i];
            if (damage > 0f) enemy.TakeDamage(damage);
            if (!enemy.IsAlive) continue;
            enemy.Knockback(origin, knockback, stun);
        }
    }
}
