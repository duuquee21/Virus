using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;

    public void PlayGame()
    {
        
        if (LevelManager.instance != null)
        {
            LevelManager.instance.TryStartGame();
        }
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Cerrando aplicación");
        Application.Quit();
    }
}