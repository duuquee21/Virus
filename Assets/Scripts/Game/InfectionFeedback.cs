using UnityEngine;
using System.Collections;


public class InfectionFeedback : MonoBehaviour
{
    public static InfectionFeedback instance;

    [Header("Efectos Visuales (VFX)")]
    public GameObject infectionParticles;

    [Header("Efectos de Sonido (SFX)")]
    public AudioSource audioSource;
    public AudioClip[] infectionSounds;

    [Header("Cámara & Shake")]
    public Transform cameraTransform; // Arrastra la Cámara Principal aquí
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

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
        if (infectionParticles != null)
        {
            GameObject vfx = Instantiate(infectionParticles, position, Quaternion.identity);

            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = particleColor * 1.3f;
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

        // 3. SHAKE DE CÁMARA
        if (cameraTransform != null)
        {
            StartCoroutine(Shake());
        }
    }

    private IEnumerator Shake()
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // Genera un punto aleatorio dentro de una esfera multiplicado por la intensidad
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            cameraTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            yield return null; // Espera al siguiente frame
        }

        cameraTransform.localPosition = originalPos; // Vuelve a la normalidad
    }
}