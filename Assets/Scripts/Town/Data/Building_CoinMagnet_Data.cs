using UnityEngine;

[CreateAssetMenu(fileName = "Coin Magnet", menuName = "Purchasable Building/Coin Magnet")]
public class Building_CoinMagnet_Data : Building_Purchaseable_Data
{
    [Header("Magnet")]
    [Tooltip("Collection radius at purchase (level 1).")]
    [Min(0.1f)] public float baseRange = 5f;

    [Tooltip("How range grows with each upgrade after purchase.")]
    public StatScaling rangeScaling = new StatScaling(ScalingMethod.Linear, 0.2f, 2f);

    [Tooltip("Range ignores upgrades past this.")]
    [Min(1)] public int maxRangeUpgrades = 5;

    public float GetRange(int upgradeLevel)
    {
        int rangeLevel = Mathf.Clamp(upgradeLevel, 0, maxRangeUpgrades);
        return Mathf.Max(0.1f, rangeScaling.Evaluate(baseRange, rangeLevel, false));
    }
}
