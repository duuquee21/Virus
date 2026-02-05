using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Importante para usar Corrutinas

public class PersonaInfeccion : MonoBehaviour
{
    public static float globalInfectTime = 2f;

    public SpriteRenderer spritePersona;
    public Color infectedColor = Color.red;

    public GameObject infectionBarCanvas;
    public Image fillingBarImage;

    private float currentInfectionTime;
    private bool isInsideZone = false;
    public bool alreadyInfected = false;

    [Header("Shiny Settings")]
    public bool isShiny = false;
    public Color shinyColor = Color.yellow;
    public int shinyReward = 1;

    [Header("Visual Feedback Settings")]
    public float flashDuration = 0.1f; // Cuánto tiempo se queda en blanco
    public float fadeDuration = 0.5f;  // Cuánto tarda en pasar de blanco a rojo

    private Color originalColor;


    void Start()
    {
        if (spritePersona == null)
            spritePersona = GetComponent<SpriteRenderer>();

        originalColor = spritePersona.color;   // 👈 NUEVO
        infectionBarCanvas.SetActive(false);
    }

    void Update()
    {

        if (LevelManager.instance != null && !LevelManager.instance.isGameActive)
        {
            // Opcional: Ocultar barra si el día acabó y no se infectó
            if (!alreadyInfected) infectionBarCanvas.SetActive(false);
            return;
        }
        if (alreadyInfected) return;

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
        fillingBarImage.transform.localScale = new Vector3(progress, 1f, 1f);

        if (currentInfectionTime > 0)
            infectionBarCanvas.SetActive(true);
        else
            infectionBarCanvas.SetActive(false);

        if (currentInfectionTime >= globalInfectTime)
            BecomeInfected();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alreadyInfected && other.CompareTag("InfectionZone"))
            isInsideZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("InfectionZone"))
            isInsideZone = false;
    }

    void BecomeInfected()
    {
        alreadyInfected = true;
        infectionBarCanvas.SetActive(false);

        // Iniciamos la secuencia de color
        StartCoroutine(InfectionColorSequence());

        if (LevelManager.instance != null)
            LevelManager.instance.RegisterInfection();

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, originalColor);

        if (isShiny && LevelManager.instance != null)
            LevelManager.instance.RegisterShinyCapture(this);
    }

    private IEnumerator InfectionColorSequence()
    {
        // 1. Cambio instantáneo a Blanco
        spritePersona.color = Color.white;

        // 2. Esperar un instante (el destello)
        yield return new WaitForSeconds(flashDuration);

        // 3. Fade suave hacia el color infectado
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // Interpola linealmente entre Blanco y el color final
            spritePersona.color = Color.Lerp(Color.white, infectedColor, elapsed / fadeDuration);
            yield return null;
        }

        // Asegurar que termine exactamente en el color deseado
        spritePersona.color = infectedColor;
    }

    public void MakeShiny()
    {
        isShiny = true;
        spritePersona.color = shinyColor;
        transform.localScale = transform.localScale * 1.2f;
    }
}