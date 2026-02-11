using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndDayResultsPanel : MonoBehaviour
{
    public static EndDayResultsPanel instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI infectedText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI zoneText;
    public TextMeshProUGUI finalCoinsText;

    [Header("Evolución por Fases")]
    public TextMeshProUGUI fasesEvolutionText;

    [Header("Shiny UI")]
    public TextMeshProUGUI shinyFoundText;
    public TextMeshProUGUI shinyMultiplierText;
    public TextMeshProUGUI shinyFinalText;

    [Header("Button Config")]
    public Button continueButton;
    public TextMeshProUGUI buttonText; // Arrastra el texto del botón aquí en el inspector

    [Header("Timing (Unscaled)")]
    public float letterSpeed = 0.03f;
    public float stepDelay = 0.5f;

    private int finalCoins;
    private int finalShinies;
    private bool isAnimating = false;
    private bool skipRequested = false;
    private string fullFasesEvolution;


    // Guardamos los strings para el salto instantáneo
    private string fullInfected, fullMult, fullZone, fullFinalCoins;
    private string fullShinyFound, fullShinyMult, fullShinyFinal;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    public void ShowResults(int infected, int baseMultiplier, int zoneMultiplier, int shiniesCaptured, int shinyMultiplier)
    {
        // Calculamos valores finales
        finalCoins = infected * baseMultiplier * zoneMultiplier;
        finalShinies = shiniesCaptured * shinyMultiplier;

        // Preparamos los textos
        fullInfected = "Infectados: " + infected;
        fullMult = "x " + baseMultiplier;
        fullZone = "x " + zoneMultiplier;
        fullFinalCoins = "= " + finalCoins + " monedas";

        fullShinyFound = "Shinys capturados: " + shiniesCaptured;
        fullShinyMult = "x " + shinyMultiplier;
        fullShinyFinal = "= " + finalShinies + " ADN Shiny";

        panel.SetActive(true);

        // Configuramos botón como "Omitir"
        skipRequested = false;
        continueButton.gameObject.SetActive(true);
        if (buttonText != null) buttonText.text = "Omitir";

        // Construimos texto de evoluciones entre fases
        fullFasesEvolution =
            "Evoluciones:\n" +
            "0 → 1: " + PersonaInfeccion.evolucionesEntreFases[0] + "\n" +
            "1 → 2: " + PersonaInfeccion.evolucionesEntreFases[1] + "\n" +
            "2 → 3: " + PersonaInfeccion.evolucionesEntreFases[2] + "\n" +
            "3 → 4: " + PersonaInfeccion.evolucionesEntreFases[3];


        StartCoroutine(AnimateResults());
    }

    IEnumerator AnimateResults()
    {
        isAnimating = true;
        ClearTexts();

        // Secuencia de Monedas
        yield return StartCoroutine(TypeText(infectedText, fullInfected));
        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(multiplierText, fullMult));
        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(zoneText, fullZone));
        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(finalCoinsText, fullFinalCoins));
        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay * 1.5f);

        // Secuencia de Shinies
        yield return StartCoroutine(TypeText(shinyFoundText, fullShinyFound));
        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(shinyMultiplierText, fullShinyMult));
        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(shinyFinalText, fullShinyFinal));

        if (!skipRequested) yield return new WaitForSecondsRealtime(stepDelay);

        yield return StartCoroutine(TypeText(fasesEvolutionText, fullFasesEvolution));

        // Finalizar
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
        if (buttonText != null) buttonText.text = "Continuar";

        // Aseguramos que todos los textos estén completos por si hubo skip
        infectedText.text = fullInfected;
        multiplierText.text = fullMult;
        zoneText.text = fullZone;
        finalCoinsText.text = fullFinalCoins;
        shinyFoundText.text = fullShinyFound;
        shinyMultiplierText.text = fullShinyMult;
        shinyFinalText.text = fullShinyFinal;
        fasesEvolutionText.text = fullFasesEvolution;

    }

    public void OnClickContinue()
    {
        if (isAnimating)
        {
            // Si está animando, marcamos el skip
            skipRequested = true;
        }
        else
        {
            // Si ya terminó, cerramos y volvemos al Manager
            panel.SetActive(false);
            LevelManager.instance.OnEndDayResultsFinished(finalCoins, finalShinies);
        }
    }

    void ClearTexts()
    {
        infectedText.text = multiplierText.text = zoneText.text = finalCoinsText.text = "";
        shinyFoundText.text = shinyMultiplierText.text = shinyFinalText.text = "";
        fasesEvolutionText.text = "";
    }

}