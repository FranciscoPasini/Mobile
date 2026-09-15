using UnityEngine;

public class Base_Bullet : MonoBehaviour
{
    [SerializeField] protected Base_Bullet_Data bulletData;

    protected float speed;
    protected int damage;
    protected Vector3 targetPosition;
    protected GameObject targetObject;
    protected float lifetime;
    protected float lifeTimer;

    public virtual void Initialize(
        Base_Bullet_Data data,
        float bulletSpeed,
        int bulletDamage,
        Vector3 targetPos,
        GameObject target)
    {
        bulletData = data;
        speed = bulletSpeed;
        damage = bulletDamage;
        targetPosition = targetPos;
        targetObject = target;
        lifetime = data != null ? data.bulletLifetime : 3f;
        lifeTimer = 0f;

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    protected virtual void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            DestroyBullet();
            return;
        }

        MoveBullet();
    }

    protected virtual void MoveBullet()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (targetObject != null && other.gameObject != targetObject)
        {
            return;
        }

        OnHit(other);
    }

    protected virtual void OnHit(Collider other)
    {
        DestroyBullet();
    }

    protected virtual void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
