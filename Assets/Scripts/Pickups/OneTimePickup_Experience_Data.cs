using UnityEngine;

[CreateAssetMenu(fileName = "Experience Pickup", menuName = "One-Time Pickup/Experience")]
public class OneTimePickup_Experience_Data : Base_OneTimePickup_Data
{
    [Header("Experience")]
    [Tooltip("Flat XP granted on collect, before the percent below.")]
    [Min(0)]
    [SerializeField] private int bonusExperience = 40;

    [Tooltip("Extra XP as a percent of the current level's requirement. 25 = a quarter of a level.")]
    [Range(0f, 100f)]
    [SerializeField] private float percentOfNextLevel = 25f;

    public int BonusExperience => bonusExperience;
    public float PercentOfNextLevel => percentOfNextLevel;

    public int GetExperienceAmount(Player_ExperienceAndStats stats)
    {
        int amount = bonusExperience;
        if (stats != null && percentOfNextLevel > 0f)
        {
            amount += Mathf.RoundToInt(stats.GetExperienceToNextLevel() * (percentOfNextLevel / 100f));
        }

        return Mathf.Max(0, amount);
    }
}
