using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string level;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private InterstitialAdManager interstitialAdManager;

    public void PlayButton()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(level))
        {
            Debug.LogError("MainMenuManager: no scene name set on Play.", this);
            return;
        }

        if (interstitialAdManager != null)
        {
            interstitialAdManager.ShowInterstitial(LoadGame);
        }
        else
        {
            LoadGame();
        }
    }

    public void Exit()
    {
        if (interstitialAdManager != null)
        {
            interstitialAdManager.ShowInterstitial(Application.Quit);
        }
        else
        {
            Application.Quit();
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void LoadGame()
    {
        SceneManager.LoadScene(level);
    }
}
