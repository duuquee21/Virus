using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Fuentes de Audio")]
    public AudioSource musicSource; 
    public AudioSource sfxSource;   

    [Header("Música")]
    public float fadeDuration = 1.0f;
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Efectos de Sonido (SFX)")]
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
        if (menuMusic != null) { musicSource.clip = menuMusic; musicSource.Play(); }
    }

   
    public void PlaySFX(AudioClip clip)
    {
        // PlayOneShot permite que suenen varios a la vez sin cortarse
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    
    public void PlayBuyUpgrade() { PlaySFX(buyUpgradeSound); }
    public void PlayBuyZone() { PlaySFX(buyZoneSound); }
    public void PlayError() { PlaySFX(errorSound); }
    public void PlayClick() { PlaySFX(clickSound); }

   
    public void SwitchToGameMusic() { if (musicSource.clip != gameMusic) StartCoroutine(FadeTrack(gameMusic)); }
    public void SwitchToMenuMusic() { if (musicSource.clip != menuMusic) StartCoroutine(FadeTrack(menuMusic)); }

    private IEnumerator FadeTrack(AudioClip newClip)
    {
        float currentTime = 0;
        float startVolume = musicSource.volume;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }
        musicSource.volume = 0; musicSource.Stop(); musicSource.clip = newClip; musicSource.Play();
        currentTime = 0;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, 1f, currentTime / fadeDuration);
            yield return null;
        }
        musicSource.volume = 1f;
    }
}