using UnityEngine;

[CreateAssetMenu(fileName = "Electric Zone", menuName = "Purchasable Building/Electric Zone")]
public class Building_ElectricZone_Data : Building_Purchaseable_Data
{
    [Header("Zap")]
    [Tooltip("Damage per pulse at purchase (level 1).")]
    [Min(0f)] public float baseDamage = 5f;

    [Tooltip("Seconds between pulses at purchase. Lower is faster.")]
    [Min(0.1f)] public float baseElectrifyInterval = 2f;

    [Tooltip("How damage grows with each upgrade after purchase.")]
    public StatScaling damageScaling = new StatScaling(ScalingMethod.Linear, 0.25f, 0f);

    [Tooltip("How much faster pulses get. Linear, and Building_ElectricZone stops applying it after maxElectrifyUpgrades.")]
    public StatScaling electrifySpeedScaling = new StatScaling(ScalingMethod.Linear, 0.2f, 2f);

    [Tooltip("Electrify speed ignores upgrades past this. Damage still follows the cost list.")]
    [Min(1)] public int maxElectrifyUpgrades = 5;

    public float GetDamage(int upgradeLevel)
    {
        return damageScaling.Evaluate(baseDamage, Mathf.Max(0, upgradeLevel), false);
    }

    public float GetElectrifyInterval(int upgradeLevel)
    {
        int speedLevel = Mathf.Clamp(upgradeLevel, 0, maxElectrifyUpgrades);
        return Mathf.Max(0.1f, electrifySpeedScaling.Evaluate(baseElectrifyInterval, speedLevel, true));
    }
}
