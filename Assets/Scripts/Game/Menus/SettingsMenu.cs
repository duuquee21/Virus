using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    void Start()
    {
        // cargar volumen
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;
        if(volumeSlider) volumeSlider.value = savedVolume;

        // pantalla completa
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = isFullscreen;
        if(fullscreenToggle) fullscreenToggle.isOn = isFullscreen;
    }

    

    public void SetVolume(float volume)
    {
        // cambiar volumen
        AudioListener.volume = volume;
      
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        // Guardar (1 = true, 0 = false)
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }
}