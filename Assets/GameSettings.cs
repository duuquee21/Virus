using UnityEngine;
using UnityEngine.Audio; // Necesario para el control de volumen

public class GameSettings : MonoBehaviour
{
    public static GameSettings instance;

    [Header("Motor de Audio")]
    public AudioMixer audioMixer; // Arrastra aquí tu AudioMixer

    // Variables guardadas
    public bool shakeEnabled = true;
    public bool isFullscreen = true;
    public int targetFPS = 60;
    public float masterVol = 1f;
    public float musicVol = 1f;
    public float sfxVol = 1f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyAllSettings();
    }

    
    public void LoadSettings()
    {
        shakeEnabled = PlayerPrefs.GetInt("ShakeEnabled", 1) == 1;
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        targetFPS = PlayerPrefs.GetInt("FPS", 60);
        // Compatibilidad: algunos sistemas usan las claves "MasterVolume" vs "MasterVol"
        masterVol = PlayerPrefs.GetFloat("MasterVolume", PlayerPrefs.GetFloat("MasterVol", 1f));
        musicVol = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);
    }

    
    public void ApplyAllSettings()
    {
        SetFullscreen(isFullscreen);
        SetFPS(targetFPS);
        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    // 🎛️ FUNCIONES DE SETEO
    public void SetShake(bool value)
    {
        shakeEnabled = value;
        PlayerPrefs.SetInt("ShakeEnabled", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFull)
    {
        isFullscreen = isFull;

#if UNITY_WEBGL
        // CÓDIGO SOLO PARA WEB (Itch.io)
        // En web, el navegador manda. Solo pedimos el cambio de estado.
        Screen.fullScreen = isFull;
#else
        // CÓDIGO PARA PC (Windows/Mac/Linux)
        if (isFull)
        {
            Resolution maxRes = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(maxRes.width, maxRes.height, true);
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(1280, 720, false);
        }
#endif

        PlayerPrefs.SetInt("Fullscreen", isFull ? 1 : 0);
        PlayerPrefs.Save();
    }
    public void SetFPS(int fps)
    {
        targetFPS = fps;
        Application.targetFrameRate = targetFPS;
        PlayerPrefs.SetInt("FPS", targetFPS);
        PlayerPrefs.Save();
    }

    // 🔊 VOLUMEN (Convertido a escala Logarítmica para que suene natural)
    public void SetMasterVolume(float vol)
    {
        masterVol = vol;
        if (audioMixer != null) audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(vol, 0.0001f)) * 20f);

        // Guardamos en varias claves para mantener compatibilidad con código antiguo.
        PlayerPrefs.SetFloat("MasterVolume", vol);
        PlayerPrefs.SetFloat("MasterVol", vol);
        PlayerPrefs.SetFloat("VolumenGlobal", vol);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float vol)
    {
        musicVol = vol;
        if (audioMixer != null) audioMixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Max(vol, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("MusicVol", vol);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float vol)
    {
        sfxVol = vol;
        if (audioMixer != null) audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(vol, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("SFXVol", vol);
        PlayerPrefs.Save();
    }
}