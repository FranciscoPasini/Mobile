using UnityEngine;

/// <summary>
/// Heals the town by a percent of max health from <see cref="OneTimePickup_TownHeal_Data"/>.
/// </summary>
public class OneTimePickup_TownHeal : Base_OneTimePickup
{
    protected override void ApplyEffect()
    {
        Building_Town town = Town;
        if (town == null)
        {
            town = FindFirstObjectByType<Building_Town>();
        }

        if (town == null) return;

        OneTimePickup_TownHeal_Data healData = pickupData as OneTimePickup_TownHeal_Data;
        float percent = healData != null ? healData.HealPercent / 100f : 0.2f;
        float amount = town.maxHealth * percent;
        if (amount <= 0f) return;

        town.Heal(amount);
    }
}
