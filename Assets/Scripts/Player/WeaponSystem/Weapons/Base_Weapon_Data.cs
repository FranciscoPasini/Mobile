using UnityEngine;
using System;
using NaughtyAttributes;

public class WeaponClassAttribute : PropertyAttribute { }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon")]
public class Base_Weapon_Data : ScriptableObject
{
    [WeaponClass]
    [SerializeReference] public Base_Weapon weaponClass;
    public string weaponName;
    public string weaponDescription;
    public Sprite weaponIcon;
    public float weaponBaseDamage;
    public float weaponBaseRange;
    public float weaponBaseFireRate;
    public float weaponBaseBulletSpeed;
    public int weaponBaseAmmo;
    public AudioClip weaponFireSound;
    public Base_Bullet_Data bulletData;

    [Header("Level Scaling")]
    [Tooltip("How each stat grows with the player's upgrade level for that stat.")]
    public WeaponScaling scaling = new WeaponScaling();

    public Base_Weapon CreateRuntimeWeapon(WeaponStatLevels statLevels = default)
    {
        Type type = weaponClass != null ? weaponClass.GetType() : typeof(Base_Weapon);
        Base_Weapon weapon = (Base_Weapon)Activator.CreateInstance(type);
        // Clone so weapon-specific fields (pellet count, etc.) come from the asset.
        if (weaponClass != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(weaponClass), weapon);
        }
        weapon.Initialize(this, statLevels);
        return weapon;
    }

    [Button("Log Scaling Preview")]
    private void LogScalingPreview()
    {
        int[] previewLevels = { 0, 1, 2, 3, 5, 10, 15, 20, 30, 50, 100 };
        var log = new System.Text.StringBuilder($"{name} scaling (stat level: value):\n");

        foreach (int level in previewLevels)
        {
            float fireRate = scaling.Evaluate(WeaponStat.FireRate, weaponBaseFireRate, level);
            float damage = scaling.Evaluate(WeaponStat.Damage, weaponBaseDamage, level);
            float dps = fireRate > 0f ? damage / fireRate : 0f;

            log.AppendLine(
                $"Lv {level}: damage {damage:0.##}, every {fireRate:0.###}s, " +
                $"range {scaling.Evaluate(WeaponStat.Range, weaponBaseRange, level):0.#}, " +
                $"speed {scaling.Evaluate(WeaponStat.BulletSpeed, weaponBaseBulletSpeed, level):0.#}, " +
                $"ammo {Mathf.RoundToInt(scaling.Evaluate(WeaponStat.Ammo, weaponBaseAmmo, level))}, " +
                $"dps if damage and fire rate share this level {dps:0.##}");
        }

        Debug.Log(log.ToString(), this);
    }
}
