using UnityEngine;

[CreateAssetMenu(fileName = "Shockwave Pickup", menuName = "One-Time Pickup/Shockwave")]
public class OneTimePickup_Shockwave_Data : Base_OneTimePickup_Data
{
    [Header("Shockwave")]
    [Tooltip("How far from the player the blast reaches.")]
    [Min(0.5f)]
    [SerializeField] private float radius = 8f;

    [Tooltip("Damage dealt to each enemy in the radius.")]
    [Min(0f)]
    [SerializeField] private float damage = 8f;

    [Tooltip("Seconds enemies stay still after the blast.")]
    [Min(0f)]
    [SerializeField] private float stunDuration = 1.5f;

    [Tooltip("How far enemies are shoved on the NavMesh. 0 = stun only.")]
    [Min(0f)]
    [SerializeField] private float knockback = 1.2f;

    [SerializeField] private LayerMask hitMask = ~0;

    public float Radius => radius;
    public float Damage => damage;
    public float StunDuration => stunDuration;
    public float Knockback => knockback;
    public LayerMask HitMask => hitMask;
}
