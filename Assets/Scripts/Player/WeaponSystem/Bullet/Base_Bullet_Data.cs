using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet", menuName = "Bullet")]
public class Base_Bullet_Data : ScriptableObject
{
    public GameObject bulletPrefab;
    public string bulletName;
    public float bulletLifetime = 3f;

    public Base_Bullet Spawn(Vector3 origin, Vector3 targetPosition, GameObject targetObject, float speed, int damage)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"{name} has no bullet prefab assigned.");
            return null;
        }

        Vector3 direction = targetPosition - origin;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        GameObject bulletObject = Object.Instantiate(bulletPrefab, origin, rotation);

        Base_Bullet bullet = bulletObject.GetComponent<Base_Bullet>();
        if (bullet == null)
        {
            Debug.LogError($"{name} prefab '{bulletPrefab.name}' is missing a Base_Bullet component.");
            Object.Destroy(bulletObject);
            return null;
        }

        bullet.Initialize(this, speed, damage, targetPosition, targetObject);
        return bullet;
    }
}
