using System;
using UnityEngine;

public class Player_ExperienceAndStats : MonoBehaviour
{


    [Header("References")]
    [SerializeField] private Building_Town TownBuilding;

    [NaughtyAttributes.HorizontalLine(3, NaughtyAttributes.EColor.White)]
    [Header("Player Level")]
    [SerializeField, Min(1)] private int currentPlayerLevel = 1;
    [Tooltip("Calculated from the curve below, editing it has no effect.")]
    [SerializeField, NaughtyAttributes.ReadOnly] private int experienceToNextLevel;
    [SerializeField] private int currentPlayerExperience;
    [SerializeField] private ExperienceCurve experienceCurve = new ExperienceCurve();

    [NaughtyAttributes.HorizontalLine(3, NaughtyAttributes.EColor.White)]
    [Header("Town Stats")]
    [SerializeField, Min(0)] private int townHealthLevel;
    [Tooltip("+15% of the town's starting max health per level, no cap. The added max health is also healed.")]
    [SerializeField] private StatScaling townHealthScaling = new StatScaling(ScalingMethod.Linear, 0.15f, 0f);
    [SerializeField, NaughtyAttributes.ReadOnly] private int currentTownMaxHealth;

    [NaughtyAttributes.HorizontalLine(3, NaughtyAttributes.EColor.White)]
    [Header("Player Weapon Stats")]
    [Tooltip("Upgrade level per weapon stat. Each weapon data asset decides how a level turns into stats.")]
    [SerializeField] private WeaponStatLevels weaponStatLevels;

    [NaughtyAttributes.HorizontalLine(3, NaughtyAttributes.EColor.White)]
    [Header("Player Stats")]
    [SerializeField, Min(0)] private int moveSpeedLevel;
    [Tooltip("Approaches 1.6x the base speed. Much faster and the joystick gets hard to control.")]
    [SerializeField] private StatScaling moveSpeedScaling = new StatScaling(ScalingMethod.Asymptotic, 0.1f, 1.6f);
    [SerializeField, Min(0)] private int pickupRangeLevel;
    [Tooltip("Multiplies the coin magnet zones. Approaches 2.5x, beyond that coins get vacuumed from across the map.")]
    [SerializeField] private StatScaling pickupRangeScaling = new StatScaling(ScalingMethod.Asymptotic, 0.12f, 2.5f);
    [SerializeField, Min(0)] private int expMultiplierLevel;
    [Tooltip("Approaches 3x experience from kills. High levels let you climb the XP curve much faster.")]
    [SerializeField] private StatScaling expMultiplierScaling = new StatScaling(ScalingMethod.Asymptotic, 0.12f, 3f);
    [SerializeField, NaughtyAttributes.ReadOnly] private float currentExpMultiplier = 1f;
    [SerializeField, Min(0)] private int pickupDropChanceLevel;
    [Tooltip("Adds to the one-time pickup drop chance. +3% per level, capped at +15%.")]
    [SerializeField] private StatScaling pickupDropChanceScaling = new StatScaling(ScalingMethod.Linear, 0.03f, 1.15f);
    [SerializeField, Min(0)] private int coinMultiplierLevel;
    [Tooltip("Starts at 1x coin value. Change method/rate/max here to tune how it grows.")]
    [SerializeField] private StatScaling coinMultiplierScaling = new StatScaling(ScalingMethod.Linear, 0.15f, 0f);
    [SerializeField, NaughtyAttributes.ReadOnly] private float currentCoinMultiplier = 1f;


    public Action onPlayerLevelUp;
    public Action onPlayerStatsUpgraded;
    public Action onTownStatsUpgraded;
    /// <summary>Current experience, experience needed for the next level.</summary>
    public Action<int, int> onExperienceChanged;

    private int baseTownMaxHealth;


    private void Awake()
    {
        currentPlayerLevel = Mathf.Max(1, currentPlayerLevel);
        experienceToNextLevel = experienceCurve.GetExperienceToNextLevel(currentPlayerLevel);
        RefreshExperienceMultiplier();
        RefreshCoinMultiplier();

        if (TownBuilding == null)
        {
            TownBuilding = FindFirstObjectByType<Building_Town>();
        }

        // Scaling always starts from the town's own max health, so levels never compound.
        if (TownBuilding != null)
        {
            baseTownMaxHealth = TownBuilding.maxHealth;
            ApplyTownHealth();
        }
    }


    #region Experience Functions

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        int granted = Mathf.Max(1, Mathf.RoundToInt(amount * currentExpMultiplier));
        currentPlayerExperience += granted;

        // One big reward can be worth several levels, leftover carries into the next one.
        while (currentPlayerExperience >= experienceToNextLevel)
        {
            currentPlayerExperience -= experienceToNextLevel;
            currentPlayerLevel++;
            experienceToNextLevel = experienceCurve.GetExperienceToNextLevel(currentPlayerLevel);
            onPlayerLevelUp?.Invoke();
        }

