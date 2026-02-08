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
    public Image[] fillingBarImages;
    public GameObject infectionBarCanvas;
    public Color colorCargaInicial = Color.green;
    public Color colorCargaFinal = Color.red;

    [Header("Recompensa Económica (Coins)")]
    public int[] monedasPorFase = { 5, 4, 3, 2, 1 };

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

        if (isInsideZone)
        {
            // Ahora solo usa el multiplicador de velocidad de infección normal
            float multiplier = (Guardado.instance != null) ? Guardado.instance.infectSpeedMultiplier : 1f;
            currentInfectionTime += Time.deltaTime * multiplier;
        }
        else
        {
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, globalInfectTime);
        float progress = currentInfectionTime / globalInfectTime;

        if (!infectionBarCanvas.activeSelf && currentInfectionTime > 0)
        {
            infectionBarCanvas.SetActive(true);
        }

        ActualizarProgresoBarras(progress);

        if (currentInfectionTime >= globalInfectTime)
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
            {
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);
            }

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
        }
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
                {
                    fillingBarImages[i].sprite = contornosFases[i];
                }
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
        if (!alreadyInfected && faseActual < fasesSprites.Length - 1)
        {
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
    }
}
