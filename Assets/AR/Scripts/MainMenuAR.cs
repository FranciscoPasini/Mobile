using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuAR : MonoBehaviour
{
    public string aR1;
    public string aR2;
    public string mainMenuAr;

    public void AR1()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(aR1);
    }

    public void AR2()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(aR2);
    }

    public void MainMenuARScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuAr);
    }
}
