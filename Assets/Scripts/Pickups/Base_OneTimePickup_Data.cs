using UnityEngine;

[CreateAssetMenu(fileName = "New One-Time Pickup", menuName = "One-Time Pickup/Pickup Data")]
public class Base_OneTimePickup_Data : ScriptableObject
{
    [Tooltip("Prefab with a Base_OneTimePickup (or child) on the root.")]
    public GameObject pickupPrefab;
    public string pickupName;
    public Sprite pickupIcon;

    [Header("Lifetime")]
    [Tooltip("Seconds on the ground before an uncollected pickup vanishes with no effect.")]
    [Min(0.5f)] public float lifetime = 30f;
    [Tooltip("Seconds before disappearing that the pickup starts blinking.")]
    [Min(0f)] public float blinkWarningTime = 5f;
    [Min(0.1f)] public float blinkRateStart = 2f;
    [Min(0.1f)] public float blinkRateEnd = 12f;
}
