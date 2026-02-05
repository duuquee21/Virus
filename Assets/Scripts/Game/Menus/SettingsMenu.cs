using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AudioControl
{
    public string saveKey; // Clave: "MusicVolume" o "SFXVolume"
    public string mixerParameter; // Nombre en el Mixer: "MusicVol" o "SFXVol"
    public Slider volumeSlider;
}

public class SettingsMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    public AudioControl[] audioControls;
    public Toggle fullscreenToggle;

    void Start()
    {
        // Configurar cada Slider (Música y SFX)
        foreach (var control in audioControls)
        {
            if (control.volumeSlider != null)
            {
                float savedVolume = PlayerPrefs.GetFloat(control.saveKey, 0.75f);
                control.volumeSlider.value = savedVolume;

                // Aplicar valor inicial al Mixer al abrir el menú
                AudioManager.instance.UpdateMixerVolume(control.mixerParameter, savedVolume);

                // Listener para cambios en tiempo real
                control.volumeSlider.onValueChanged.AddListener((v) => {
                    AudioManager.instance.UpdateMixerVolume(control.mixerParameter, v);
                    PlayerPrefs.SetFloat(control.saveKey, v);
                });
            }
        }

        // Pantalla completa
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggle) fullscreenToggle.isOn = isFullscreen;
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }
}