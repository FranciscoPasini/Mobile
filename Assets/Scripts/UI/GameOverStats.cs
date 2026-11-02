using TMPro;
using UnityEngine;

/// <summary>
/// Fills in the game over summary when the panel opens.
/// </summary>
public class GameOverStats : MonoBehaviour
{
    [SerializeField] private TownManager townManager;
    [SerializeField] private TextMeshProUGUI wavesText;

    private void OnEnable()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
        if (townManager == null || wavesText == null) return;

        int waves = Mathf.Max(0, townManager.CurrentWave - 1);
        wavesText.text = waves == 1 ? "YOU SURVIVED 1 WAVE" : $"YOU SURVIVED {waves} WAVES";
    }
}
