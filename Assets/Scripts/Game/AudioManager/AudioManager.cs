using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Referencias al Mixer")]
    public AudioMixer mainMixer;

    [Header("Fuentes de Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Configuración Música")]
    public float fadeDuration = 1.0f;
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Efectos de Sonido (Clips)")]
    public AudioClip buyUpgradeSound;
    public AudioClip buyZoneSound;
    public AudioClip errorSound;
    public AudioClip clickSound;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Cargar volúmenes guardados al iniciar el juego
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = master;
        UpdateMixerVolume("Master", master);

        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        UpdateMixerVolume("MusicVol", musicVol);
        UpdateMixerVolume("SFXVol", sfxVol);
        if (musicSource != null)
        {
            musicSource.volume = 1f;
            musicSource.mute = false;
            musicSource.loop = true; // <--- AÑADE ESTA LÍNEA
        }

        Debug.Log($"[AudioManager] Start: master={master}, music={musicVol}, sfx={sfxVol}, musicSource={(musicSource!=null ? "ok" : "NULL")}");

        if (menuMusic != null && musicSource != null)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
            Debug.Log($"[AudioManager] Reproduciendo menuMusic: {menuMusic.name}");
        }
        else
        {
            if (menuMusic == null) Debug.LogWarning("[AudioManager] menuMusic no asignado");
            if (musicSource == null) Debug.LogWarning("[AudioManager] musicSource no asignado");
        }
    }

    // Método centralizado para el volumen (Escala logarítmica para el Mixer)
    public void UpdateMixerVolume(string parameterName, float sliderValue)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20;
        mainMixer.SetFloat(parameterName, dB);
    }

    // --- TUS MÉTODOS DE SFX ORIGINALES ---
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayBuyUpgrade() { PlaySFX(buyUpgradeSound); }
    public void PlayBuyZone() { PlaySFX(buyZoneSound); }
    public void PlayError() { PlaySFX(errorSound); }
    public void PlayClick() { PlaySFX(clickSound); }

    // --- GESTIÓN DE MÚSICA Y FADE ---
    public void SwitchToGameMusic() { if (musicSource.clip != gameMusic) StartCoroutine(FadeTrack(gameMusic)); }
    public void SwitchToMenuMusic() { if (musicSource.clip != menuMusic) StartCoroutine(FadeTrack(menuMusic)); }

    private IEnumerator FadeTrack(AudioClip newClip)
    {
        float currentTime = 0f;
        float startVolume = musicSource != null ? musicSource.volume : 1f;

        // Bajar volumen de la fuente (no del mixer)
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // Subir volumen de la fuente
        currentTime = 0f;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, 1f, currentTime / fadeDuration);
            yield return null;
        }
        musicSource.volume = 1f;
    }
}