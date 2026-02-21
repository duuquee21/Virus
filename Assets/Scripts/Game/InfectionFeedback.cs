using UnityEngine;
using System.Collections;


public class InfectionFeedback : MonoBehaviour
{
    public static InfectionFeedback instance;

    [Header("Efectos Visuales (VFX)")]
    public GameObject infectionParticles;
    public GameObject infection1Particles;
    public GameObject basicImpactParticles;

    [Header("Efectos de Sonido (SFX)")]
    public AudioSource audioSource;
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
        if (instance == null) instance = this;

        // Si no asignas la cámara, intenta buscar la principal por defecto
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public void PlayEffect(Vector3 position, Color particleColor)
    {
        // 1. VISUAL
        if (infection1Particles != null)
        {
            GameObject vfx = Instantiate(infection1Particles, position, Quaternion.identity);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.white * 1.3f;
            }

            Destroy(vfx, 2f);
        }

        // 2. SONIDO
        if (audioSource != null && infectionSounds.Length > 0)
        {

            AudioClip clip = infectionSounds[Random.Range(0, infectionSounds.Length)];



            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(clip);
        }

     
    }

    public void PlayPhaseChangeEffect(Vector3 position, Color particleColor)
    {
        // 1. VISUAL
        if (infectionParticles != null)
        {
            GameObject vfx = Instantiate(infectionParticles, position, Quaternion.identity);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.white * 1.3f;
            }

            Destroy(vfx, 2f);
        }

        PlayPhaseChangeSound();

    }

    public void PlayPhaseChangeSound()
    {
        if (audioSource != null && phaseChangeSounds.Length > 0)
        {
            AudioClip clip = phaseChangeSounds[Random.Range(0, phaseChangeSounds.Length)];
            // Un pitch un poco más alto para que suene "progresivo"
            audioSource.pitch = Random.Range(1.1f, 1.3f);
            audioSource.PlayOneShot(clip);
        }

    }
    public void PlayBasicImpactSoundAgainstWall()
    {
        if (audioSource != null && basicWallImpactSounds.Length > 0)
        {
            AudioClip clip = basicWallImpactSounds[Random.Range(0, basicWallImpactSounds.Length)];
            // Un pitch un poco más alto para que suene "progresivo"
            audioSource.pitch = Random.Range(1.1f, 1.3f);
            audioSource.PlayOneShot(clip);
        }

    }
    public void PlayBasicImpactSound()
    {
        if (audioSource != null && basicImpactSounds.Length > 0)
        {
            AudioClip clip = basicImpactSounds[Random.Range(0, basicImpactSounds.Length)];
            // Un pitch un poco más alto para que suene "progresivo"
            audioSource.pitch = Random.Range(1.1f, 1.3f);
            audioSource.PlayOneShot(clip);
        }

    }

    public void PlayBasicImpactEffectAgainstWall(Vector3 position, Color particleColor)
    {
        // 1. VISUAL
        if (basicImpactParticles != null)
        {
            GameObject vfx = Instantiate(basicImpactParticles, position, Quaternion.identity);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.white * 1.3f;
            }

            Destroy(vfx, 2f);
        }

        PlayBasicImpactSoundAgainstWall();

        GameObject[] zonas = GameObject.FindGameObjectsWithTag(zonaTag);
        foreach (GameObject zona in zonas)
        {
            StartCoroutine(ShakeObject(zona.transform, 2)); // Cambia el 2 por el multiplicador que quieras
        }
    }

    public void PlayBasicImpactEffect(Vector3 position, Color particleColor, bool sonido)
    {
        // 1. VISUAL
        if (basicImpactParticles != null)
        {

            GameObject vfx = Instantiate(basicImpactParticles, position, Quaternion.identity);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.white * 1.3f;

                // Accedemos al valor constante actual y lo multiplicamos
                float currentSize = main.startSize.constant;
                main.startSize = currentSize * 0.5f;
            }

            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.white * 1.3f;
            }

            Destroy(vfx, 2f);
        }
        if (sonido)
        {
            PlayBasicImpactSound();
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

            objTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localPosition = originalPos;
    }

    public void PlayUltraEffect(Vector3 position, Color particleColor)
    {
        // 1. VISUAL
        if (infection1Particles != null)
        {
            GameObject vfx = Instantiate(infection1Particles, position, Quaternion.identity);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.white * 1.3f;
            }

            Destroy(vfx, 2f);
        }

        // 2. SONIDO
        if (audioSource != null && bolaBlancaSounds.Length > 0)
        {

            AudioClip clip = bolaBlancaSounds[Random.Range(0, bolaBlancaSounds.Length)];



            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(clip);
        }

        GameObject[] zonas = GameObject.FindGameObjectsWithTag(zonaTag);
        foreach (GameObject zona in zonas)
        {
            StartCoroutine(ShakeObject(zona.transform, 2)); // Cambia el 2 por el multiplicador que quieras
        }
    }


}