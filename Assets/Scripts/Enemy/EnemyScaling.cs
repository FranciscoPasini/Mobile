using System;
using UnityEngine;

/// <summary>
/// Per-prefab curves so a fast weak enemy and a slow tank can grow differently.
/// Difficulty level 0 is the prefab's base stats.
/// </summary>
[Serializable]
public class EnemyScaling
{
    [Tooltip("+20% of base health per difficulty step, no cap. Needs to outpace player damage.")]
    public StatScaling health = new StatScaling(ScalingMethod.Linear, 0.2f, 0f);

    [Tooltip("+15% of base damage per step, no cap.")]
    public StatScaling damage = new StatScaling(ScalingMethod.Linear, 0.15f, 0f);

    [Tooltip("Approaches 1.8x base speed. Faster than that and they become hard to track.")]
    public StatScaling speed = new StatScaling(ScalingMethod.Asymptotic, 0.08f, 1.8f);

    [Tooltip("Seconds between attacks. Approaches 2x faster than base.")]
    public StatScaling attackRate = new StatScaling(ScalingMethod.Asymptotic, 0.08f, 2f);

    [Tooltip("Kill XP grows with the enemy so harder enemies stay worth fighting.")]
    public StatScaling experience = new StatScaling(ScalingMethod.Linear, 0.15f, 0f);

    public float EvaluateHealth(float baseValue, int level) => health.Evaluate(baseValue, level, false);
    public float EvaluateDamage(float baseValue, int level) => damage.Evaluate(baseValue, level, false);
    public float EvaluateSpeed(float baseValue, int level) => speed.Evaluate(baseValue, level, false);
    public float EvaluateAttackRate(float baseValue, int level) => attackRate.Evaluate(baseValue, level, true);
    public float EvaluateExperience(float baseValue, int level) => experience.Evaluate(baseValue, level, false);
}
