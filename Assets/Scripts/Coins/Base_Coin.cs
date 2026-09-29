using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;

public enum CoinType
{
    Copper = 1,
    Silver = 5,
    Gold = 10,
    Diamond = 25,
}

[System.Serializable]
public class CoinTypeVisual
{
    public CoinType coinType;
    public Material material;
}

[RequireComponent(typeof(Rigidbody))]
public class Base_Coin : MonoBehaviour, IPoolable
{
    private enum CoinState
    {
        Dropping,
        Resting,
        Collecting,
    }

    public event System.Action<Base_Coin> Despawned;

    [SerializeField] private CoinType coinType;
    [SerializeField] private int coinValue;

    [Header("Colliders")]
    [Tooltip("Solid collider the coin lands with. Found automatically as the first non-trigger collider.")]
    [SerializeField] private Collider physicsCollider;
    [Tooltip("Player entering this collider pulls the coin in. Sphere or Box. Found automatically as the first trigger collider.")]
    [SerializeField] private Collider magnetZone;

    [Header("Drop Launch")]
    [SerializeField] private float launchUpSpeed = 5f;
    [Tooltip("Largest random sideways speed, so coins from one kill spread out.")]
    [SerializeField] private float launchSideSpeed = 1.5f;
    [Tooltip("Layers the coin can land on. Leave only the ground so coins don't bump into enemies or the player.")]
    [SerializeField] private LayerMask collideWith = ~0;
    [Tooltip("Coin counts as landed once it has touched something and is moving slower than this.")]
    [SerializeField] private float landedSpeed = 0.2f;
    [Tooltip("Force a landing after this long, in case the coin gets stuck on something.")]
    [SerializeField] private float maxDropTime = 3f;

    [Header("Lifetime")]
    [Tooltip("Seconds after spawning before an uncollected coin disappears, giving nothing.")]
    [SerializeField] private float lifetime = 10f;
    [Tooltip("Seconds before disappearing that the coin starts blinking.")]
    [SerializeField, Min(0f)] private float blinkWarningTime = 5f;
    [Tooltip("Blinks per second when the warning starts.")]
    [SerializeField, Min(0.1f)] private float blinkRateStart = 2f;
    [Tooltip("Blinks per second right before the coin disappears.")]
    [SerializeField, Min(0.1f)] private float blinkRateEnd = 12f;

    [Header("Fly To Player")]
    [SerializeField] private float flyStartSpeed = 4f;
    [SerializeField] private float flyAcceleration = 40f;
    [Tooltip("Aim this far above the player's pivot so coins land on the body, not the feet.")]
    [SerializeField] private float playerHeightOffset = 1f;
    [Tooltip("Safety net: award the coin if it somehow hasn't arrived after this long.")]
    [SerializeField] private float maxFlyTime = 3f;

    [Header("Visuals")]
    [SerializeField] private Renderer coinRenderer;
    [SerializeField] private List<CoinTypeVisual> typeVisuals = new List<CoinTypeVisual>();
    [SerializeField] private float spinSpeed = 180f;

    private Rigidbody body;
    private Transform player;
    private CoinState state;
    private float lifeTimer;
    private float dropTimer;
    private bool hasTouchedGround;
    private float flySpeed;
    private float flyTimer;
    private float blinkPhase;

    // Magnet size from the prefab, so the pickup range multiplier never compounds.
    private float baseMagnetRadius;
    private Vector3 baseMagnetSize;

    public CoinType CoinType => coinType;
    public int CoinValue => coinValue;
    public bool IsCollecting => state == CoinState.Collecting;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        // Rotation stays under script control so the idle spin isn't fighting the physics.
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

        if (physicsCollider == null)
        {
            Debug.LogWarning($"{name}: no solid collider, the coin will fall through the ground until it's forced to land.", this);
        }
        if (magnetZone == null)
        {
            Debug.LogWarning($"{name}: no trigger collider for the pickup radius, the coin only gets collected by its lifetime.", this);
        }

        if (coinRenderer == null) coinRenderer = GetComponentInChildren<Renderer>();

