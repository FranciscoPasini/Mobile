using UnityEngine;
using System;

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

    //STATS AFTER APPLYING THE PLAYER'S UPGRADE LEVELS
    private float weaponDamage;
    private float weaponRange;
    private float weaponFireRate;
    private float weaponBulletSpeed;
    private int weaponMaxAmmo;
    private WeaponStatLevels statLevels;

    private Vector3 targetPosition;
    private GameObject targetObject;
    private Transform firePoint;

    private float ShootTimer;


    public Action onWeaponOutOfAmmo;



    public void Initialize(Base_Weapon_Data weaponData, WeaponStatLevels statLevels = default)
    {
        this.weaponData = weaponData;
        ExtractWeaponData();
        ApplyStatLevels(statLevels);
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
        weaponBaseAmmo = weaponData.weaponBaseAmmo;
        bulletData = weaponData.bulletData;
    }

    /// <summary>
    /// Recalculates every stat from the base values and the player's upgrade levels.
    /// Safe to call at any time; a bigger magazine adds the extra rounds without refilling.
    /// </summary>
    public void ApplyStatLevels(WeaponStatLevels statLevels)
    {
        this.statLevels = statLevels;
        int previousMaxAmmo = weaponMaxAmmo;

        weaponDamage = CalculateStat(WeaponStat.Damage, weaponBaseDamage);
        weaponRange = CalculateStat(WeaponStat.Range, weaponBaseRange);
        weaponFireRate = CalculateStat(WeaponStat.FireRate, weaponBaseFireRate);
        weaponBulletSpeed = CalculateStat(WeaponStat.BulletSpeed, weaponBaseBulletSpeed);
        weaponMaxAmmo = Mathf.Max(0, Mathf.RoundToInt(CalculateStat(WeaponStat.Ammo, weaponBaseAmmo)));

        if (previousMaxAmmo > 0)
        {
            weaponCurrentAmmo = Mathf.Clamp(weaponCurrentAmmo + weaponMaxAmmo - previousMaxAmmo, 0, weaponMaxAmmo);
        }
    }

    private float CalculateStat(WeaponStat stat, float baseValue)
    {
        WeaponScaling scaling = weaponData != null ? weaponData.scaling : null;
        return scaling != null ? scaling.Evaluate(stat, baseValue, statLevels.Get(stat)) : baseValue;
    }

    public int GetWeaponCalculatedBaseAmmo()
    {
        return weaponMaxAmmo;
    }

    public void ResetAmmo()
    {
        weaponCurrentAmmo = GetWeaponCalculatedBaseAmmo();
    }

    public float GetWeaponCalculatedBaseRange()
    {
        return weaponRange;
    }

    public float GetWeaponCalculatedBulletSpeed()
    {
        return weaponBulletSpeed;
    }

    public float GetWeaponCalculatedDamage()
    {
        return weaponDamage;
    }

    public float GetWeaponCalculatedFireRate()
    {
        return weaponFireRate;
    }

    public int GetCurrentAmmo()
    {
        return weaponCurrentAmmo;
    }

    protected bool HasValidTarget()
    {
        return targetObject != null && targetObject.activeInHierarchy;
    }

    protected Vector3 GetFireOrigin()
    {
        return firePoint != null ? firePoint.position : Vector3.zero;
    }

    protected Vector3 GetAimDirection()
    {
        Vector3 origin = GetFireOrigin();
        Vector3 toTarget = targetPosition - origin;
        if (toTarget.sqrMagnitude > 0.0001f) return toTarget.normalized;
        return firePoint != null ? firePoint.forward : Vector3.forward;
    }

    protected void FireBullet()
    {
        FireBullet(GetFireOrigin(), targetPosition, targetObject, true);
    }

    protected void FireBulletInDirection(Vector3 direction, bool playSound = false)
    {
        Vector3 origin = GetFireOrigin();
        if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.forward;
        FireBullet(origin, origin + direction.normalized * 20f, null, playSound);
    }

    protected void FireBullet(Vector3 origin, Vector3 aimPoint, GameObject target, bool playSound)
    {
        if (bulletData == null)
        {
            return;
        }

        bulletData.Spawn(
            origin,
            aimPoint,
            target,
            GetWeaponCalculatedBulletSpeed(),
            GetWeaponCalculatedDamage());

        if (playSound) PlayFireSound(origin);
    }

    protected void PlayFireSound(Vector3 origin)
    {
        if (weaponData != null && weaponData.weaponFireSound != null)
        {
            AudioSource.PlayClipAtPoint(weaponData.weaponFireSound, origin);
        }
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
        if (ShootTimer >= weaponFireRate)
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
