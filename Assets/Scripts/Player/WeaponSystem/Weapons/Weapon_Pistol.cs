using UnityEngine;

[System.Serializable]
public class Weapon_Pistol : Base_Weapon
{
    public override void FireEffect()
    {
        FireBullet();
    }
}
