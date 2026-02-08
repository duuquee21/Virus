using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PersonaInfeccion : MonoBehaviour
{
    public static float globalInfectTime = 2f;

    [Header("Sprites de Evolución")]
    public Sprite[] fasesSprites;
    public Sprite[] contornosFases;
    public Color[] coloresFases;

    [Header("Dificultad por Fase")]
    [Tooltip("Multiplicador de tiempo: 1 = 2s, 1.5 = 3s, etc.")]
    public float[] resistenciaPorFase = { 1f, 1.2f, 1.5f, 1.8f, 2.2f };

    [Header("Recompensa Económica (Coins)")]
    public int[] monedasPorFase = { 5, 4, 3, 2, 1 };

    [Header("Referencias Visuales")]
    public SpriteRenderer spritePersona;
    public Image[] fillingBarImages;
    public GameObject infectionBarCanvas;
    public Color colorCargaInicial = Color.green;
    public Color colorCargaFinal = Color.red;

    [Header("Feedback Infección Final")]
    public float flashDuration = 0.1f;
    public float fadeDuration = 0.5f;
    public Color infectedColor = Color.red;

    [Header("Físicas")]
    public float fuerzaRetroceso = 8f;
    public float fuerzaRotacion = 5f;

    private float currentInfectionTime;
    private bool isInsideZone = false;
    public bool alreadyInfected = false;
    private Color originalColor;
    private int faseActual = 0;
    private Transform transformInfector;
    private Movement movementScript;

    public ParticleSystem particulasDeFuego;
    void Start()
    {
        movementScript = GetComponent<Movement>();
        if (spritePersona == null) spritePersona = GetComponent<SpriteRenderer>();
        originalColor = spritePersona.color;

        if (infectionBarCanvas != null) infectionBarCanvas.SetActive(true);

        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
    }

    void Update()
    {
        if (alreadyInfected)
        {
 
            return;
        }

        float resistenciaActual = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
        float tiempoNecesarioEstaFase = globalInfectTime * resistenciaActual;

        if (isInsideZone)
        {
            float multiplier = 1f;
            if (Guardado.instance != null) multiplier = Guardado.instance.infectSpeedMultiplier;
            currentInfectionTime += Time.deltaTime * multiplier;
        }
        else
        {
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, tiempoNecesarioEstaFase);
        float progress = currentInfectionTime / tiempoNecesarioEstaFase;

        if (!infectionBarCanvas.activeSelf) infectionBarCanvas.SetActive(true);

        ActualizarProgresoBarras(progress);

        if (currentInfectionTime >= tiempoNecesarioEstaFase)
        {
            IntentarAvanzarFase();
        }

        
    }

    void ActualizarProgresoBarras(float progress)
    {
        if (fillingBarImages == null) return;
        float inverseProgress = 1f - progress;

        for (int i = 0; i < fillingBarImages.Length; i++)
        {
            if (fillingBarImages[i] == null) continue;

            if (i == faseActual)
            {
                fillingBarImages[i].gameObject.SetActive(true);
                fillingBarImages[i].fillAmount = inverseProgress;
                fillingBarImages[i].color = Color.Lerp(colorCargaFinal, colorCargaInicial, inverseProgress);
            }
            else
            {
                fillingBarImages[i].gameObject.SetActive(false);
            }
        }
    }

    // --- NUEVA FUNCIÓN PARA EL CLON ---
    public void EstablecerFaseDirecta(int fase)
    {
        faseActual = fase;
        currentInfectionTime = 0f;
        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
    }

    void IntentarAvanzarFase()
    {
        if (LevelManager.instance != null && faseActual < monedasPorFase.Length)
            LevelManager.instance.AddCoins(monedasPorFase[faseActual]);

        currentInfectionTime = 0f;
        faseActual++;

        if (faseActual < fasesSprites.Length)
        {
            ActualizarVisualFase();
            StartCoroutine(FlashCambioFase());

            if (InfectionFeedback.instance != null)
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);

            if (transformInfector != null && movementScript != null)
            {
                Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;
                movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
            }
        }
        else
        {
            BecomeInfected();
            if (transformInfector != null && movementScript != null)
            {
                Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;
                movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
            }
        }
    }

    void BecomeInfected()
    {
       
        alreadyInfected = true;
        infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, originalColor);
        particulasDeFuego?.Play();

        if (LevelManager.instance != null) LevelManager.instance.RegisterInfection();

        StartCoroutine(InfectionColorSequence());
    }

    void ActualizarVisualFase()
    {
        if (faseActual < fasesSprites.Length)
        {
            spritePersona.sprite = fasesSprites[faseActual];
            if (faseActual < coloresFases.Length) spritePersona.color = coloresFases[faseActual];

            for (int i = 0; i < fillingBarImages.Length; i++)
            {
                if (fillingBarImages[i] != null && i < contornosFases.Length)
                    fillingBarImages[i].sprite = contornosFases[i];
            }
        }
    }

    private IEnumerator FlashCambioFase()
    {
        Color col = spritePersona.color;
        spritePersona.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        spritePersona.color = col;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alreadyInfected && other.CompareTag("InfectionZone"))
        {
            isInsideZone = true;
            transformInfector = other.transform;
        }
    }
    void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("InfectionZone")) isInsideZone = false; }

    private IEnumerator InfectionColorSequence()
    {
        spritePersona.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            spritePersona.color = Color.Lerp(Color.white, infectedColor, elapsed / fadeDuration);
            yield return null;
        }
        spritePersona.color = infectedColor;
    }

    public void IntentarAvanzarFasePorChoque()
    {
        if (alreadyInfected) return;

        // --- LÓGICA DE HABILIDAD CON DEBUGS ---
        if (Guardado.instance != null)
        {
            float probActual = Guardado.instance.probabilidadDuplicarChoque;
            float dado = Random.value; // Tira un dado entre 0.0 y 1.0

            Debug.Log($"<color=cyan>[Habilidad Duplicar]</color> Probabilidad: {probActual * 100}% | Dado sacado: {dado:F2}");

            if (dado <= probActual)
            {
                PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
                if (pm != null)
                {
                    Debug.Log("<color=magenta><b>¡ÉXITO!</b></color> Duplicando persona en fase: " + faseActual);
                    pm.InstanciarCopia(transform.position, faseActual, this.gameObject);
                }
            }
            else
            {
                Debug.Log("<color=red>Fallo:</color> El dado fue mayor que la probabilidad.");
            }
        }
        else
        {
            Debug.LogWarning("No se encontró la instancia de Guardado para verificar la probabilidad.");
        }

        // --- LÓGICA NORMAL DE AVANCE ---
        if (faseActual < fasesSprites.Length - 1)
        {
            if (LevelManager.instance != null && faseActual < monedasPorFase.Length)
                LevelManager.instance.AddCoins(monedasPorFase[faseActual]);

            if (InfectionFeedback.instance != null)
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);

            currentInfectionTime = 0f;
            faseActual++;
            ActualizarVisualFase();
            StartCoroutine(FlashCambioFase());
        }
        else
        {
            BecomeInfected();
        }
    }

    public bool EsFaseMaxima() => faseActual >= fasesSprites.Length - 1;
}