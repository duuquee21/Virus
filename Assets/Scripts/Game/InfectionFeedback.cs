using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InfectionFeedback : MonoBehaviour
{
    public static InfectionFeedback instance;

    [Header("Efectos Visuales (VFX)")]
    public GameObject infectionParticles;
    public GameObject infection1Particles;
    public GameObject basicImpactParticles;

    [Header("Ajustes de Volumen Global")]
    [Range(0f, 1f)] public float masterSFXVolume = 1f;

    [Header("Efectos de Sonido (SFX)")]
    public AudioSource audioSource;
    private float lastSoundTime;
    private const float MIN_SOUND_INTERVAL = 0.05f;

    [Space(5)]
    public AudioClip[] infectionSounds;
    [Range(0f, 1f)] public float infectionVolume = 0.8f;

    public AudioClip[] phaseChangeSounds;
    [Range(0f, 1f)] public float phaseChangeVolume = 1f;

    public AudioClip[] bolaBlancaSounds;
    [Range(0f, 1f)] public float bolaBlancaVolume = 1f;

    public AudioClip[] basicWallImpactSounds;
    [Range(0f, 1f)] public float wallImpactVolume = 0.7f;

    public AudioClip[] basicImpactSounds;
    [Range(0f, 1f)] public float basicImpactVolume = 0.6f;

    [Header("Cámara & Shake")]
    public Transform cameraTransform;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    public string zonaTag = "Zona";

    private Transform[] zonasCached;

    // --- LÓGICA DE POOLING ---
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<Transform, Coroutine> activeShakes = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();

    void Awake()
    {
        if (instance == null) instance = this;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.priority = 128;
        }

        InitPool(infectionParticles);
        InitPool(infection1Particles);
        InitPool(basicImpactParticles);
    }

    void Start()
    {
        Transform[] todosLosTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        List<Transform> listaZonas = new List<Transform>();

        foreach (Transform t in todosLosTransforms)
        {
            if (t.CompareTag(zonaTag) && t.gameObject.scene.name != null)
            {
                listaZonas.Add(t);
                if (!originalPositions.ContainsKey(t))
                    originalPositions.Add(t, t.localPosition);
            }
        }

        zonasCached = listaZonas.ToArray();
        Debug.Log($"Se han encontrado {zonasCached.Length} zonas (incluyendo desactivadas)");
    }

    private void InitPool(GameObject prefab)
    {
        if (prefab != null && !poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
        }
    }

    // --- MÉTODOS ORIGINALES ACTUALIZADOS CON VOLUMEN ---

    public void PlayEffect(Vector3 position, Color particleColor, bool mute)
    {
        if (infection1Particles != null)
            SpawnVFX(infection1Particles, position, particleColor, 1.0f);

        if (!mute)
            PlayRandomClip(infectionSounds, 0.85f, 1.15f, infectionVolume);
    }

    public void PlayPhaseChangeEffect(Vector3 position, Color particleColor)
    {
        if (infectionParticles != null)
            SpawnVFX(infectionParticles, position, Color.white * 1.3f, 1.0f);

        PlayPhaseChangeSound();
    }

    public void PlayPhaseChangeSound() => PlayRandomClip(phaseChangeSounds, 1.1f, 1.3f, phaseChangeVolume);
    public void PlayBasicImpactSoundAgainstWall() => PlayRandomClip(basicWallImpactSounds, 1.1f, 1.3f, wallImpactVolume);
    public void PlayBasicImpactSound() => PlayRandomClip(basicImpactSounds, 1.1f, 1.3f, basicImpactVolume);

    public void PlayBasicImpactEffectAgainstWall(Vector3 position, Color particleColor)
    {
        if (basicImpactParticles != null)
            SpawnVFX(basicImpactParticles, position, Color.white * 1.3f, 1.0f);

        PlayBasicImpactSoundAgainstWall();
        TriggerShakeOnZonas(2);
    }

    public void PlayBasicImpactEffect(Vector3 position, Color particleColor, bool sonido)
    {
        if (basicImpactParticles != null)
            SpawnVFX(basicImpactParticles, position, Color.white * 1.3f, 0.5f);

        if (sonido) PlayBasicImpactSound();
    }

    public void PlayUltraEffect(Vector3 position, Color particleColor)
    {
        if (infection1Particles != null)
            SpawnVFX(infection1Particles, position, Color.white * 1.3f, 1.0f);

        PlayRandomClip(bolaBlancaSounds, 0.85f, 1.15f, bolaBlancaVolume);
        TriggerShakeOnZonas(2);
    }

    // --- LÓGICA DE SPAWN CON POOLING ---

    private void SpawnVFX(GameObject prefab, Vector3 pos, Color color, float sizeMultiplier)
    {
        if (!poolDictionary.ContainsKey(prefab)) InitPool(prefab);

        GameObject vfx;

        if (poolDictionary[prefab].Count > 0)
        {
            vfx = poolDictionary[prefab].Dequeue();
            vfx.SetActive(true);
            vfx.transform.position = pos;
        }
        else
        {
            vfx = Instantiate(prefab, pos, Quaternion.identity);
        }

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;

            float baseSize = prefab.GetComponent<ParticleSystem>().main.startSize.constant;
            main.startSize = baseSize * sizeMultiplier;

            ps.Play();

            float totalDuration = main.duration + main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(prefab, vfx, totalDuration));
        }
    }

    private IEnumerator ReturnToPool(GameObject prefab, Vector3 pos, float delay) { yield return null; }

    private IEnumerator ReturnToPool(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (instance == null) yield break;
        instance.SetActive(false);
        if (prefab != null && poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab].Enqueue(instance);
        }
    }

    // --- HELPER METHODS ---

    // Nuevo parámetro 'volume' añadido aquí
    private void PlayRandomClip(AudioClip[] clips, float minPitch, float maxPitch, float volume)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        if (Time.time < lastSoundTime + MIN_SOUND_INTERVAL) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        // El volumen final es el volumen específico multiplicado por el Master
        float finalVolume = volume * masterSFXVolume;

        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], finalVolume);
        lastSoundTime = Time.time;
    }

    private void TriggerShakeOnZonas(int multiplier)
    {
        if (zonasCached == null) return;
        // Se asume que GameSettings existe como en el código original
        // if (GameSettings.instance != null && !GameSettings.instance.shakeEnabled) return;

        foreach (Transform zona in zonasCached)
        {
            if (zona != null)
            {
                if (activeShakes.ContainsKey(zona) && activeShakes[zona] != null)
                {
                    StopCoroutine(activeShakes[zona]);
                }
                activeShakes[zona] = StartCoroutine(ShakeObject(zona, multiplier));
            }
        }
    }

    private IEnumerator ShakeObject(Transform objTransform, int multiplier)
    {
        Vector3 anchorPos = originalPositions[objTransform];
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            if (Time.timeScale > 0)
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude * multiplier;
                float y = Random.Range(-1f, 1f) * shakeMagnitude * multiplier;

                objTransform.localPosition = anchorPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        objTransform.localPosition = anchorPos;
        activeShakes[objTransform] = null;
    }

    public void CleanAllActiveParticles()
    {
        StopAllCoroutines();
        foreach (var pool in poolDictionary.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        poolDictionary.Clear();
        activeShakes.Clear();
    }
}