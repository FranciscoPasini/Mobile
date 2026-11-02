using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string level;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private InterstitialAdManager interstitialAdManager;


    public void PlayButton()
    {
        interstitialAdManager.ShowInterstitial(() =>
        {
            SceneManager.LoadScene(level);
        });
    }

    public void Exit()
    {
        interstitialAdManager.ShowInterstitial(() =>
        {
            Application.Quit();
        });
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}
