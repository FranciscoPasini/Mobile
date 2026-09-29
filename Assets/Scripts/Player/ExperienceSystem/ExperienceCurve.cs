using UnityEngine;
using System;

public enum ExperienceCurveMethod
{
    /// <summary>base + linearIncrease * (level - 1). Each level costs the same extra amount.</summary>
    Linear,
    /// <summary>base * level ^ exponent. The usual RPG curve, grows fast but never explodes.</summary>
    Polynomial,
    /// <summary>base * growth ^ (level - 1). Gets brutal at high levels, pair it with a cap.</summary>
    Exponential
}

/// <summary>
/// Experience needed to go from a level to the next one.
/// </summary>
[Serializable]
public class ExperienceCurve
{
    public ExperienceCurveMethod method = ExperienceCurveMethod.Polynomial;

    [Tooltip("Experience needed to go from level 1 to 2.")]
    [Min(1)] public int baseExperience = 100;

    [Tooltip("Linear: extra experience added per level.")]
    [Min(0)] public int linearIncrease = 50;

    [Tooltip("Polynomial: 1 is linear, 1.5 is a gentle curve, 2 is steep.")]
    [Min(1f)] public float exponent = 1.5f;

    [Tooltip("Exponential: multiplier per level. 1.12 means each level needs 12% more than the last.")]
    [Min(1f)] public float exponentialGrowth = 1.12f;

    [Tooltip("Highest experience a single level can require, 0 = no cap.")]
    [Min(0)] public int maxExperience = 0;

    [Tooltip("Round requirements to a multiple of this so the UI shows clean numbers. 1 = no rounding.")]
    [Min(1)] public int roundTo = 5;

    // Keeps the requirement far enough below int.MaxValue that adding experience can't overflow.
    private const double SafeLimit = int.MaxValue / 2.0;

    public int GetExperienceToNextLevel(int level)
    {
        level = Mathf.Max(1, level);

        double experience;
        switch (method)
        {
            case ExperienceCurveMethod.Linear:
                experience = baseExperience + (double)linearIncrease * (level - 1);
                break;
            case ExperienceCurveMethod.Exponential:
                experience = baseExperience * Math.Pow(exponentialGrowth, level - 1);
                break;
            default:
                experience = baseExperience * Math.Pow(level, exponent);
                break;
        }

        if (maxExperience > 0) experience = Math.Min(experience, maxExperience);
        if (double.IsNaN(experience) || experience > SafeLimit) experience = SafeLimit;

        int step = Mathf.Max(1, roundTo);
        experience = Math.Round(experience / step) * step;

        return (int)Math.Max(1, experience);
    }
}
