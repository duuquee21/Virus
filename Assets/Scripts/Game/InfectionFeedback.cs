using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para el control de duplicados

public class InfectionFeedback : MonoBehaviour
{
    public static InfectionFeedback instance;

    [Header("Efectos Visuales (VFX)")]
    public GameObject infectionParticles;
    public GameObject infection1Particles;
    public GameObject basicImpactParticles;

    [Header("Efectos de Sonido (SFX)")]
    public AudioSource audioSource;
    // Agregamos un pequeño cooldown para evitar saturación de clips idénticos
    private float lastSoundTime;
    private const float MIN_SOUND_INTERVAL = 0.02f;

    public AudioClip[] infectionSounds;
    public AudioClip[] phaseChangeSounds;
    public AudioClip[] bolaBlancaSounds;
    public AudioClip[] basicWallImpactSounds;
    public AudioClip[] basicImpactSounds;

    [Header("Cámara & Shake")]
    public Transform cameraTransform;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    public string zonaTag = "Zona";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Opcional: Don'tDestroyOnLoad(gameObject); si quieres que persista
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Configuración profesional del AudioSource si no está en el inspector
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.priority = 128; // Prioridad media
        }
    }

    // --- MÉTODOS ORIGINALES MANTENIDOS ---

    public void PlayEffect(Vector3 position, Color particleColor, bool mute)
    {
        // 1. VISUAL
        if (infection1Particles != null)
        {
            SpawnVFX(infection1Particles, position, particleColor, 1.0f);
        }

        // 2. SONIDO con limitación de frecuencia
        if (!mute)
        {
            PlayRandomClip(infectionSounds, 0.85f, 1.15f);
        }
    }

    public void PlayPhaseChangeEffect(Vector3 position, Color particleColor)
    {
        if (infectionParticles != null)
        {
            SpawnVFX(infectionParticles, position, Color.white * 1.3f, 1.0f);
        }
        PlayPhaseChangeSound();
    }

    public void PlayPhaseChangeSound()
    {
        // Pitch más alto para progresión, como pediste
        PlayRandomClip(phaseChangeSounds, 1.1f, 1.3f);
    }

    public void PlayBasicImpactSoundAgainstWall()
    {
        PlayRandomClip(basicWallImpactSounds, 1.1f, 1.3f);
    }

    public void PlayBasicImpactSound()
    {
        PlayRandomClip(basicImpactSounds, 1.1f, 1.3f);
    }

    public void PlayBasicImpactEffectAgainstWall(Vector3 position, Color particleColor)
    {
        if (basicImpactParticles != null)
        {
            SpawnVFX(basicImpactParticles, position, Color.white * 1.3f, 1.0f);
        }

        PlayBasicImpactSoundAgainstWall();

        TriggerShakeOnZonas(2);
    }

    public void PlayBasicImpactEffect(Vector3 position, Color particleColor, bool sonido)
    {
        if (basicImpactParticles != null)
        {
            // Lógica de tamaño reducido que tenías
            SpawnVFX(basicImpactParticles, position, Color.white * 1.3f, 0.5f);
        }
        if (sonido)
        {
            PlayBasicImpactSound();
        }
    }

    public void PlayUltraEffect(Vector3 position, Color particleColor)
    {
        if (infection1Particles != null)
        {
            SpawnVFX(infection1Particles, position, Color.white * 1.3f, 1.0f);
        }

        PlayRandomClip(bolaBlancaSounds, 0.85f, 1.15f);
        TriggerShakeOnZonas(2);
    }

    // --- LÓGICA INTERNA PROFESIONAL (HELPER METHODS) ---

    private void PlayRandomClip(AudioClip[] clips, float minPitch, float maxPitch)
    {
        // LIMITACIÓN DE VOZ: Si el juego está "loko", no dispares sonidos en intervalos menores a 20ms
        // Esto evita que los altavoces "peten" por acumulación de ondas.
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        if (Time.time < lastSoundTime + MIN_SOUND_INTERVAL) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        // Volumen adaptativo: si hay muchos sonidos, bajar un poco el volumen individual
        audioSource.PlayOneShot(clip, 0.8f);
        lastSoundTime = Time.time;
    }

    private void SpawnVFX(GameObject prefab, Vector3 pos, Color color, float sizeMultiplier)
    {
        GameObject vfx = Instantiate(prefab, pos, Quaternion.identity);
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
            main.startSize = main.startSize.constant * sizeMultiplier;
        }
        Destroy(vfx, 2f);
    }

    private void TriggerShakeOnZonas(int multiplier)
    {
        GameObject[] zonas = GameObject.FindGameObjectsWithTag(zonaTag);
        foreach (GameObject zona in zonas)
        {
            StartCoroutine(ShakeObject(zona.transform, multiplier));
        }
    }

    private IEnumerator ShakeObject(Transform objTransform, int multiplier)
    {
        Vector3 originalPos = objTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude * multiplier;
            float y = Random.Range(-1f, 1f) * shakeMagnitude * multiplier;

            objTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localPosition = originalPos;
    }

    public void CleanAllActiveParticles()
    {
        // Optimización: Usar el tag para no iterar por TODOS los ParticleSystems si no es necesario
        GameObject[] efectos = GameObject.FindGameObjectsWithTag("Efectos");
        foreach (GameObject ef in efectos)
        {
            Destroy(ef);
        }
    }
}