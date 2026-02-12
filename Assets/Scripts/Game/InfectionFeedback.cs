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

    [Header("Optimización")]
    public float tiempoMinimoEntreImpactos = 0.05f; // Máximo 20 efectos por segundo
    private float ultimoTiempoImpacto;
    private bool isScaling = false;
    private readonly Vector3 escalaRealConstante = new Vector3(0.4f, 0.4f, 0.4f);
    private float escalaActualAcumulada = 0.4f;

    private Coroutine feedbackEscalaCoroutine;

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

        // 3. SHAKE DE CÁMARA
        if (cameraTransform != null)
        {
            StartCoroutine(Shake(2));
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

        // 3. SHAKE DE CÁMARA
        if (cameraTransform != null)
        {
            StartCoroutine(Shake(1));
        }
    }

    public void PlayBasicImpactEffect(Vector3 position, Color particleColor, bool sonido)
    {

        if (Time.time - ultimoTiempoImpacto < tiempoMinimoEntreImpactos)
        {
            // Opcional: Instanciar una partícula simple sin sonido
            return;
        }
        ultimoTiempoImpacto = Time.time;
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


    private IEnumerator Shake(int ShakeAmountMultiplier)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // Genera un punto aleatorio dentro de una esfera multiplicado por la intensidad
            float x = Random.Range(-1f, 1f) * shakeMagnitude * ShakeAmountMultiplier;
            float y = Random.Range(-1f, 1f) * shakeMagnitude * ShakeAmountMultiplier;

            cameraTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            yield return null; // Espera al siguiente frame
        }

        cameraTransform.localPosition = originalPos; // Vuelve a la normalidad
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

        // 3. SHAKE DE CÁMARA
        if (cameraTransform != null)
        {
            StartCoroutine(Shake(3));
        }
    }
    public void PlayHitFeedback(GameObject target, float incremento = 0.03f)
    {
        if (target == null) return;

        // 1. Detener cualquier movimiento de escala previo para que no haya saltos
        if (feedbackEscalaCoroutine != null) StopCoroutine(feedbackEscalaCoroutine);

        // 2. Aumentar el progreso
        escalaActualAcumulada += incremento;

        // 3. ¿Llegamos al final?
        if (escalaActualAcumulada >= 1.0f)
        {
            // Forzamos escala 1 y disparamos animación
            target.transform.localScale = Vector3.one;

            AnimacionFinalNivel anim = GetComponent<AnimacionFinalNivel>();
            if (anim != null) anim.Ejecutar();

            // RESET ABSOLUTO: Volvemos a los valores iniciales
            escalaActualAcumulada = 0.4f;
            target.transform.localScale = escalaRealConstante;
            isScaling = false;
        }
        else
        {
            // 4. Si no es el final, hacemos el Flash y el crecimiento suave
            feedbackEscalaCoroutine = StartCoroutine(HitFeedbackCoroutine(target, escalaActualAcumulada));
        }
    }

    private IEnumerator HitFeedbackCoroutine(GameObject target, float metaEscala)
    {
        Transform t = target.transform;
        Renderer rend = target.GetComponent<Renderer>();

        // --- EFECTO FLASH ---
        if (rend != null) rend.material.color = Color.white * 4f; // Flash brillante

        // --- EFECTO BUMP (Golpe visual) ---
        // Hacemos que crezca un poco más de su meta para que se note el impacto
        Vector3 escalaImpacto = Vector3.one * (metaEscala + 0.08f);
        t.localScale = escalaImpacto;

        yield return new WaitForSeconds(0.04f); // Flash muy rápido

        // --- RETORNO A LA ESCALA META ---
        if (rend != null) rend.material.color = Color.white; // Volver a color normal

        float elapsed = 0;
        float duration = 0.15f; // Tiempo que tarda en asentarse en su nueva escala
        Vector3 escalaMetaFinal = Vector3.one * metaEscala;

        while (elapsed < duration)
        {
            if (t == null) yield break;
            t.localScale = Vector3.Lerp(escalaImpacto, escalaMetaFinal, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        t.localScale = escalaMetaFinal;
    }
    public void ResetEscalaJugador(Transform target)
    {
        escalaActualAcumulada = 0.4f;
        if (target != null) target.localScale = escalaRealConstante;
        isScaling = false;
    }

}