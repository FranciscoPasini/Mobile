using UnityEngine;
using System;

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

    public Base_Weapon CreateRuntimeWeapon()
    {
        Type type = weaponClass != null ? weaponClass.GetType() : typeof(Base_Weapon);
        Base_Weapon weapon = (Base_Weapon)Activator.CreateInstance(type);
        weapon.Initialize(this);
        return weapon;
    }
}
