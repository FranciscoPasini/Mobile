using UnityEngine;
using System;
using System.Collections.Generic;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "New Turret", menuName = "Turret")]
public class Building_Turret_Data : ScriptableObject
{
    [Tooltip("Entry 0 is the purchase. Every entry after that is one upgrade.")]
    public List<TurretLevel> levels = new List<TurretLevel> { new TurretLevel() };

    [Tooltip("Keep upgrading past the last entry, scaling from it with the formula below.")]
    public bool infiniteLevels = false;

    [ShowIf(nameof(infiniteLevels))]
    public TurretScaling scaling = new TurretScaling();

    public bool HasLevel(int index)
    {
        if (levels == null || levels.Count == 0 || index < 0) return false;
        return infiniteLevels || index < levels.Count;
    }

    /// <summary>
    /// Stats for a 0-based level index. Indexes past the list are extrapolated from the
    /// last entry when infinite levels are on. Allocates for extrapolated levels, so cache the result.
    /// </summary>
    public TurretLevel GetLevel(int index)
    {
        if (levels == null || levels.Count == 0) return null;

        int last = levels.Count - 1;
        if (index <= last) return levels[Mathf.Max(index, 0)];
        if (!infiniteLevels) return levels[last];

        return scaling.Extrapolate(levels[last], index - last);
    }

    [Button("Log Level Preview")]
    private void LogLevelPreview()
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning($"{name}: no levels to preview.", this);
            return;
        }

        int count = infiniteLevels ? levels.Count + 30 : levels.Count;
        var log = new System.Text.StringBuilder($"{name} levels:\n");

        for (int i = 0; i < count; i++)
        {
            TurretLevel stats = GetLevel(i);
            float dps = stats.fireRate > 0f ? stats.bulletDamage / stats.fireRate : 0f;
            log.AppendLine($"Lv {i + 1}: cost {stats.cost}, damage {stats.bulletDamage}, every {stats.fireRate:0.00}s, range {stats.range:0.0}, speed {stats.bulletSpeed:0.0}, dps {dps:0.0}");
        }

        Debug.Log(log.ToString(), this);
    }
}

[Serializable]
public class TurretLevel
{
    [Tooltip("Coins needed to buy this level.")]
    public int cost = 50;
    public int bulletDamage = 1;
    public float bulletSpeed = 20f;
    public float range = 8f;
    [Tooltip("Seconds between shots. Lower is faster, same as the weapon fire rate.")]
    public float fireRate = 1f;
}

/// <summary>
/// Cost grows exponentially while damage grows linearly and the other stats approach caps,
/// so each coin buys less power the higher the level goes.
/// </summary>
[Serializable]
public class TurretScaling
{
    [Header("Cost")]
    [Tooltip("Multiplier applied per level. 1.15 means each level costs 15% more than the last.")]
    [Min(1f)] public float costGrowth = 1.15f;

    [Header("Damage")]
    [Tooltip("Damage added per level, as a fraction of the last hand-made level's damage.")]
    [Min(0f)] public float damageGrowth = 0.25f;

    [Header("Fire Rate")]
    [Tooltip("Fastest allowed seconds between shots. Never reached, only approached.")]
    [Min(0.05f)] public float minFireRate = 0.15f;
    [Tooltip("Share of the remaining gap to the limit closed each level.")]
    [Range(0f, 1f)] public float fireRateApproach = 0.07f;

    [Header("Range")]
    [Tooltip("Upper limit for range. Keep it below the map size or turrets cover everything.")]
    public float maxRange = 16f;
    [Range(0f, 1f)] public float rangeApproach = 0.08f;

    [Header("Bullet Speed")]
    [Tooltip("Bullets move a fixed step per frame, so going much faster lets them skip through enemies.")]
    public float maxBulletSpeed = 50f;
    [Range(0f, 1f)] public float bulletSpeedApproach = 0.08f;

    public TurretLevel Extrapolate(TurretLevel from, int levelsPast)
    {
        double cost = Math.Max(from.cost, 1) * Math.Pow(costGrowth, levelsPast);

        return new TurretLevel
        {
            // Clamped because the exponential outgrows int within a couple hundred levels.
            cost = (int)Math.Round(Math.Min(cost, int.MaxValue)),
            bulletDamage = Mathf.Max(from.bulletDamage, Mathf.RoundToInt(from.bulletDamage * (1f + damageGrowth * levelsPast))),
            // Never worse than the last hand-made level, even if it already beats the cap.
            fireRate = Mathf.Min(from.fireRate, Approach(from.fireRate, minFireRate, fireRateApproach, levelsPast)),
            range = Mathf.Max(from.range, Approach(from.range, maxRange, rangeApproach, levelsPast)),
            bulletSpeed = Mathf.Max(from.bulletSpeed, Approach(from.bulletSpeed, maxBulletSpeed, bulletSpeedApproach, levelsPast)),
        };
    }

    private static float Approach(float start, float limit, float rate, int steps)
    {
        return limit + (start - limit) * Mathf.Pow(1f - rate, steps);
    }
}
