using UnityEngine;

public class UIsTexts : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI waveText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;

    TownManager townManager;

    private void Update()
    {
        UIsTextUpdete();
    }

    private void UIsTextUpdete()
    {
      //  waveText.text = "Wave: " + townManager.CurrentWave.ToString();
        coinsText.text = "Coins: " + TownManager.Coins.ToString();
    }
}