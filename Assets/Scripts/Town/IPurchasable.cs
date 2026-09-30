using System;

/// <summary>
/// Something a PurchaseArea can buy and upgrade.
/// </summary>
public interface IPurchasable
{
    bool IsPurchased { get; }
    int Level { get; }
    bool IsMaxLevel { get; }
    int NextCost { get; }
    int RemainingCost { get; }
    float PaymentProgress { get; }

    event Action<float> OnPaymentProgress;
    event Action OnPurchased;
    event Action<int> OnUpgraded;

    /// <summary>
    /// Spends up to <paramref name="amount"/> toward the next level.
    /// Returns how many coins were accepted. Completes the level when paid in full.
    /// </summary>
    int ApplyPayment(int amount);
}
