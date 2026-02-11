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

    [Header("Modo Musical (Racha)")]
    public bool activarModoMusical = true; 
    [Tooltip("Arrastra aquí el sonido 'Ding' o 'Blip' para la racha")]
    public AudioClip sonidoMusicalCombo; 
    
    public float tiempoParaResetear = 2.0f; 

    [Header("Ajustes de Limpieza")]
    public float cooldownSonido = 0.15f; 
    private float tiempoUltimoSonidoJugado = -1f;

    // Escala Arcade
    private float[] escalaMayor = new float[] { 
        1.0f, 1.25f, 1.5f, 1.75f, 2.0f, 2.5f, 3.0f 
    };

    private int indiceNotaActual = 0; 
    private float ultimoTiempoInfeccion;

    [Header("Cámara & Shake")]
    public Transform cameraTransform; 
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    void Awake()
    {
        if (instance == null) instance = this;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    // --- CAMBIO: Ahora devuelve el ÍNDICE (0, 1, 2...) no el pitch ---
    private int ActualizarYObtenerNota()
    {
        // Si pasó mucho tiempo, reseteamos a 0 (Primer golpe)
        if (Time.time - ultimoTiempoInfeccion > tiempoParaResetear)
        {
            indiceNotaActual = 0;
        }
        else
        {
            // Si estamos en racha, sumamos
            indiceNotaActual++;
        }

        // Tope
        if (indiceNotaActual >= escalaMayor.Length)
        {
            indiceNotaActual = escalaMayor.Length - 1;
        }

        ultimoTiempoInfeccion = Time.time;
        return indiceNotaActual;
    }

    private bool PuedeSonar()
    {
        if (Time.time - tiempoUltimoSonidoJugado < cooldownSonido) return false; 
        tiempoUltimoSonidoJugado = Time.time;
        return true;
    }

    public void PlayEffect(Vector3 position, Color particleColor )
    {
        // VFX
        if (infection1Particles != null)
        {
            GameObject vfx = Instantiate(infection1Particles, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // SONIDO
        if (audioSource != null && PuedeSonar()) 
        {
            ProcesarSonidoLogico(infectionSounds);
        }

        if (cameraTransform != null) StartCoroutine(Shake(2));
    }

    public void PlayPhaseChangeEffect(Vector3 position, Color particleColor)
    {
        if (infectionParticles != null)
        {
            GameObject vfx = Instantiate(infectionParticles, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        PlayPhaseChangeSound();
    }

    public void PlayPhaseChangeSound()
    {
        if (audioSource != null && PuedeSonar()) 
        {
            ProcesarSonidoLogico(phaseChangeSounds);
        }
    }

    // --- FUNCIÓN MAESTRA QUE DECIDE QUÉ SONIDO TOCAR ---
    private void ProcesarSonidoLogico(AudioClip[] sonidosPorDefecto)
    {
        if (activarModoMusical && sonidoMusicalCombo != null)
        {
            // 1. Calculamos en qué nota estamos
            int nota = ActualizarYObtenerNota();

            // 2. DECISIÓN:
            if (nota == 0) 
            {
                // ES EL PRIMER GOLPE: Suena "normal" (sonido de impacto básico o el de la lista)
                // Usamos el sonido básico para no gastar el efecto especial todavía
                if (sonidosPorDefecto.Length > 0)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(sonidosPorDefecto[Random.Range(0, sonidosPorDefecto.Length)]);
                }
            }
            else 
            {
                // ES EL SEGUNDO GOLPE O MÁS: ¡MÚSICA!
                audioSource.pitch = escalaMayor[nota];
                audioSource.PlayOneShot(sonidoMusicalCombo);
            }
        }
        else if (sonidosPorDefecto.Length > 0)
        {
            // Modo clásico (sin música activada)
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidosPorDefecto[Random.Range(0, sonidosPorDefecto.Length)]);
        }
    }

    // --- RESTO IGUAL ---
    public void PlayBasicImpactSoundAgainstWall() { if(audioSource!=null) audioSource.PlayOneShot(basicWallImpactSounds[0]); }
    public void PlayBasicImpactSound() { if(audioSource!=null) audioSource.PlayOneShot(basicImpactSounds[0]); }

    public void PlayBasicImpactEffectAgainstWall(Vector3 position, Color particleColor)
    {
        if (basicImpactParticles != null) Instantiate(basicImpactParticles, position, Quaternion.identity);
        PlayBasicImpactSoundAgainstWall();
        if (cameraTransform != null) StartCoroutine(Shake(1));
    }

    public void PlayBasicImpactEffect(Vector3 position, Color particleColor, bool sonido)
    {
        if (basicImpactParticles != null)
        {
            GameObject vfx = Instantiate(basicImpactParticles, position, Quaternion.identity);
            if (vfx.GetComponent<ParticleSystem>() != null) 
            {
                var main = vfx.GetComponent<ParticleSystem>().main;
                main.startSize = main.startSize.constant * 0.5f;
            }
            Destroy(vfx, 2f);
        }
        if (sonido) PlayBasicImpactSound();
    }

    public void PlayUltraEffect(Vector3 position, Color particleColor)
    {
        if (infection1Particles != null) Instantiate(infection1Particles, position, Quaternion.identity);
        if (audioSource != null && bolaBlancaSounds.Length > 0)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f); 
            audioSource.PlayOneShot(bolaBlancaSounds[Random.Range(0, bolaBlancaSounds.Length)]);
        }
        if (cameraTransform != null) StartCoroutine(Shake(3));
    }

    private IEnumerator Shake(int ShakeAmountMultiplier)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude * ShakeAmountMultiplier;
            float y = Random.Range(-1f, 1f) * shakeMagnitude * ShakeAmountMultiplier;
            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cameraTransform.localPosition = originalPos;
    }
}