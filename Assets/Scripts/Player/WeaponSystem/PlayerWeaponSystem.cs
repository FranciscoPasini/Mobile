using UnityEngine;
using System;

public class PlayerWeaponSystem : MonoBehaviour
{
    [SerializeField] private Base_Weapon_Data defaultWeaponData;
    [SerializeField] private Transform firePoint;

    private Base_Weapon defaultWeapon;
    private Base_Weapon pickedUpWeapon;

    public Action OnActiveWeaponChanged;

    void Awake()
    {
        if (defaultWeaponData == null)
        {
            return;
        }

        defaultWeapon = defaultWeaponData.CreateRuntimeWeapon();
        defaultWeapon.SetIsDefaultWeapon(true);
        defaultWeapon.SetFirePoint(firePoint != null ? firePoint : transform);
        OnActiveWeaponChanged?.Invoke();
    }

    void Update()
    {
        GetActiveWeapon()?.AutoFire();
    }

    public void PickUpWeapon(Base_Weapon_Data weaponData)
    {
        if (weaponData == null)
        {
            return;
        }

        PickUpWeapon(weaponData.CreateRuntimeWeapon());
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
