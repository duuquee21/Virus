using UnityEngine; 

public class InfectionFeedback : MonoBehaviour
{
    public static InfectionFeedback instance;

    [Header("Efectos Visuales (VFX)")]
    public GameObject infectionParticles; 
    
    [Header("Efectos de Sonido (SFX)")]
    public AudioSource audioSource;
    public AudioClip[] infectionSounds; 

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void PlayEffect(Vector3 position)
    {
        // 1. VISUAL
        if (infectionParticles != null)
        {
            GameObject vfx = Instantiate(infectionParticles, position, Quaternion.identity);
            Destroy(vfx, 2f); 
        }

        // 2. SONIDO
        if (audioSource != null && infectionSounds.Length > 0)
        {
            AudioClip clip = infectionSounds[Random.Range(0, infectionSounds.Length)];
            audioSource.pitch = Random.Range(0.85f, 1.15f); // Variación de tono
            audioSource.PlayOneShot(clip);
        }
    }
}