using UnityEngine;
using System;

public enum WeaponStat
{
    Damage,
    FireRate,
    BulletSpeed,
    Range,
    Ammo
}

public enum ScalingMethod
{
    /// <summary>Stat never changes with level.</summary>
    None,
    /// <summary>factor = 1 + rate * level. Steady, predictable growth.</summary>
    Linear,
    /// <summary>factor = (1 + rate) ^ level. Explodes at high levels, use tiny rates or a cap.</summary>
    Exponential,
    /// <summary>factor = 1 + rate * ln(1 + level). Fast early, then slows down a lot.</summary>
    Logarithmic,
    /// <summary>factor = 1 + rate * sqrt(level). Between linear and logarithmic.</summary>
    SquareRoot,
    /// <summary>Closes 'rate' of the remaining gap to maxFactor each level. Never reaches it.</summary>
    Asymptotic
}

/// <summary>
/// Upgrade levels the player has bought for each weapon stat. Level 0 means base stats.
/// </summary>
[Serializable]
public struct WeaponStatLevels
{
    [Min(0)] public int damage;
    [Min(0)] public int fireRate;
    [Min(0)] public int bulletSpeed;
    [Min(0)] public int range;
    [Min(0)] public int ammo;

    public int Get(WeaponStat stat)
    {
        switch (stat)
        {
            case WeaponStat.Damage: return damage;
            case WeaponStat.FireRate: return fireRate;
            case WeaponStat.BulletSpeed: return bulletSpeed;
            case WeaponStat.Range: return range;
            case WeaponStat.Ammo: return ammo;
            default: return 0;
        }
    }

    public void Add(WeaponStat stat, int amount)
    {
        switch (stat)
        {
            case WeaponStat.Damage: damage = Mathf.Max(0, damage + amount); break;
            case WeaponStat.FireRate: fireRate = Mathf.Max(0, fireRate + amount); break;
            case WeaponStat.BulletSpeed: bulletSpeed = Mathf.Max(0, bulletSpeed + amount); break;
            case WeaponStat.Range: range = Mathf.Max(0, range + amount); break;
            case WeaponStat.Ammo: ammo = Mathf.Max(0, ammo + amount); break;
        }
    }
}

/// <summary>
/// Turns a base stat and an upgrade level into an improvement factor (always 1 or more).
/// Higher-is-better stats are multiplied by it, lower-is-better stats (fire rate) are divided.
/// </summary>
[Serializable]
public class StatScaling
{
    public ScalingMethod method = ScalingMethod.Linear;

    [Tooltip("Linear/Log/Sqrt: factor gained per step. Exponential: growth per level (0.05 = +5%). Asymptotic: share of the remaining gap closed per level (0-1).")]
    [Min(0f)] public float rate = 0.1f;

    [Tooltip("Asymptotic: the factor it approaches. Other methods: hard cap on the factor, 0 = no cap. 2 means at most double (or half the time for fire rate).")]
    [Min(0f)] public float maxFactor = 0f;

    // Keeps exponential curves from overflowing into Infinity at absurd levels.
    private const float MaxSafeFactor = 1000000f;

    public StatScaling() { }

    public StatScaling(ScalingMethod method, float rate, float maxFactor)
    {
        this.method = method;
        this.rate = rate;
        this.maxFactor = maxFactor;
    }

    public float Evaluate(float baseValue, int level, bool lowerIsBetter)
    {
        float factor = GetFactor(level);
        return lowerIsBetter ? baseValue / factor : baseValue * factor;
    }

    public float GetFactor(int level)
    {
        if (level <= 0) return 1f;

        float factor;
        switch (method)
        {
            case ScalingMethod.Linear:
                factor = 1f + rate * level;
                break;
            case ScalingMethod.Exponential:
                factor = Mathf.Pow(1f + rate, level);
                break;
            case ScalingMethod.Logarithmic:
                factor = 1f + rate * Mathf.Log(1f + level);
                break;
            case ScalingMethod.SquareRoot:
                factor = 1f + rate * Mathf.Sqrt(level);
                break;
            case ScalingMethod.Asymptotic:
                float limit = Mathf.Max(maxFactor, 1f);
                return limit + (1f - limit) * Mathf.Pow(1f - Mathf.Clamp01(rate), level);
            default:
                return 1f;
        }

        if (float.IsNaN(factor) || factor > MaxSafeFactor) factor = MaxSafeFactor;
        if (maxFactor > 1f) factor = Mathf.Min(factor, maxFactor);
        return Mathf.Max(factor, 1f);
    }

    /// <summary>
    /// True when another level would not raise the factor. Asymptotic curves never report capped.
    /// </summary>
    public bool IsCapped(int level)
    {
        if (method == ScalingMethod.None) return true;
        if (method == ScalingMethod.Asymptotic) return false;
        if (maxFactor <= 1f) return false;
        return GetFactor(level + 1) <= GetFactor(level) + 0.0001f;
    }
}

/// <summary>
/// Per-weapon scaling. Damage and ammo grow without limit, while fire rate, range and bullet
/// speed approach caps: an ever-faster weapon fires every frame, an ever-longer range covers
/// the map, and ever-faster bullets tunnel through enemies.
/// </summary>
[Serializable]
public class WeaponScaling
{
    [Tooltip("+15% of base damage per level, no cap. Enemy health should outpace this over the waves.")]
    public StatScaling damage = new StatScaling(ScalingMethod.Linear, 0.15f, 0f);

    [Tooltip("Seconds between shots. Approaches 3x faster than base.")]
    public StatScaling fireRate = new StatScaling(ScalingMethod.Asymptotic, 0.08f, 3f);

    [Tooltip("Approaches 2x base speed.")]
    public StatScaling bulletSpeed = new StatScaling(ScalingMethod.Asymptotic, 0.1f, 2f);

    [Tooltip("Approaches 1.5x base range.")]
    public StatScaling range = new StatScaling(ScalingMethod.Asymptotic, 0.06f, 1.5f);

    [Tooltip("Only matters for picked-up weapons, the default weapon has infinite ammo.")]
    public StatScaling ammo = new StatScaling(ScalingMethod.Linear, 0.2f, 0f);

    public StatScaling Get(WeaponStat stat)
    {
        switch (stat)
        {
            case WeaponStat.Damage: return damage;
            case WeaponStat.FireRate: return fireRate;
            case WeaponStat.BulletSpeed: return bulletSpeed;
            case WeaponStat.Range: return range;
            case WeaponStat.Ammo: return ammo;
            default: return null;
        }
    }

    public static bool IsLowerBetter(WeaponStat stat)
    {
        return stat == WeaponStat.FireRate;
    }

    public float Evaluate(WeaponStat stat, float baseValue, int level)
    {
        StatScaling scaling = Get(stat);
        return scaling != null ? scaling.Evaluate(baseValue, level, IsLowerBetter(stat)) : baseValue;
    }
}
