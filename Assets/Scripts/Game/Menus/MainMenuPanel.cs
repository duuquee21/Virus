using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;

    public void PlayGame()
    {
        if (LevelManager.instance != null)
        {
            // El LevelManager ya pone el hexágono (1) por defecto en NewGame
            LevelManager.instance.NewGameFromMainMenu();
        }
    }

    public void OpenSettings()
    {
        if (LevelManager.instance != null && LevelManager.instance.transitionScript != null)
        {
            // 1. Cambiamos la forma a Pentágono (2)
            LevelManager.instance.transitionScript.SetShape(2);

            // 2. Usamos la corrutina de transición del LevelManager
            LevelManager.instance.ChangePanelWithTransition(mainMenuPanel, settingsPanel);
        }
        else
        {
            // Fallback: si no hay transición, abrir normal
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (LevelManager.instance != null && LevelManager.instance.transitionScript != null)
        {
            // Opcional: Volver a círculo (0) al salir de ajustes
            LevelManager.instance.transitionScript.SetShape(0);

            LevelManager.instance.ChangePanelWithTransition(settingsPanel, mainMenuPanel);
        }
        else
        {
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }

    // Mantengo estos por compatibilidad, aunque podrías usar CloseSettings()
    public void BackToMainMenu()
    {
        CloseSettings();
    }

    public void QuitGame()
    {
        Debug.Log("Cerrando aplicación");
        Application.Quit();
    }
}