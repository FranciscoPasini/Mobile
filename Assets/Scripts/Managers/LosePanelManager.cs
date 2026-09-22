using UnityEngine;
using UnityEngine.SceneManagement;

public class LosePanelManager : MonoBehaviour
{
    public string mainMenuSceneName;
    public string GameSceneName;

    private void RestartLevel()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
