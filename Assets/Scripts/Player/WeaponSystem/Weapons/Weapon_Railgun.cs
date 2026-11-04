using UnityEngine;

[System.Serializable]
public class Weapon_Railgun : Base_Weapon
{
    public override void FireEffect()
    {
        FireBullet();
    }
}
