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

    [Header("Configuración Música (Playlists)")]
    public float fadeDuration = 1.0f;

    // 1. Cambiamos las canciones individuales por Arrays (Listas)
    public AudioClip[] menuMusicPlaylist;
    public AudioClip[] gameMusicPlaylist;

    // Variables internas para gestionar la playlist actual
    private AudioClip[] currentPlaylist;
    private Coroutine playlistCoroutine;
    private int currentTrackIndex = 0;

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
            // 2. MUY IMPORTANTE: Quitamos el loop para saber cuándo termina la canción
            musicSource.loop = false;
        }

        Debug.Log($"[AudioManager] Start: master={master}, music={musicVol}, sfx={sfxVol}, musicSource={(musicSource != null ? "ok" : "NULL")}");

        // Iniciamos directamente la música del menú usando el nuevo sistema
        SwitchToMenuMusic();
    }

    // Método centralizado para el volumen
    public void UpdateMixerVolume(string parameterName, float sliderValue)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20;
        mainMixer.SetFloat(parameterName, dB);
    }

    // --- MÉTODOS DE SFX ORIGINALES ---
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
    public void SwitchToGameMusic()
    {
        if (currentPlaylist != gameMusicPlaylist)
            StartCoroutine(FadeAndSwitchPlaylist(gameMusicPlaylist));
    }

    public void SwitchToMenuMusic()
    {
        if (currentPlaylist != menuMusicPlaylist)
            StartCoroutine(FadeAndSwitchPlaylist(menuMusicPlaylist));
    }

    private IEnumerator FadeAndSwitchPlaylist(AudioClip[] newPlaylist)
    {
        float currentTime = 0f;
        float startVolume = musicSource != null ? musicSource.volume : 1f;

        // Bajar volumen (Fade out)
        if (musicSource.isPlaying)
        {
            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
                yield return null;
            }
        }

        musicSource.Stop();

        // Detenemos la rutina de la playlist anterior para que no se pisen
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }

        // Asignamos la nueva playlist
        currentPlaylist = newPlaylist;

        // Iniciamos el reproductor de la lista
        if (currentPlaylist != null && currentPlaylist.Length > 0)
        {
            playlistCoroutine = StartCoroutine(PlayPlaylistRoutine());
        }
        else
        {
            Debug.LogWarning("[AudioManager] La playlist está vacía o no asignada.");
        }
    }

    // 3. Nueva rutina que reproduce las canciones en bucle
    private IEnumerator PlayPlaylistRoutine()
    {
        currentTrackIndex = 0;
        bool isFirstTrack = true;

        while (true) // Este bucle mantiene viva la playlist
        {
            musicSource.clip = currentPlaylist[currentTrackIndex];
            musicSource.Play();

            // Solo hacemos el fade-in para la primera canción al cambiar de playlist
            if (isFirstTrack)
            {
                float currentTime = 0f;
                while (currentTime < fadeDuration)
                {
                    currentTime += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0f, 1f, currentTime / fadeDuration);
                    yield return null;
                }
                isFirstTrack = false;
            }

            musicSource.volume = 1f; // Aseguramos el volumen máximo tras el fade

            // Esperamos hasta que la canción actual deje de sonar
            yield return new WaitWhile(() => musicSource.isPlaying);

            // Pasamos a la siguiente canción
            currentTrackIndex++;

            // Si hemos llegado al final de la lista, volvemos a la primera canción (Loop de playlist)
            if (currentTrackIndex >= currentPlaylist.Length)
            {
                currentTrackIndex = 0;
            }
        }
    }
}