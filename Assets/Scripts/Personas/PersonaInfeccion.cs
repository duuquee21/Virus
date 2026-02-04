using UnityEngine;
using UnityEngine.UI;

public class PersonaInfeccion : MonoBehaviour
{
    // ⏱ Tiempo global base para infectar a alguien
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

    void Start()
    {
        if (spritePersona == null)
            spritePersona = GetComponent<SpriteRenderer>();

        infectionBarCanvas.SetActive(false);
    }

    void Update()
    {
        if (alreadyInfected) return;

        if (isInsideZone)
        {
            // --- LÓGICA DE VELOCIDAD DIFERENCIADA ---
            float multiplier = 1f;

            if (Guardado.instance != null)
            {
                if (isShiny)
                {
                    // Si es Shiny, usamos el multiplicador del árbol de habilidades "Shiny Capture"
                    multiplier = Guardado.instance.shinyCaptureMultiplier;
                }
                else
                {
                    // Si es normal, usamos el multiplicador de "Infect Speed" normal
                    multiplier = Guardado.instance.infectSpeedMultiplier;
                }
            }

            // Aplicamos el multiplicador al progreso
            currentInfectionTime += Time.deltaTime * multiplier;
        }
        else
        {
            // Si sale de la zona, la barra baja (el doble de rápido para penalizar)
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        // Aseguramos que el tiempo esté entre 0 y el máximo
        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, globalInfectTime);

        // Actualizamos la barra visualmente
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
        spritePersona.color = infectedColor;

        if (LevelManager.instance != null)
        {
            LevelManager.instance.RegisterInfection();
        }

        if (InfectionFeedback.instance != null)
        {
            InfectionFeedback.instance.PlayEffect(transform.position);
        }

        if (isShiny && LevelManager.instance != null)
        {
            LevelManager.instance.RegisterShinyCapture(this);
        }
    }

    public void MakeShiny()
    {
        isShiny = true;
        spritePersona.color = shinyColor;
        transform.localScale = transform.localScale * 1.2f;
    }
}