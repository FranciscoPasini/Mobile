using UnityEngine;

public enum UpgradeType {
    PlayerSpeed,
    PlayerPickupRange,
    TownHealth,
    WeaponDamage,
    WeaponFireRate,
    WeaponBulletSpeed,
    WeaponRange,
    WeaponAmmo,
    ExperienceGain,
    PickupDropChance,
    CoinMultiplier,
}

/// <summary>
/// One entry the level-up popup can offer. The level shown on the card is read live
/// from Player_ExperienceAndStats, so it isn't stored here.
/// </summary>
[System.Serializable]
public class Upgrade_Class
{
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private string upgradeName;
    [TextArea(2, 4)]
    [SerializeField] private string upgradeDescription;
    [SerializeField] private Sprite upgradeIcon;

    public UpgradeType UpgradeType => upgradeType;
    public string UpgradeName => upgradeName;
    public string UpgradeDescription => upgradeDescription;
    public Sprite UpgradeIcon => upgradeIcon;

    public Upgrade_Class() { }

    public Upgrade_Class(UpgradeType type, string name, string description)
    {
        upgradeType = type;
        upgradeName = name;
        upgradeDescription = description;
    }
}
