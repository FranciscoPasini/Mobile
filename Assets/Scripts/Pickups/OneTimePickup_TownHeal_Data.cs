using UnityEngine;

[CreateAssetMenu(fileName = "Town Heal Pickup", menuName = "One-Time Pickup/Town Heal")]
public class OneTimePickup_TownHeal_Data : Base_OneTimePickup_Data
{
    [Header("Town Heal")]
    [Tooltip("Percent of the town's max health restored on collect. 20 = 20%.")]
    [Range(1f, 100f)]
    [SerializeField] private float healPercent = 20f;

    public float HealPercent => healPercent;
}
