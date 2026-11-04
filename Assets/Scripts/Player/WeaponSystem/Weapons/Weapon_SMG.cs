using UnityEngine;

[System.Serializable]
public class Weapon_SMG : Base_Weapon
{
    public override void FireEffect()
    {
        FireBullet();
    }
}
