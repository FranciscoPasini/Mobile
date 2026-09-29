using UnityEngine;

public class Weapon_Shotgun : Base_Weapon
{
    [SerializeField, Min(1)] private int pelletCount = 6;
    [Tooltip("Total cone width in degrees. Pellets are scattered inside this.")]
    [SerializeField, Min(0f)] private float spreadAngle = 22f;

    public override void FireEffect()
    {
        Vector3 forward = GetAimDirection();
        Vector3 origin = GetFireOrigin();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();
        Vector3 up = Vector3.Cross(forward, right);

        int pellets = Mathf.Max(1, pelletCount);
        for (int i = 0; i < pellets; i++)
        {
            float yaw = Random.Range(-spreadAngle, spreadAngle) * 0.5f;
            float pitch = Random.Range(-spreadAngle, spreadAngle) * 0.25f;
            Vector3 direction = Quaternion.AngleAxis(yaw, up) * Quaternion.AngleAxis(pitch, right) * forward;
            FireBulletInDirection(direction, false);
        }

        PlayFireSound(origin);
    }
}
