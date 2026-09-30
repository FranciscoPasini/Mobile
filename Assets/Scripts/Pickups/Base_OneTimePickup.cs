using UnityEngine;

/// <summary>
/// Dropped pickup with one effect, defined by a child class in <see cref="ApplyEffect"/>.
/// Values (lifetime, heal %, …) live on the matching pickup data asset.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Base_OneTimePickup : MonoBehaviour, IPoolable
{
    public event System.Action<Base_OneTimePickup> Despawned;

    [SerializeField] protected Base_OneTimePickup_Data pickupData;

    [Header("Colliders")]
    [SerializeField] private Collider physicsCollider;
    [SerializeField] private Collider magnetZone;

    [Header("Drop Launch")]
    [SerializeField] private float launchUpSpeed = 5f;
    [SerializeField] private float launchSideSpeed = 1.2f;
    [SerializeField] private LayerMask collideWith = ~0;
    [SerializeField] private float landedSpeed = 0.2f;
    [SerializeField] private float maxDropTime = 3f;

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
    private Building_Town town;
    private Player_ExperienceAndStats playerStats;
    private State state;
    private float lifeTimer;
    private float lifetime = 30f;
    private float blinkWarningTime = 5f;
    private float blinkRateStart = 2f;
    private float blinkRateEnd = 12f;
    private float dropTimer;
    private bool hasTouchedGround;
    private float flySpeed;
    private float flyTimer;
    private float blinkPhase;
    private float baseMagnetRadius;
    private Vector3 baseMagnetSize;

    public bool IsCollecting => state == State.Collecting;
    public Base_OneTimePickup_Data PickupData => pickupData;
    protected Building_Town Town => town;
    protected Transform PlayerTransform => player;
    protected Player_ExperienceAndStats PlayerStats => playerStats;

    /// <summary>
    /// Runs once when the player collects this pickup. Implement in the child class.
    /// </summary>
    protected abstract void ApplyEffect();

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
        ApplyDataSettings();
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void SetTown(Building_Town townBuilding)
    {
        town = townBuilding;
    }

    public void SetPlayerStats(Player_ExperienceAndStats stats)
    {
        playerStats = stats;
    }

    public void BindData(Base_OneTimePickup_Data data)
    {
        pickupData = data;
        ApplyDataSettings();
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
        ApplyDataSettings();

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

    private void ApplyDataSettings()
    {
        if (pickupData == null) return;

        lifetime = Mathf.Max(0.5f, pickupData.lifetime);
        blinkWarningTime = Mathf.Max(0f, pickupData.blinkWarningTime);
        blinkRateStart = Mathf.Max(0.1f, pickupData.blinkRateStart);
        blinkRateEnd = Mathf.Max(0.1f, pickupData.blinkRateEnd);

        if (iconRenderer != null)
        {
            iconRenderer.sprite = pickupData.pickupIcon;
            iconRenderer.enabled = pickupData.pickupIcon != null;
        }
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
        ApplyEffect();
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
