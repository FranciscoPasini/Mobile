using UnityEngine;
using UnityEngine.Rendering;

public class Base_Bullet : MonoBehaviour
{
    [SerializeField] protected Base_Bullet_Data bulletData;

    [Header("Trail")]
    [SerializeField] private bool showTrail = true;
    [SerializeField, Min(0f), Tooltip("World-space length of the trail behind the bullet.")]
    private float trailTailLength = 1.2f;
    [SerializeField, Min(0.01f)] private float trailWidth = 0.08f;
    [SerializeField] private Color trailColor = new Color(1f, 0.82f, 0.35f, 0.95f);

    protected float speed;
    protected float damage;
    protected Vector3 targetPosition;
    protected GameObject targetObject;
    protected float lifetime;
    protected float lifeTimer;

    private TrailRenderer flightTrail;
    private static Material sharedTrailMaterial;

    protected virtual bool UsesFlightTrail => true;

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
        EnsureFlightTrail();
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
        ReleaseFlightTrail();
        Destroy(gameObject);
    }

    private void EnsureFlightTrail()
    {
        if (!showTrail || !UsesFlightTrail || trailTailLength <= 0f)
        {
            return;
        }

        if (flightTrail == null)
        {
            Transform existing = transform.Find("BulletTrail");
            GameObject trailObject = existing != null ? existing.gameObject : new GameObject("BulletTrail");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = Vector3.zero;
            trailObject.transform.localRotation = Quaternion.identity;

            flightTrail = trailObject.GetComponent<TrailRenderer>();
            if (flightTrail == null)
            {
                flightTrail = trailObject.AddComponent<TrailRenderer>();
            }
        }

        Material material = GetTrailMaterial();
        if (material != null)
        {
            flightTrail.sharedMaterial = material;
        }

        flightTrail.shadowCastingMode = ShadowCastingMode.Off;
        flightTrail.receiveShadows = false;
        flightTrail.alignment = LineAlignment.View;
        flightTrail.textureMode = LineTextureMode.Stretch;
        flightTrail.numCapVertices = 4;
        flightTrail.numCornerVertices = 2;
        flightTrail.minVertexDistance = 0.04f;
        flightTrail.time = trailTailLength / Mathf.Max(speed, 0.01f);
        flightTrail.widthMultiplier = trailWidth;
        flightTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        flightTrail.colorGradient = BuildTrailGradient(trailColor);
        flightTrail.emitting = true;
        flightTrail.Clear();
    }

    private void ReleaseFlightTrail()
    {
        if (flightTrail == null) return;

        float linger = flightTrail.time;
        flightTrail.emitting = false;
        flightTrail.transform.SetParent(null, true);
        Destroy(flightTrail.gameObject, linger + 0.05f);
        flightTrail = null;
    }

    private static Gradient BuildTrailGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Material GetTrailMaterial()
    {
        if (sharedTrailMaterial != null) return sharedTrailMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return null;

        sharedTrailMaterial = new Material(shader);
        return sharedTrailMaterial;
    }
}
