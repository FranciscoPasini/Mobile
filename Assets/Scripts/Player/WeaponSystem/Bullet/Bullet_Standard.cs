using UnityEngine;

public class Bullet_Standard : Base_Bullet
{
    protected override void OnHit(Collider other)
    {
        // Later: apply damage to enemies here
        base.OnHit(other);
    }
}
