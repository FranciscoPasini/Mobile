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


[System.Serializable]
public class Base_Weapon
{
    public Base_Weapon() { }

    [SerializeField] public Base_Weapon_Data weaponData;
    [SerializeField] private string weaponName;
    [SerializeField] private string weaponDescription;
    [SerializeField] private Sprite weaponIcon;
    [SerializeField] private float weaponBaseDamage;
    [SerializeField] private float weaponBaseRange;
    [SerializeField] private float weaponBaseFireRate;
    [SerializeField] private float weaponBaseBulletSpeed;
    [SerializeField] private int weaponBaseAmmo;
    [SerializeField] private int weaponCurrentAmmo;
    [SerializeField] private Base_Bullet_Data bulletData;

    //The default weapon will have infinite Ammo
    [SerializeField] private bool isDefaultWeapon;

    private Vector3 targetPosition;
    private GameObject targetObject;
    private Transform firePoint;

    private float ShootTimer;


    public Action onWeaponOutOfAmmo;



    public void Initialize(Base_Weapon_Data weaponData)
    {
        this.weaponData = weaponData;
        ExtractWeaponData();
        ResetAmmo();
    }

    public virtual void FireEffect()
    {
        // Override this on child class

    }

    /**
     * Set the target position and object
     * @param targetPosition The position of the target
     * @param targetObject The object of the target
     */
    public void SetTarget(Vector3 targetPosition, GameObject targetObject)
    {
        this.targetPosition = targetPosition;
        this.targetObject = targetObject;
    }

    public void SetFirePoint(Transform firePoint)
    {
        this.firePoint = firePoint;
    }

    private void ExtractWeaponData()
    {
        weaponName = weaponData.weaponName;
        weaponDescription = weaponData.weaponDescription;
        weaponIcon = weaponData.weaponIcon;
        weaponBaseDamage = weaponData.weaponBaseDamage;
        weaponBaseRange = weaponData.weaponBaseRange;
        weaponBaseFireRate = weaponData.weaponBaseFireRate;
        weaponBaseBulletSpeed = weaponData.weaponBaseBulletSpeed;
        bulletData = weaponData.bulletData;
        weaponBaseAmmo = GetWeaponCalculatedBaseAmmo();
    }



    public int GetWeaponCalculatedBaseAmmo()
    {
        // Later implement the functionality that extracts the multiplier from player current stats
        return weaponData != null ? weaponData.weaponBaseAmmo : weaponBaseAmmo;
    }

    public void ResetAmmo()
    {
        weaponCurrentAmmo = GetWeaponCalculatedBaseAmmo();
    }

    public float GetWeaponCalculatedBaseRange()
    {
        // Later implement the functionality that extracts the multiplier from player current stats
        return weaponData != null ? weaponData.weaponBaseRange : weaponBaseRange;
    }

    public float GetWeaponCalculatedBulletSpeed()
    {
        // Later implement the functionality that extracts the multiplier from player current stats
        return weaponData != null ? weaponData.weaponBaseBulletSpeed : weaponBaseBulletSpeed;
    }

    protected void FireBullet()
    {
        if (bulletData == null || !HasValidTarget())
        {
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : Vector3.zero;
        bulletData.Spawn(
            origin,
            targetPosition,
            targetObject,
            GetWeaponCalculatedBulletSpeed(),
            weaponBaseDamage);
            if (weaponData.weaponFireSound != null) {
                AudioSource.PlayClipAtPoint(weaponData.weaponFireSound, origin);
            }
    }


    /// <summary>
    /// A pooled enemy is deactivated instead of destroyed, so a null check is not enough
    /// to tell whether the current target is still worth shooting at.
    /// </summary>
    private bool HasValidTarget()
    {
        return targetObject != null && targetObject.activeInHierarchy;
    }

    public void AutoFire()
    {
        if (!HasValidTarget())
        {
            return;
        }

        if (!isDefaultWeapon && weaponCurrentAmmo <= 0)
        {
            CheckAmmo();
            return;
        }

        ShootTimer += Time.deltaTime;
        if (ShootTimer >= weaponBaseFireRate)
        {
            ShootTimer = 0;
            FireEffect();

            if (!isDefaultWeapon)
            {
                weaponCurrentAmmo--;
                CheckAmmo();
            }
        }
    }

    private void CheckAmmo()
    {
        if (weaponCurrentAmmo <= 0) {
            onWeaponOutOfAmmo?.Invoke();
        }
    }

    public GameObject GetTargetObject()
    {
        return targetObject;
    }

    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }

    public void SetIsDefaultWeapon(bool isDefaultWeapon)
    {
        this.isDefaultWeapon = isDefaultWeapon;
    }
}
