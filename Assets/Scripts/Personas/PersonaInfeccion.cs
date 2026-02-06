using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PersonaInfeccion : MonoBehaviour
{
    public static float globalInfectTime = 2f;

    [Header("Sprites de Evolución (Personaje)")]
    public Sprite[] fasesSprites; // Hex, Pent, Cuad, Tria, Circ

    [Header("Sprites de Contorno (Barra de Carga)")]
    public Sprite[] contornosFases; // 👈 NUEVO: Pon aquí los bordes (Hexágono, Pentágono, etc.)

    [Header("Colores por Fase (Normales)")]
    public Color[] coloresFases;

    [Header("Configuración de Carga Radial")]
    public Color colorCargaInicial = Color.green;
    public Color colorCargaFinal = Color.red;

    [Header("Referencias")]
    public SpriteRenderer spritePersona;
    public Image fillingBarImage;
    public GameObject infectionBarCanvas;

    private float currentInfectionTime;
    private bool isInsideZone = false;
    public bool alreadyInfected = false;

    [Header("Shiny Settings")]
    public bool isShiny = false;
    public Color shinyColor = Color.yellow;
    public Sprite spriteCirculoShiny;
    public Sprite contornoShiny; // Borde para el Shiny

    [Header("Visual Feedback Settings")]
    public float flashDuration = 0.1f;
    public float fadeDuration = 0.5f;
    public Color infectedColor = Color.red;

    private Color originalColor;
    private int faseActual = 0;

    void Start()
    {
        if (spritePersona == null)
            spritePersona = GetComponent<SpriteRenderer>();

        originalColor = spritePersona.color;
        infectionBarCanvas.SetActive(false);

        if (fillingBarImage != null) fillingBarImage.fillAmount = 0;

        ActualizarVisualFase();
    }

    void Update()
    {
        if (alreadyInfected) return;

        // --- LÓGICA DE TIEMPO ---
        if (isInsideZone)
        {
            float multiplier = 1f;
            if (Guardado.instance != null)
            {
                multiplier = isShiny ? Guardado.instance.shinyCaptureMultiplier : Guardado.instance.infectSpeedMultiplier;
            }
            currentInfectionTime += Time.deltaTime * multiplier;
        }
        else
        {
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, globalInfectTime);
        float progress = currentInfectionTime / globalInfectTime;

        // --- ACTUALIZAR BARRA DE CARGA ---
        if (fillingBarImage != null)
        {
            fillingBarImage.fillAmount = progress;
            // El borde cambia de color según el progreso
            fillingBarImage.color = Color.Lerp(colorCargaInicial, colorCargaFinal, progress);
        }

        // Mostrar el Canvas solo cuando hay progreso
        infectionBarCanvas.SetActive(currentInfectionTime > 0);

        if (currentInfectionTime >= globalInfectTime)
        {
            if (isShiny) BecomeInfected();
            else IntentarAvanzarFase();
        }
    }

    void IntentarAvanzarFase()
    {
        currentInfectionTime = 0f;
        faseActual++;

        if (faseActual < fasesSprites.Length)
        {
            ActualizarVisualFase();
            StartCoroutine(FlashCambioFase());
        }
        else
        {
            BecomeInfected();
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
            // Cambiamos el cuerpo de la persona
            spritePersona.sprite = fasesSprites[faseActual];

            if (faseActual < coloresFases.Length)
                spritePersona.color = coloresFases[faseActual];

            // 👈 TU IDEA BRILLANTE: Cambiamos el dibujo de la barra de carga para que coincida
            if (fillingBarImage != null && faseActual < contornosFases.Length)
            {
                fillingBarImage.sprite = contornosFases[faseActual];
            }
        }
    }

    public void MakeShiny()
    {
        isShiny = true;
        ActualizarVisualFase();
        transform.localScale = transform.localScale * 1.2f;
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
        if (!alreadyInfected && other.CompareTag("InfectionZone")) isInsideZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("InfectionZone")) isInsideZone = false;
    }

    void BecomeInfected()
    {
        alreadyInfected = true;
        infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, originalColor);

        StartCoroutine(InfectionColorSequence());

        if (LevelManager.instance != null) LevelManager.instance.RegisterInfection();
        if (isShiny && LevelManager.instance != null) LevelManager.instance.RegisterShinyCapture(this);
    }

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