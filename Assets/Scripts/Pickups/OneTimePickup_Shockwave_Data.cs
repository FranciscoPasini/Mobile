using UnityEngine;

[CreateAssetMenu(fileName = "Shockwave Pickup", menuName = "One-Time Pickup/Shockwave")]
public class OneTimePickup_Shockwave_Data : Base_OneTimePickup_Data
{
    [Header("Shockwave")]
    [Tooltip("How far from the player the blast reaches.")]
    [Min(0.5f)]
    [SerializeField] private float radius = 8f;

    [Tooltip("Damage dealt to each enemy in the radius at wave 1.")]
    [Min(0f)]
    [SerializeField] private float damage = 8f;

    [Tooltip("Added to damage each scale step. 0.2 = +20% of base damage.")]
    [Min(0f)]
    [SerializeField] private float damageScaleFactor = 0.2f;

    [Tooltip("Waves between each damage increase. 2 = scales on wave 3, 5, 7...")]
    [Min(1)]
    [SerializeField] private int wavesPerScale = 2;

    [Tooltip("Seconds enemies stay still after the blast.")]
    [Min(0f)]
    [SerializeField] private float stunDuration = 1.5f;

    [Tooltip("How far enemies are shoved on the NavMesh. 0 = stun only.")]
    [Min(0f)]
    [SerializeField] private float knockback = 1.2f;

    [SerializeField] private LayerMask hitMask = ~0;

    public float Radius => radius;
    public float Damage => damage;
    public float DamageScaleFactor => damageScaleFactor;
    public int WavesPerScale => wavesPerScale;
    public float StunDuration => stunDuration;
    public float Knockback => knockback;
    public LayerMask HitMask => hitMask;

    public int GetScaleSteps(int wave)
    {
        return (Mathf.Max(1, wave) - 1) / Mathf.Max(1, wavesPerScale);
    }

    public float GetDamage(int wave)
    {
        return damage * (1f + damageScaleFactor * GetScaleSteps(wave));
    }
}