        CacheMagnetSize();
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

    /// <summary>
    /// Scales the magnet zone from its prefab size. 1 = original pickup range.
    /// </summary>
    public void SetPickupRangeMultiplier(float multiplier)
    {
        multiplier = Mathf.Max(0f, multiplier);

        switch (magnetZone)
        {
            case SphereCollider sphere: sphere.radius = baseMagnetRadius * multiplier; break;
            case CapsuleCollider capsule: capsule.radius = baseMagnetRadius * multiplier; break;
            // Height is left alone, only the ground footprint matters for pickup.
            case BoxCollider box: box.size = new Vector3(baseMagnetSize.x * multiplier, baseMagnetSize.y, baseMagnetSize.z * multiplier); break;
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
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
        state = CoinState.Dropping;

        // Velocity only sticks on an active, non-kinematic body.
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
        gameObject.SetActive(false);
        Despawned?.Invoke(this);
    }

    /// <summary>
    /// Sends the coin flying to the player from any distance, even mid-drop. Safe to call more than once.
    /// Used by the magnet zone and by collect-all effects.
    /// </summary>
    public void Collect()
    {
        if (state == CoinState.Collecting) return;

        if (player == null)
        {
            Award();
            return;
        }

        FreezeBody();
        SetVisible(true);
        state = CoinState.Collecting;
        flySpeed = flyStartSpeed;
        flyTimer = 0f;
    }

    private void Update()
    {
        if (state == CoinState.Collecting)
        {
            FlyToPlayer();
            return;
        }

        if (state == CoinState.Dropping)
        {
            dropTimer += Time.deltaTime;

            // Waiting for a contact first stops the coin "landing" at the top of its arc, where it's briefly slow.
            bool settled = hasTouchedGround && body.linearVelocity.sqrMagnitude <= landedSpeed * landedSpeed;
            if (settled || dropTimer >= maxDropTime)
            {
                FreezeBody();
                state = CoinState.Resting;
            }
        }
        else
        {
            if (spinSpeed != 0f)
            {
                transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            }

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

    private void UpdateBlink()
    {
        float timeLeft = lifetime - lifeTimer;
        if (timeLeft > blinkWarningTime || blinkWarningTime <= 0f)
        {
            SetVisible(true);
            return;
        }

        // 0 when the warning starts, 1 when the coin is about to vanish.
        float urgency = 1f - timeLeft / blinkWarningTime;
        float blinkRate = Mathf.Lerp(blinkRateStart, blinkRateEnd, urgency);

        // Accumulating the phase keeps the blink smooth while the rate speeds up.
        blinkPhase += blinkRate * Time.deltaTime;
        SetVisible(blinkPhase % 1f < 0.5f);
    }

    private void SetVisible(bool visible)
    {
        if (coinRenderer != null && coinRenderer.enabled != visible)
        {
            coinRenderer.enabled = visible;
        }
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
        // Script moves the coin directly from here on, which interpolation would smear.
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

        // Re-read the target every frame so the coin chases a moving player.
        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * Time.deltaTime);

        // MoveTowards lands exactly on the target, so no arrival distance is needed.
        if (transform.position == target || flyTimer >= maxFlyTime)
        {
            Award();
        }
    }

    private void Award()
    {
        TownManager.AddCoins(coinValue);
        Despawn();
    }

    private bool IsPlayerInMagnet()
    {
        if (player == null || magnetZone == null || !magnetZone.enabled) return false;

        // Tested at the zone's height so the player's pivot height doesn't matter, same as the turret zone.
        Vector3 point = player.position;
        point.y = magnetZone.bounds.center.y;
        return (magnetZone.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
    }

    public void SetCoinValue(CoinType coinType)
    {
        this.coinType = coinType;
        this.coinValue = (int)coinType;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (coinRenderer == null) return;

        for (int i = 0; i < typeVisuals.Count; i++)
        {
            if (typeVisuals[i].coinType == coinType && typeVisuals[i].material != null)
            {
                coinRenderer.sharedMaterial = typeVisuals[i].material;
                return;
            }
        }
    }
}
