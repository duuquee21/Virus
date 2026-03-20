using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;

    // Cooldown para prevenir doble clic
    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.3f; // 300ms

    // Flag para saber si el panel de ajustes está abierto
    private bool isSettingsOpen = false;

    public void PlayGame()
    {
        // Chequear cooldown para prevenir doble clic
        if (Time.time - lastClickTime < CLICK_COOLDOWN)
            return;

        lastClickTime = Time.time;

        if (LevelManager.instance != null)
        {
            // El LevelManager ya pone el hexágono (1) por defecto en NewGame
            LevelManager.instance.NewGameFromMainMenu();
        }
    }

    public void OpenSettings()
    {
        // Chequear cooldown para prevenir doble clic
        if (Time.time - lastClickTime < CLICK_COOLDOWN)
            return;

        // Si ya está abierto, no hacer nada
        if (isSettingsOpen)
            return;

        lastClickTime = Time.time;
        isSettingsOpen = true;

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
        // Chequear cooldown para prevenir doble clic
        if (Time.time - lastClickTime < CLICK_COOLDOWN)
            return;

        lastClickTime = Time.time;
        isSettingsOpen = false;

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