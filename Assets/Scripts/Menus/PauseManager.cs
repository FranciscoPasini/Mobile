using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private string mainmenu;
    [SerializeField] private GameObject settings;

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }
    public void Pause()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    public void SettingsMenu()
    {
        settings.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void BackToPauseMenu()
    {
        settings.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void MainMenu(string sceneName)
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void changeTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
    }
}
