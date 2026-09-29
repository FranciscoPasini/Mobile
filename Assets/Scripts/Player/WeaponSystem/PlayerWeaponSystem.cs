using UnityEngine;
using System;

public class PlayerWeaponSystem : MonoBehaviour
{
    [SerializeField] private Base_Weapon_Data defaultWeaponData;
    [SerializeField] private Transform firePoint;
    [Tooltip("Source of the weapon stat levels. Found automatically if left empty.")]
    [SerializeField] private Player_ExperienceAndStats playerStats;

    private Base_Weapon defaultWeapon;
    private Base_Weapon pickedUpWeapon;

    public Action OnActiveWeaponChanged;
    public Action OnWeaponStatsChanged;

    void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponentInParent<Player_ExperienceAndStats>();
        }

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }

        if (defaultWeaponData == null)
        {
            Debug.LogError("PlayerWeaponSystem: defaultWeaponData is missing, the player cannot shoot.", this);
            return;
        }

        defaultWeapon = defaultWeaponData.CreateRuntimeWeapon(GetStatLevels());
        defaultWeapon.SetIsDefaultWeapon(true);
        defaultWeapon.SetFirePoint(firePoint != null ? firePoint : transform);
        OnActiveWeaponChanged?.Invoke();
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.onPlayerStatsUpgraded += RefreshWeaponStats;
        }

        // Levels may have changed while disabled.
        RefreshWeaponStats();
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.onPlayerStatsUpgraded -= RefreshWeaponStats;
        }
    }

    void Update()
    {
        GetActiveWeapon()?.AutoFire();
    }

    private WeaponStatLevels GetStatLevels()
    {
        return playerStats != null ? playerStats.GetWeaponStatLevels() : default;
    }

    /// <summary>
    /// Both weapons are refreshed, so the default weapon is already up to date
    /// when the picked-up one runs out of ammo.
    /// </summary>
    private void RefreshWeaponStats()
    {
        WeaponStatLevels levels = GetStatLevels();

        defaultWeapon?.ApplyStatLevels(levels);
        pickedUpWeapon?.ApplyStatLevels(levels);

        OnWeaponStatsChanged?.Invoke();
    }

    public void PickUpWeapon(Base_Weapon_Data weaponData)
    {
        if (weaponData == null)
        {
            return;
        }

        PickUpWeapon(weaponData.CreateRuntimeWeapon(GetStatLevels()));
    }

    public void PickUpWeapon(Base_Weapon weapon)
    {
        if (weapon == null)
        {
            return;
        }

        DropPickedUpWeapon(false);

        pickedUpWeapon = weapon;
        pickedUpWeapon.SetIsDefaultWeapon(false);
        pickedUpWeapon.SetFirePoint(firePoint != null ? firePoint : transform);
        pickedUpWeapon.ApplyStatLevels(GetStatLevels());
        pickedUpWeapon.ResetAmmo();
        pickedUpWeapon.onWeaponOutOfAmmo += HandlePickedUpWeaponOutOfAmmo;
        OnActiveWeaponChanged?.Invoke();
    }

    private void HandlePickedUpWeaponOutOfAmmo()
    {
        DropPickedUpWeapon(true);
    }

    private void DropPickedUpWeapon(bool notifyChange)
    {
        if (pickedUpWeapon == null)
        {
            return;
        }

        pickedUpWeapon.onWeaponOutOfAmmo -= HandlePickedUpWeaponOutOfAmmo;
        pickedUpWeapon = null;

        if (notifyChange)
        {
            OnActiveWeaponChanged?.Invoke();
        }
    }

    public Base_Weapon GetActiveWeapon()
    {
        return pickedUpWeapon ?? defaultWeapon;
    }

    public bool HasPickedUpWeapon => pickedUpWeapon != null;

    public void UpdateTarget(GameObject target)
    {
        Base_Weapon activeWeapon = GetActiveWeapon();
        if (activeWeapon == null)
        {
            return;
        }

        if (target == null)
        {
            activeWeapon.SetTarget(Vector3.zero, null);
            return;
        }

        activeWeapon.SetTarget(target.transform.position, target);
    }
}
