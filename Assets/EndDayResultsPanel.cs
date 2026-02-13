using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings; // <--- IMPORTANTE: Necesario para traducir por código

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


    // AHORA ESTO SON LAS CLAVES DE LA TABLA, NO LOS TEXTOS DIRECTOS
    private readonly string[] clavesFases =
    {
        "fase_hex",
        "fase_pent",
        "fase_cuad",
        "fase_tri",
        "fase_circ",
        "fase_bola"
    };

    private readonly int[] valorZonaPorFase =
    {
        1, 2, 3, 4, 5
    };

    // Nombre de tu tabla en Unity (asegúrate de que coincida con el nombre del archivo de la tabla)
    private string nombreTablaLocalization = "TextosUI";

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    // --- FUNCIÓN HELPER PARA TRADUCIR ---
    string GetTexto(string clave)
    {
        // Esto busca en la tabla el texto según el idioma actual
        return LocalizationSettings.StringDatabase.GetLocalizedString(nombreTablaLocalization, clave);
    }

    public void ShowResults(int monedasGanadas, int monedasTotales)
    {
        Time.timeScale = 0f;

        int totalZonaGeneral = 0;
        int totalParedGeneral = 0;
        int totalCarambolaGeneral = 0;

        // Recuperamos la palabra "monedas" traducida para usarla luego
        string txtMonedas = GetTexto("monedas");

        // ---- MONEDAS ----
        fullMonedasPartida = $"<b>{GetTexto("titulo_monedas_ganadas")}:</b> {monedasGanadas}";
        fullMonedasTotales = $"<b>{GetTexto("titulo_monedas_totales")}:</b> {monedasTotales}";

        // ---------------- ZONA ----------------
        fullZonaEvolution = $"<b>{GetTexto("titulo_ev_zona")}</b>\n\n";

        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            int cantidad = PersonaInfeccion.evolucionesEntreFases[i];
            int valorBase = valorZonaPorFase[i];
            int total = cantidad * valorBase;
            totalZonaGeneral += total;

            // Traducimos los nombres de las fases al vuelo
            string nombreFaseActual = GetTexto(clavesFases[i]);
            string nombreFaseSiguiente = GetTexto(clavesFases[i + 1]);

            fullZonaEvolution +=
                $"{nombreFaseActual} → {nombreFaseSiguiente}: {cantidad}  ({valorBase} × {cantidad} = {total})\n";
        }
        fullZonaEvolution += $"\n<b>{GetTexto("txt_total_zona")}: {totalZonaGeneral} {txtMonedas}</b>\n";

        // ---------------- PARED ----------------
        fullChoqueEvolution = $"\n<b>{GetTexto("titulo_ev_pared")}</b>\n\n";

        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            int cantidad = PersonaInfeccion.evolucionesPorChoque[i];
            int valorBase = valorZonaPorFase[i];
            int total = cantidad * valorBase;
            totalParedGeneral += total;

            string nombreFaseActual = GetTexto(clavesFases[i]);
            string nombreFaseSiguiente = GetTexto(clavesFases[i + 1]);

            fullChoqueEvolution +=
                $"{nombreFaseActual} → {nombreFaseSiguiente}: {cantidad}  ({valorBase} × {cantidad} = {total})\n";
        }
        fullChoqueEvolution += $"\n<b>{GetTexto("txt_total_pared")}: {totalParedGeneral} {txtMonedas}</b>\n";

        // ---------------- CARAMBOLA ----------------
        fullCarambolaEvolution = $"\n<b>{GetTexto("titulo_ev_carambola")}</b>\n\n";

        for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length; i++)
        {
            int cantidad = PersonaInfeccion.evolucionesCarambola[i];
            int valorBase = valorZonaPorFase[i];
            int total = cantidad * valorBase;
            totalCarambolaGeneral += total;

            string nombreFaseActual = GetTexto(clavesFases[i]);
            string nombreFaseSiguiente = GetTexto(clavesFases[i + 1]);

            fullCarambolaEvolution +=
                 $"{nombreFaseActual} → {nombreFaseSiguiente}: {cantidad}  ({valorBase} × {cantidad} = {total})\n";
        }
        fullCarambolaEvolution += $"\n<b>{GetTexto("txt_total_carambola")}: {totalCarambolaGeneral} {txtMonedas}</b>\n";


        panel.SetActive(true);

        skipRequested = false;
        continueButton.gameObject.SetActive(true);

        // Botón Omitir traducido
        if (buttonText != null)
            buttonText.text = GetTexto("btn_omitir");

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

        // Botón Continuar traducido
        if (buttonText != null)
            buttonText.text = GetTexto("btn_continuar");

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
            Time.timeScale = 1f;
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