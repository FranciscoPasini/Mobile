using UnityEngine;

public class UIsTexts : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI waveText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;
    [SerializeField] private TMPro.TextMeshProUGUI townHealthText;

    [SerializeField] private TownManager townManager;

    void OnEnable()
    {
        TownManager.OnCoinsChanged += OnCoinsChanged;
        townManager.OnWaveChanged += OnWaveChanged;
        townManager.OnTownDamageTaken += OnTownDamageTaken;
    }

    void OnDisable()
    {
        TownManager.OnCoinsChanged -= OnCoinsChanged;
        townManager.OnWaveChanged -= OnWaveChanged;
    }

    void Start() 
    {
        coinsText.text = "Coins: " + TownManager.Coins.ToString();
        waveText.text = "Wave: " + townManager.CurrentWave.ToString();
        townHealthText.text = "Town Health: " + townManager.GetTownMaxHealth().ToString() + " / " + townManager.GetTownMaxHealth().ToString();
    }

    private void OnCoinsChanged(int coins)
    {
        coinsText.text = "Coins: " + coins.ToString();
    }

    private void OnWaveChanged()
    {
        waveText.text = "Wave: " + townManager.CurrentWave.ToString();
    }

    private void OnTownDamageTaken(float damage)
    {
        townHealthText.text = "Town Health: " + townManager.GetTownHealth().ToString() + " / " + townManager.GetTownMaxHealth().ToString();
    }
}