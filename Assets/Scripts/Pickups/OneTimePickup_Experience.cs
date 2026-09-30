using UnityEngine;

/// <summary>
/// Grants experience from <see cref="OneTimePickup_Experience_Data"/> (flat amount plus a share of the current level).
/// </summary>
public class OneTimePickup_Experience : Base_OneTimePickup
{
    protected override void ApplyEffect()
    {
        Player_ExperienceAndStats stats = PlayerStats;
        if (stats == null)
        {
            stats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }

        if (stats == null) return;

        OneTimePickup_Experience_Data xpData = pickupData as OneTimePickup_Experience_Data;
        int amount = xpData != null ? xpData.GetExperienceAmount(stats) : 40;
        if (amount <= 0) return;

        stats.AddExperience(amount);
    }
}