        onExperienceChanged?.Invoke(currentPlayerExperience, experienceToNextLevel);
    }

    [NaughtyAttributes.Button("Add 100 Experience")]
    private void DebugAddExperience() => AddExperience(100);

    [NaughtyAttributes.Button("Log Experience Curve")]
    private void LogExperienceCurve()
    {
        var log = new System.Text.StringBuilder($"{name} experience curve ({experienceCurve.method}):\n");
        long total = 0;

        for (int level = 1; level <= 50; level++)
        {
            int needed = experienceCurve.GetExperienceToNextLevel(level);
            total += needed;
            log.AppendLine($"Lv {level} -> {level + 1}: {needed} xp (total {total})");
        }

        Debug.Log(log.ToString(), this);
    }
    #endregion


    #region Level Up Choices

    public void ApplyUpgrade(UpgradeType upgrade)
    {
        switch (upgrade)
        {
            case UpgradeType.WeaponDamage: UpgradeWeaponStat(WeaponStat.Damage); break;
            case UpgradeType.WeaponFireRate: UpgradeWeaponStat(WeaponStat.FireRate); break;
            case UpgradeType.WeaponBulletSpeed: UpgradeWeaponStat(WeaponStat.BulletSpeed); break;
            case UpgradeType.WeaponRange: UpgradeWeaponStat(WeaponStat.Range); break;
            case UpgradeType.WeaponAmmo: UpgradeWeaponStat(WeaponStat.Ammo); break;
            case UpgradeType.PlayerSpeed: UpgradePlayerSpeed(); break;
            case UpgradeType.PlayerPickupRange: UpgradePlayerPickupRange(); break;
            case UpgradeType.TownHealth: UpgradeTownHealth(); break;
            case UpgradeType.ExperienceGain: UpgradeExperienceGain(); break;
            case UpgradeType.PickupDropChance: UpgradePickupDropChance(); break;
            case UpgradeType.CoinMultiplier: UpgradeCoinMultiplier(); break;
        }
    }
    #endregion


    #region Town Stat Functions

    public void UpgradeTownHealth(int levels = 1)
    {
        if (levels <= 0) return;

        townHealthLevel += levels;
        ApplyTownHealth();
        onTownStatsUpgraded?.Invoke();
    }

    /// <summary>
    /// Sets the town's max health for the current level and heals exactly the amount that was added.
    /// </summary>
    private void ApplyTownHealth()
    {
        if (TownBuilding == null || baseTownMaxHealth <= 0) return;

        int newMaxHealth = Mathf.RoundToInt(townHealthScaling.Evaluate(baseTownMaxHealth, townHealthLevel, false));
        int addedHealth = newMaxHealth - TownBuilding.maxHealth;
        currentTownMaxHealth = newMaxHealth;

        if (addedHealth == 0) return;

        TownBuilding.SetMaxHealth(newMaxHealth);
        if (addedHealth > 0)
        {
            TownBuilding.Heal(addedHealth); //THIS ALREADY INVOKES IN BUILDING_TOWN
        }
        else if (TownBuilding.currentHealth > newMaxHealth)
        {
            TownBuilding.currentHealth = newMaxHealth;
        }
    }

    [NaughtyAttributes.Button("Upgrade Town Health")]
    private void upgradeTownHealth() => UpgradeTownHealth();
    #endregion


    #region Player Stat Functions


    public void UpgradeWeaponStat(WeaponStat stat, int levels = 1)
    {
        if (levels == 0) return;

        weaponStatLevels.Add(stat, levels);
        onPlayerStatsUpgraded?.Invoke();
    }

    public void UpgradePlayerSpeed(int levels = 1)
    {
        if (levels <= 0) return;

        moveSpeedLevel += levels;
        onPlayerStatsUpgraded?.Invoke();
    }

    public void UpgradePlayerPickupRange(int levels = 1)
    {
        if (levels <= 0) return;

        pickupRangeLevel += levels;
        onPlayerStatsUpgraded?.Invoke();
    }

    public void UpgradeExperienceGain(int levels = 1)
    {
        if (levels <= 0) return;

        expMultiplierLevel += levels;
        RefreshExperienceMultiplier();
        onPlayerStatsUpgraded?.Invoke();
    }

    public void UpgradePickupDropChance(int levels = 1)
    {
        if (levels <= 0) return;

        pickupDropChanceLevel += levels;
        onPlayerStatsUpgraded?.Invoke();
    }

    public void UpgradeCoinMultiplier(int levels = 1)
    {
        if (levels <= 0) return;

        coinMultiplierLevel += levels;
        RefreshCoinMultiplier();
        onPlayerStatsUpgraded?.Invoke();
    }

    private void RefreshExperienceMultiplier()
    {
        currentExpMultiplier = expMultiplierScaling.GetFactor(expMultiplierLevel);
    }

    private void RefreshCoinMultiplier()
    {
        currentCoinMultiplier = coinMultiplierScaling.GetFactor(coinMultiplierLevel);
    }

    [NaughtyAttributes.Button("Upgrade Damage")]
    private void upgradePlayerDamage() => UpgradeWeaponStat(WeaponStat.Damage);

    [NaughtyAttributes.Button("Upgrade Fire Rate")]
    private void upgradePlayerFireRate() => UpgradeWeaponStat(WeaponStat.FireRate);

    [NaughtyAttributes.Button("Upgrade Bullet Speed")]
    private void upgradePlayerBulletSpeed() => UpgradeWeaponStat(WeaponStat.BulletSpeed);

    [NaughtyAttributes.Button("Upgrade Range")]
    private void upgradePlayerRange() => UpgradeWeaponStat(WeaponStat.Range);

    [NaughtyAttributes.Button("Upgrade Ammo")]
    private void upgradePlayerAmmo() => UpgradeWeaponStat(WeaponStat.Ammo);

    [NaughtyAttributes.Button("Upgrade Move Speed")]
    private void upgradePlayerSpeed() => UpgradePlayerSpeed();

    [NaughtyAttributes.Button("Upgrade Pickup Range")]
    private void upgradePlayerPickupRange() => UpgradePlayerPickupRange();

    [NaughtyAttributes.Button("Upgrade Experience Gain")]
    private void upgradeExperienceGain() => UpgradeExperienceGain();

    [NaughtyAttributes.Button("Upgrade Pickup Drop Chance")]
    private void upgradePickupDropChance() => UpgradePickupDropChance();

    [NaughtyAttributes.Button("Upgrade Coin Multiplier")]
    private void upgradeCoinMultiplier() => UpgradeCoinMultiplier();

    // Lets values typed into the inspector during play mode take effect.
    private void OnValidate()
    {
        currentPlayerLevel = Mathf.Max(1, currentPlayerLevel);
        experienceToNextLevel = experienceCurve.GetExperienceToNextLevel(currentPlayerLevel);
        RefreshExperienceMultiplier();
        RefreshCoinMultiplier();

        if (!Application.isPlaying) return;

        ApplyTownHealth();
        onPlayerStatsUpgraded?.Invoke();
    }
    #endregion

    #region Getter
    public int GetCurrentPlayerLevel()
    {
        return currentPlayerLevel;
    }
    public int GetCurrentPlayerExperience()
    {
        return currentPlayerExperience;
    }
    public int GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }
    public WeaponStatLevels GetWeaponStatLevels()
    {
        return weaponStatLevels;
    }
    public int GetWeaponStatLevel(WeaponStat stat)
    {
        return weaponStatLevels.Get(stat);
    }
    public float GetMoveSpeedMultiplier()
    {
        return moveSpeedScaling.GetFactor(moveSpeedLevel);
    }
    public float GetPickupRangeMultiplier()
    {
        return pickupRangeScaling.GetFactor(pickupRangeLevel);
    }

    /// <summary>
    /// Extra drop chance added to the one-time pickup pool (0.03 = +3%, capped at 0.15).
    /// </summary>
    public float GetPickupDropChanceBonus()
    {
        return pickupDropChanceScaling.GetFactor(pickupDropChanceLevel) - 1f;
    }

    public float GetCoinMultiplier()
    {
        return currentCoinMultiplier;
    }
    public int GetUpgradeLevel(UpgradeType upgrade)
    {
        switch (upgrade)
        {
            case UpgradeType.WeaponDamage: return weaponStatLevels.damage;
            case UpgradeType.WeaponFireRate: return weaponStatLevels.fireRate;
            case UpgradeType.WeaponBulletSpeed: return weaponStatLevels.bulletSpeed;
            case UpgradeType.WeaponRange: return weaponStatLevels.range;
            case UpgradeType.WeaponAmmo: return weaponStatLevels.ammo;
            case UpgradeType.PlayerSpeed: return moveSpeedLevel;
            case UpgradeType.PlayerPickupRange: return pickupRangeLevel;
            case UpgradeType.TownHealth: return townHealthLevel;
            case UpgradeType.ExperienceGain: return expMultiplierLevel;
            case UpgradeType.PickupDropChance: return pickupDropChanceLevel;
            case UpgradeType.CoinMultiplier: return coinMultiplierLevel;
            default: return 0;
        }
    }

    /// <summary>
    /// True when this upgrade has a hard cap and another level would not improve it.
    /// </summary>
    public bool IsUpgradeCapped(UpgradeType upgrade)
    {
        switch (upgrade)
        {
            case UpgradeType.PlayerSpeed: return moveSpeedScaling.IsCapped(moveSpeedLevel);
            case UpgradeType.PlayerPickupRange: return pickupRangeScaling.IsCapped(pickupRangeLevel);
            case UpgradeType.TownHealth: return townHealthScaling.IsCapped(townHealthLevel);
            case UpgradeType.ExperienceGain: return expMultiplierScaling.IsCapped(expMultiplierLevel);
            case UpgradeType.PickupDropChance: return pickupDropChanceScaling.IsCapped(pickupDropChanceLevel);
            case UpgradeType.CoinMultiplier: return coinMultiplierScaling.IsCapped(coinMultiplierLevel);
            default: return false;
        }
    }

    public void HealTownPercent(float percent)
    {
        if (TownBuilding == null || percent <= 0f) return;

        float amount = TownBuilding.maxHealth * percent;
        if (amount <= 0f) return;

        TownBuilding.Heal(amount);
    }
    #endregion
}
