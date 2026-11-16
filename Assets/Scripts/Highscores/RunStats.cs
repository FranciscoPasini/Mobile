using UnityEngine;

/// <summary>
/// Counts the current run's stats. TownManager starts it on Start and finishes it on game over.
/// </summary>
public static class RunStats
{
    public static int Kills { get; private set; }
    public static int CoinsCollected { get; private set; }
    public static int TowerUpgrades { get; private set; }

    // Scaled time, so pauses don't count as survived time.
    private static float startTime;

    public static void Begin()
    {
        Kills = CoinsCollected = TowerUpgrades = 0;
        startTime = Time.time;

        Subscribe(false);
        Subscribe(true);
    }

    public static void AddCoins(int amount)
    {
        if (amount > 0) CoinsCollected += amount;
    }

    public static RunResult Finish(int wave, int level)
    {
        Subscribe(false);
        return new RunResult
        {
            wave = wave,
            survivedSeconds = Mathf.FloorToInt(Time.time - startTime),
            coins = CoinsCollected,
            kills = Kills,
            upgrades = TowerUpgrades,
            level = level,
        };
    }

    private static void Subscribe(bool subscribe)
    {
        if (subscribe)
        {
            Base_Enemy.Killed += HandleKilled;
            PurchaseArea.OnAnyUpgraded += HandleUpgraded;
        }
        else
        {
            Base_Enemy.Killed -= HandleKilled;
            PurchaseArea.OnAnyUpgraded -= HandleUpgraded;
        }
    }

    private static void HandleKilled(Base_Enemy enemy) => Kills++;

    private static void HandleUpgraded(PurchaseArea area)
    {
        if (area.Target is Building_Turret) TowerUpgrades++;
    }
}
