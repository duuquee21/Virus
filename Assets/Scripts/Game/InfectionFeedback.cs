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

    [Header("Efectos de Sonido (SFX)")]
    public AudioSource audioSource;
    private float lastSoundTime;
    private const float MIN_SOUND_INTERVAL = 0.05f;

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

    private Transform[] zonasCached; // Añade esta variable arriba

    // --- LÓGICA DE POOLING ---
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<Transform, Coroutine> activeShakes = new Dictionary<Transform, Coroutine>();

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

        // Pre-inicializar pools para evitar tirones en runtime
        InitPool(infectionParticles);
        InitPool(infection1Particles);
        InitPool(basicImpactParticles);
    }

    void Start()
    {
        // Esto busca TODOS los objetos de tipo Transform en la escena, incluso desactivados
        Transform[] todosLosTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        List<Transform> listaZonas = new List<Transform>();

        foreach (Transform t in todosLosTransforms)
        {
            // Filtramos por Tag y nos aseguramos que pertenezcan a la escena (no sean prefabs en la carpeta)
            if (t.CompareTag(zonaTag) && t.gameObject.scene.name != null)
            {
                listaZonas.Add(t);
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

    // --- MÉTODOS ORIGINALES MANTENIDOS ---

    public void PlayEffect(Vector3 position, Color particleColor, bool mute)
    {
        if (infection1Particles != null)
            SpawnVFX(infection1Particles, position, particleColor, 1.0f);

        if (!mute)
            PlayRandomClip(infectionSounds, 0.85f, 1.15f);
    }

    public void PlayPhaseChangeEffect(Vector3 position, Color particleColor)
    {
        if (infectionParticles != null)
            SpawnVFX(infectionParticles, position, Color.white * 1.3f, 1.0f);

        PlayPhaseChangeSound();
    }

    public void PlayPhaseChangeSound() => PlayRandomClip(phaseChangeSounds, 1.1f, 1.3f);
    public void PlayBasicImpactSoundAgainstWall() => PlayRandomClip(basicWallImpactSounds, 1.1f, 1.3f);
    public void PlayBasicImpactSound() => PlayRandomClip(basicImpactSounds, 1.1f, 1.3f);

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

        PlayRandomClip(bolaBlancaSounds, 0.85f, 1.15f);
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

            // --- CORRECCIÓN AQUÍ ---
            // Usamos el valor del prefab original para que el multiplicador no sea acumulativo
            float baseSize = prefab.GetComponent<ParticleSystem>().main.startSize.constant;
            main.startSize = baseSize * sizeMultiplier;

            ps.Play();

            // Calculamos el tiempo total de vida para saber cuándo apagarlo
            float totalDuration = main.duration + main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(prefab, vfx, totalDuration));
        }
    }

    private IEnumerator ReturnToPool(GameObject prefab, Vector3 pos, float delay) { yield return null; } // Firma dummy para no romper lógica

    private IEnumerator ReturnToPool(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check if the object still exists before touching it
        if (instance == null) yield break;

        instance.SetActive(false);

        // Check if the prefab/pool still exists (in case CleanAllActiveParticles was called)
        if (prefab != null && poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab].Enqueue(instance);
        }
    }

    // --- HELPER METHODS ---

    private void PlayRandomClip(AudioClip[] clips, float minPitch, float maxPitch)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        if (Time.time < lastSoundTime + MIN_SOUND_INTERVAL) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], 0.8f);
        lastSoundTime = Time.time;
    }

    private void TriggerShakeOnZonas(int multiplier)
    {
        if (zonasCached == null) return;
        if (GameSettings.instance != null && !GameSettings.instance.shakeEnabled) return;
        foreach (Transform zona in zonasCached)
        {
            if (zona != null)
            {
                // Si ya está vibrando, la detenemos para no acumular offsets
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
        Vector3 originalPos = objTransform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            objTransform.localPosition = originalPos + new Vector3(Random.Range(-1f, 1f) * shakeMagnitude * multiplier, Random.Range(-1f, 1f) * shakeMagnitude * multiplier, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localPosition = originalPos;
    }

    public void CleanAllActiveParticles()
    {
        // Stop all pending ReturnToPool routines to prevent them from accessing null refs
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

        // Also clear the active shakes dictionary since those coroutines were just stopped
        activeShakes.Clear();
    }
}