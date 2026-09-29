using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WeaponPickup : MonoBehaviour, IPoolable
{
    public event System.Action<WeaponPickup> Despawned;

    [Header("Colliders")]
    [SerializeField] private Collider physicsCollider;
    [SerializeField] private Collider magnetZone;

    [Header("Drop Launch")]
    [SerializeField] private float launchUpSpeed = 5f;
    [SerializeField] private float launchSideSpeed = 1.2f;
    [SerializeField] private LayerMask collideWith = ~0;
    [SerializeField] private float landedSpeed = 0.2f;
    [SerializeField] private float maxDropTime = 3f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 12f;
    [SerializeField, Min(0f)] private float blinkWarningTime = 5f;
    [SerializeField, Min(0.1f)] private float blinkRateStart = 2f;
    [SerializeField, Min(0.1f)] private float blinkRateEnd = 12f;

    [Header("Fly To Player")]
    [SerializeField] private float flyStartSpeed = 6f;
    [SerializeField] private float flyAcceleration = 40f;
    [SerializeField] private float playerHeightOffset = 1f;
    [SerializeField] private float maxFlyTime = 3f;

    [Header("Visuals")]
    [SerializeField] private Renderer pickupRenderer;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private float spinSpeed = 140f;

    private enum State { Dropping, Resting, Collecting }

    private Rigidbody body;
    private Transform player;
    private PlayerWeaponSystem weaponSystem;
    private Base_Weapon_Data weaponData;
    private State state;
    private float lifeTimer;
    private float dropTimer;
    private bool hasTouchedGround;
    private float flySpeed;
    private float flyTimer;
    private float blinkPhase;
    private float baseMagnetRadius;
    private Vector3 baseMagnetSize;

    public bool IsCollecting => state == State.Collecting;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.excludeLayers = ~collideWith;

        foreach (Collider candidate in GetComponentsInChildren<Collider>())
        {
            if (candidate == magnetZone || candidate == physicsCollider) continue;
            if (magnetZone == null && candidate.isTrigger) magnetZone = candidate;
            else if (physicsCollider == null && !candidate.isTrigger) physicsCollider = candidate;
        }

        if (magnetZone != null) magnetZone.isTrigger = true;
        if (physicsCollider != null) physicsCollider.isTrigger = false;
        if (pickupRenderer == null) pickupRenderer = GetComponentInChildren<Renderer>();
        if (iconRenderer == null) iconRenderer = GetComponentInChildren<SpriteRenderer>();

        CacheMagnetSize();
    }

    public void SetPlayer(Transform playerTransform, PlayerWeaponSystem system)
    {
        player = playerTransform;
        weaponSystem = system;
    }

    public void SetWeapon(Base_Weapon_Data data)
    {
        weaponData = data;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = data != null ? data.weaponIcon : null;
            iconRenderer.enabled = iconRenderer.sprite != null;
        }
    }

    public void SetPickupRangeMultiplier(float multiplier)
    {
        multiplier = Mathf.Max(0f, multiplier);
        switch (magnetZone)
        {
            case SphereCollider sphere: sphere.radius = baseMagnetRadius * multiplier; break;
            case CapsuleCollider capsule: capsule.radius = baseMagnetRadius * multiplier; break;
            case BoxCollider box: box.size = new Vector3(baseMagnetSize.x * multiplier, baseMagnetSize.y, baseMagnetSize.z * multiplier); break;
        }
    }

    public void Spawn(Vector3 position)
    {
        transform.position = position;
        lifeTimer = 0f;
        dropTimer = 0f;
        hasTouchedGround = false;
        flySpeed = 0f;
        flyTimer = 0f;
        blinkPhase = 0f;
        SetVisible(true);
        state = State.Dropping;

        gameObject.SetActive(true);
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.position = position;

        Vector2 side = Random.insideUnitCircle * launchSideSpeed;
        body.linearVelocity = new Vector3(side.x, launchUpSpeed, side.y);
        body.angularVelocity = Vector3.zero;
    }

    public void Despawn()
    {
        if (!gameObject.activeSelf) return;

        FreezeBody();
        SetVisible(true);
        gameObject.SetActive(false);
        Despawned?.Invoke(this);
    }

    public void Collect()
    {
        if (state == State.Collecting) return;

        if (player == null)
        {
            Award();
            return;
        }

        FreezeBody();
        SetVisible(true);
        state = State.Collecting;
        flySpeed = flyStartSpeed;
        flyTimer = 0f;
    }

    private void Update()
    {
        if (state == State.Collecting)
        {
            FlyToPlayer();
            return;
        }

        if (state == State.Dropping)
        {
            dropTimer += Time.deltaTime;
            bool settled = hasTouchedGround && body.linearVelocity.sqrMagnitude <= landedSpeed * landedSpeed;
            if (settled || dropTimer >= maxDropTime)
            {
                FreezeBody();
                state = State.Resting;
            }
        }
        else
        {
            if (spinSpeed != 0f) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            if (IsPlayerInMagnet())
            {
                Collect();
                return;
            }
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Despawn();
            return;
        }

        UpdateBlink();
    }

    private void OnCollisionEnter(Collision collision)
    {
        hasTouchedGround = true;
    }

    private void FreezeBody()
    {
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        body.interpolation = RigidbodyInterpolation.None;
    }

    private void FlyToPlayer()
    {
        if (player == null)
        {
            Award();
            return;
        }

        flyTimer += Time.deltaTime;
        flySpeed += flyAcceleration * Time.deltaTime;
        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * Time.deltaTime);

        if (transform.position == target || flyTimer >= maxFlyTime)
        {
            Award();
        }
    }

    private void Award()
    {
        if (weaponSystem != null && weaponData != null)
        {
            weaponSystem.PickUpWeapon(weaponData);
        }

        Despawn();
    }

    private bool IsPlayerInMagnet()
    {
        if (player == null || magnetZone == null || !magnetZone.enabled) return false;

        Vector3 point = player.position;
        point.y = magnetZone.bounds.center.y;
        return (magnetZone.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
    }

    private void CacheMagnetSize()
    {
        switch (magnetZone)
        {
            case SphereCollider sphere: baseMagnetRadius = sphere.radius; break;
            case CapsuleCollider capsule: baseMagnetRadius = capsule.radius; break;
            case BoxCollider box: baseMagnetSize = box.size; break;
        }
    }

    private void UpdateBlink()
    {
        float timeLeft = lifetime - lifeTimer;
        if (timeLeft > blinkWarningTime || blinkWarningTime <= 0f)
        {
            SetVisible(true);
            return;
        }

        float urgency = 1f - timeLeft / blinkWarningTime;
        float blinkRate = Mathf.Lerp(blinkRateStart, blinkRateEnd, urgency);
        blinkPhase += blinkRate * Time.deltaTime;
        SetVisible(blinkPhase % 1f < 0.5f);
    }

    private void SetVisible(bool visible)
    {
        if (pickupRenderer != null && pickupRenderer.enabled != visible) pickupRenderer.enabled = visible;
        if (iconRenderer != null && iconRenderer.sprite != null && iconRenderer.enabled != visible) iconRenderer.enabled = visible;
    }
}
