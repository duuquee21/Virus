using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndDayResultsPanel : MonoBehaviour
{
    public static EndDayResultsPanel instance;

    [Header("UI")]
    public GameObject panel;

    [Header("Evoluciones")]
    public TextMeshProUGUI zonaEvolutionText;
    public TextMeshProUGUI choqueEvolutionText;
    public TextMeshProUGUI carambolaEvolutionText;

    [Header("Button Config")]
    public Button continueButton;
    public TextMeshProUGUI buttonText;

    [Header("Monedas")]
    public TextMeshProUGUI monedasPartidaText;
    public TextMeshProUGUI monedasTotalesText;


    [Header("Timing")]
    public float letterSpeed = 0.03f;
    public float stepDelay = 0.5f;

    private bool isAnimating = false;
    private bool skipRequested = false;

    private string fullZonaEvolution;
    private string fullChoqueEvolution;
    private string fullCarambolaEvolution;

    private string fullMonedasPartida;
    private string fullMonedasTotales;


    // Nombres reales de tus fases (orden correcto según tu sistema)
    private readonly string[] nombresFases =
    {
        "Hexágono",
        "Pentágono",
        "Cuadrado",
        "Triángulo",
        "Círculo"
    };

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    public void ShowResults(int monedasGanadas, int monedasTotales)
    {
        Time.timeScale = 0f;
        // ---- MONEDAS ----
        fullMonedasPartida = "<b>Monedas ganadas:</b> " + monedasGanadas;
        fullMonedasTotales = "<b>Monedas totales:</b> " + monedasTotales;

        // ---------------- ZONA ----------------
        fullZonaEvolution = "<b>Evoluciones por Zona</b>\n\n";

        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length - 1; i++)
        {
            fullZonaEvolution +=
                nombresFases[i] + " → " + nombresFases[i + 1] + ": " +
                PersonaInfeccion.evolucionesEntreFases[i] + "\n";
        }

        // ---------------- PARED ----------------
        fullChoqueEvolution = "\n<b>Evoluciones por Pared</b>\n\n";

        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length - 1; i++)
        {
            fullChoqueEvolution +=
                nombresFases[i] + " → " + nombresFases[i + 1] + ": " +
                PersonaInfeccion.evolucionesPorChoque[i] + "\n";
        }

        // ---------------- CARAMBOLA ----------------
        fullCarambolaEvolution = "\n<b>Evoluciones por Carambola</b>\n\n";

        for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length - 1; i++)
        {
            fullCarambolaEvolution +=
                nombresFases[i] + " → " + nombresFases[i + 1] + ": " +
                PersonaInfeccion.evolucionesCarambola[i] + "\n";
        }

        panel.SetActive(true);

        skipRequested = false;
        continueButton.gameObject.SetActive(true);

        if (buttonText != null)
            buttonText.text = "Omitir";

        StartCoroutine(AnimateResults());
    }

    IEnumerator AnimateResults()
    {
        isAnimating = true;
        ClearTexts();

        yield return StartCoroutine(TypeText(zonaEvolutionText, fullZonaEvolution));
        yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(choqueEvolutionText, fullChoqueEvolution));
        yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(carambolaEvolutionText, fullCarambolaEvolution));
        yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(monedasPartidaText, fullMonedasPartida));
        yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(monedasTotalesText, fullMonedasTotales));

        FinishAnimation();
    }


    IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";

        if (skipRequested)
        {
            textComponent.text = fullText;
            yield break;
        }

        foreach (char c in fullText)
        {
            textComponent.text += c;

            if (skipRequested)
            {
                textComponent.text = fullText;
                yield break;
            }

            yield return new WaitForSecondsRealtime(letterSpeed);
        }
    }

    void FinishAnimation()
    {
        isAnimating = false;

        if (buttonText != null)
            buttonText.text = "Continuar";

        zonaEvolutionText.text = fullZonaEvolution;
        choqueEvolutionText.text = fullChoqueEvolution;
        carambolaEvolutionText.text = fullCarambolaEvolution;
        monedasPartidaText.text = fullMonedasPartida;
        monedasTotalesText.text = fullMonedasTotales;

    }
    public void OnClickContinue()
    {
        if (isAnimating)
        {
            skipRequested = true;
        }
        else
        {
            panel.SetActive(false);
            Time.timeScale = 1f; // Reanuda el juego
            LevelManager.instance.OnEndDayResultsFinished(0, 0);
        }
    }

    void ClearTexts()
    {
        zonaEvolutionText.text = "";
        choqueEvolutionText.text = "";
        carambolaEvolutionText.text = "";
        monedasPartidaText.text = "";
        monedasTotalesText.text = "";
    }


}
