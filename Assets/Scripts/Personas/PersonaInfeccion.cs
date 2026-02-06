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

    [Header("Recompensa Económica (Coins)")]
    [Tooltip("Dinero que da al completar cada fase intermiedia")]
    public int[] monedasPorFase = { 5, 4, 3, 2, 1 };

    [Header("Referencias Visuales")]
    public SpriteRenderer spritePersona;
    public Image fillingBarImage;
    public GameObject infectionBarCanvas;
    public Color colorCargaInicial = Color.green;
    public Color colorCargaFinal = Color.red;

    [Header("Shiny Settings")]
    public bool isShiny = false;
    public Color shinyColor = Color.yellow;
    public Sprite spriteCirculoShiny;
    public Sprite contornoShiny;

    [Header("Feedback Infección Final")]
    public float flashDuration = 0.1f;
    public float fadeDuration = 0.5f;
    public Color infectedColor = Color.red;

    private float currentInfectionTime;
    private bool isInsideZone = false;
    public bool alreadyInfected = false;
    private Color originalColor;
    private int faseActual = 0;

    public float fuerzaRetroceso = 8f; // Fuerza del empuje
    public float fuerzaRotacion = 5f; // Nueva variable para el torque
    private Transform transformInfector; // Para saber de dónde viene el virus
    private Movement movementScript;

    void Start()
    {
        movementScript = GetComponent<Movement>();
        if (spritePersona == null) spritePersona = GetComponent<SpriteRenderer>();
        originalColor = spritePersona.color;
        infectionBarCanvas.SetActive(false);
        ActualizarVisualFase();
    }

    void Update()
    {
        if (alreadyInfected) return;

        if (isInsideZone)
        {
            float multiplier = 1f;
            if (Guardado.instance != null)
                multiplier = isShiny ? Guardado.instance.shinyCaptureMultiplier : Guardado.instance.infectSpeedMultiplier;
            currentInfectionTime += Time.deltaTime * multiplier;
        }
        else
        {
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, globalInfectTime);
        float progress = currentInfectionTime / globalInfectTime;

        if (fillingBarImage != null)
        {
            fillingBarImage.fillAmount = progress;
            fillingBarImage.color = Color.Lerp(colorCargaInicial, colorCargaFinal, progress);
        }

        infectionBarCanvas.SetActive(currentInfectionTime > 0);

        if (currentInfectionTime >= globalInfectTime)
        {
            if (isShiny) BecomeInfected();
            else IntentarAvanzarFase();
        }
    }

    void IntentarAvanzarFase()
    {
        // --- DINERO POR PROGRESO ---
        if (LevelManager.instance != null && faseActual < monedasPorFase.Length)
        {
            LevelManager.instance.AddCoins(monedasPorFase[faseActual]);
        }

        // --- NUEVO: FEEDBACK DE SONIDO DE FASE ---
        if (InfectionFeedback.instance != null)
        {
            InfectionFeedback.instance.PlayPhaseChangeSound();
        }

        currentInfectionTime = 0f;
        faseActual++;
        if (faseActual < fasesSprites.Length)
        {
            ActualizarVisualFase();
            StartCoroutine(FlashCambioFase());

            if (transformInfector != null && movementScript != null)
            {
                Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;

                // Llamamos al nuevo método con torque
                movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
            }
        }
        else
        {
            BecomeInfected();
        }
    }

    void BecomeInfected()
    {
        alreadyInfected = true;
        infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, originalColor);

        StartCoroutine(InfectionColorSequence());

        // --- INFECCIÓN FINAL (CAPACIDAD) ---
        if (LevelManager.instance != null)
        {
            // Solo aquí se suma 1 a la capacidad de la ronda
            LevelManager.instance.RegisterInfection();

            if (isShiny) LevelManager.instance.RegisterShinyCapture(this);
        }
    }

    void ActualizarVisualFase()
    {
        if (isShiny)
        {
            spritePersona.sprite = spriteCirculoShiny;
            spritePersona.color = shinyColor;
            if (fillingBarImage != null && contornoShiny != null)
                fillingBarImage.sprite = contornoShiny;
        }
        else if (faseActual < fasesSprites.Length)
        {
            spritePersona.sprite = fasesSprites[faseActual];
            if (faseActual < coloresFases.Length) spritePersona.color = coloresFases[faseActual];
            if (fillingBarImage != null && faseActual < contornosFases.Length)
                fillingBarImage.sprite = contornosFases[faseActual];
        }
    }

    public void MakeShiny() { isShiny = true; ActualizarVisualFase(); transform.localScale *= 1.2f; }

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
            transformInfector = other.transform; // Guardamos la referencia
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
}