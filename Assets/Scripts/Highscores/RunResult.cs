using System;
using System.Collections.Generic;

/// <summary>
/// One run's stats, as stored locally and in the leaderboard.
/// </summary>
[Serializable]
public class RunResult
{
    public string playerId;
    public string name;
    public int wave;
    public int survivedSeconds;
    public int coins;
    public int kills;
    public int upgrades;
    public int level;

    /// <summary>Higher wave wins, ties go to whoever survived longer.</summary>
    public bool IsBetterThan(RunResult other)
    {
        if (other == null) return true;
        if (wave != other.wave) return wave > other.wave;
        return survivedSeconds > other.survivedSeconds;
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "name", name },
            { "wave", wave },
            { "survivedSeconds", survivedSeconds },
            { "coins", coins },
            { "kills", kills },
            { "upgrades", upgrades },
            { "level", level },
        };
    }

    public string SurvivedText => $"{survivedSeconds / 60}:{survivedSeconds % 60:00}";
}
