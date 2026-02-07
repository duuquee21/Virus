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

    [Header("Referencias Visuales")]
    public SpriteRenderer spritePersona;
    // La lista de imágenes de las barras (una por cada fase)
    public Image[] fillingBarImages;
    public GameObject infectionBarCanvas;
    public Color colorCargaInicial = Color.green;
    public Color colorCargaFinal = Color.red;

    // ... (resto de variables como monedasPorFase, shiny, etc., se mantienen igual) ...
    [Header("Recompensa Económica (Coins)")]
    public int[] monedasPorFase = { 5, 4, 3, 2, 1 };

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

    public float fuerzaRetroceso = 8f;
    public float fuerzaRotacion = 5f;
    private Transform transformInfector;
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

        // 1. Lógica de incremento/decremento
        if (isInsideZone)
        {
            float multiplier = 1f;
            if (Guardado.instance != null)
                multiplier = isShiny ? Guardado.instance.shinyCaptureMultiplier : Guardado.instance.infectSpeedMultiplier;

            currentInfectionTime += Time.deltaTime * multiplier;
        }
        else
        {
            // Si quieres que la barra se recupere (se llene de nuevo) al salir:
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, globalInfectTime);
        float progress = currentInfectionTime / globalInfectTime;

        // 2. Mantener el Canvas siempre encendido
        // Eliminamos: infectionBarCanvas.SetActive(currentInfectionTime > 0 || faseActual > 0);
        if (!infectionBarCanvas.activeSelf)
        {
            infectionBarCanvas.SetActive(true);
        }

        ActualizarProgresoBarras(progress);

        if (currentInfectionTime >= globalInfectTime)
        {
            if (isShiny) BecomeInfected();
            else IntentarAvanzarFase();
        }
    }
    void ActualizarProgresoBarras(float progress)
    {
        if (fillingBarImages == null) return;

        float inverseProgress = 1f - progress; // 1 = Llena, 0 = Vacía

        for (int i = 0; i < fillingBarImages.Length; i++)
        {
            if (fillingBarImages[i] == null) continue;

            if (i == faseActual)
            {
                fillingBarImages[i].gameObject.SetActive(true);
                fillingBarImages[i].fillAmount = inverseProgress;

                // Color: Lerp de Verde (lleno) a Rojo (vacío)
                fillingBarImages[i].color = Color.Lerp(colorCargaFinal, colorCargaInicial, inverseProgress);
            }
            else
            {
                fillingBarImages[i].gameObject.SetActive(false);
            }
        }
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

            // --- CORRECCIÓN AQUÍ ---
            if (InfectionFeedback.instance != null)
            {
                // Ahora llamamos al efecto visual y de sonido completo
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);
            }
            // -----------------------

            if (transformInfector != null && movementScript != null)
            {
                Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;
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

        if (LevelManager.instance != null)
        {
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

            // Si es Shiny, podrías querer que solo se vea la primera barra con el contorno shiny
            if (fillingBarImages.Length > 0 && fillingBarImages[0] != null)
                fillingBarImages[0].sprite = contornoShiny;
        }
        else if (faseActual < fasesSprites.Length)
        {
            spritePersona.sprite = fasesSprites[faseActual];
            if (faseActual < coloresFases.Length) spritePersona.color = coloresFases[faseActual];

            // Asignar el contorno correspondiente a cada barra según su fase
            for (int i = 0; i < fillingBarImages.Length; i++)
            {
                if (fillingBarImages[i] != null && i < contornosFases.Length)
                {
                    fillingBarImages[i].sprite = contornosFases[i];
                }
            }
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

    public void IntentarAvanzarFasePorChoque()
    {
        // Solo avanza si NO está infectado Y si NO es la última fase disponible
        // (fasesSprites.Length - 1) es el índice de la última fase antes de la infección total
        if (!alreadyInfected && faseActual < fasesSprites.Length - 1)
        {
            Debug.Log($"<color=cyan>Choque fuerte: Avanzando a fase {faseActual + 1}</color>");

            // Copiamos la lógica de avance pero sin riesgo de llegar a BecomeInfected()
            if (LevelManager.instance != null && faseActual < monedasPorFase.Length)
            {
                LevelManager.instance.AddCoins(monedasPorFase[faseActual]);
            }

            if (InfectionFeedback.instance != null)
            {
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);
            }

            currentInfectionTime = 0f;
            faseActual++;
            ActualizarVisualFase();
            StartCoroutine(FlashCambioFase());
        }
        else if (faseActual >= fasesSprites.Length - 1)
        {
            Debug.Log("<color=orange>Choque detectado, pero ya está en la fase máxima. No se infectará por choque.</color>");
        }
    }

    void ActualizarSpritesDeBarras(Sprite nuevoSprite)
    {
        if (fillingBarImages == null || nuevoSprite == null) return;

        foreach (Image img in fillingBarImages)
        {
            if (img != null) img.sprite = nuevoSprite;
        }
    }
}