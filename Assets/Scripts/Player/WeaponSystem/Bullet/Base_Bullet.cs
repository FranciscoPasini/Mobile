using UnityEngine;

public class Base_Bullet : MonoBehaviour
{
    [SerializeField] protected Base_Bullet_Data bulletData;

    protected float speed;
    protected float damage;
    protected Vector3 targetPosition;
    protected GameObject targetObject;
    protected float lifetime;
    protected float lifeTimer;

    public virtual void Initialize(
        Base_Bullet_Data data,
        float bulletSpeed,
        float bulletDamage,
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
        if (ShouldIgnoreCollider(other))
        {
            return;
        }

        OnHit(other);
    }

    protected virtual bool ShouldIgnoreCollider(Collider other)
    {
        if (other == null) return true;
        if (other.CompareTag("Player")) return true;
        if (targetObject != null && other.gameObject != targetObject) return true;
        return false;
    }

    protected bool TryGetEnemy(Collider other, out Base_Enemy enemy)
    {
        enemy = other != null ? other.GetComponentInParent<Base_Enemy>() : null;
        return enemy != null && enemy.IsAlive;
    }

    protected void DealDamage(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>()
            ?? other.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);
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
