using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class EndDayResultsPanel : MonoBehaviour
{
    public static EndDayResultsPanel instance;

    [Header("UI")]
    public GameObject panel;

    [Header("Cálculos de Evolución (Lista)")]
    public TextMeshProUGUI zonaEvolutionText;
    public TextMeshProUGUI choqueEvolutionText;
    public TextMeshProUGUI carambolaEvolutionText;

    [Header("Monedas por Habilidad (Totales)")]
    public TextMeshProUGUI zonaMonedasText;
    public TextMeshProUGUI choqueMonedasText;
    public TextMeshProUGUI carambolaMonedasText;

    [Header("Resumen General")]
    public TextMeshProUGUI monedasPartidaText;
    public TextMeshProUGUI monedasTotalesText;

    [Header("Resumen General (Nuevas Referencias)")]
    public TextMeshProUGUI monedasPartidaEtiqueta; // El texto "Monedas Ganadas:"
    public TextMeshProUGUI monedasTotalesEtiqueta;  // El texto "Total Monedas:"
                                                    // Nota: monedasPartidaText y monedasTotalesText ahora serán SOLO para los números.

    [Header("Button Config")]
    public Button continueButton;
    public TextMeshProUGUI buttonText;

    private readonly string[] clavesFases = { "fase_hex", "fase_pent", "fase_cuad", "fase_tri", "fase_circ", "fase_bola" };
    private readonly int[] valorZonaPorFase = { 1, 2, 3, 4, 5 };
    private string nombreTablaLocalization = "MisTextos";


    [Header("Animación")]
    private bool isTransferring = false;
    private int monedasTempPartida;
    private int monedasTempTotales;

    public bool TieneMonedasPendientes => monedasTempPartida > 0;

    public Image flashImage; // Una imagen blanca (Sprite: Square o Glow) detrás del texto

    public Color flashColor = Color.white; // Esto aparecerá en el Inspector para que elijas el color
    private Color colorOriginal; // Variable para recordar cómo era la imagen antes de empezar

    void Awake()
    {
        instance = this;
        panel.SetActive(false);

        if (flashImage != null)
        {
            // Guardamos el color exacto que configuraste en el editor (RGB y Alfa)
            colorOriginal = flashImage.color;
        }

    }

    string GetTexto(string clave) => LocalizationSettings.StringDatabase.GetLocalizedString(nombreTablaLocalization, clave);

    public void ShowResults(int monedasGanadas, int monedasTotales)
    {
        // Pausar el juego
        Time.timeScale = 0f;
        string txtMonedas = GetTexto("monedas");

       
        // 1. PROCESAR ZONA
        int totalZ = 0;
        string evZona = $"<b>{GetTexto("titulo_ev_zona")}</b>\n\n";
        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesEntreFases[i];
            int val = valorZonaPorFase[i];
            totalZ += (cant * val);
            evZona += $"{GetTexto(clavesFases[i])}: {cant} ({val}×{cant}={cant * val})\n";
        }
        zonaEvolutionText.text = evZona;
        zonaMonedasText.text = $"<b>{GetTexto("txt_total_zona")} {totalZ} {txtMonedas}</b>";

        // 2. PROCESAR PARED/CHOQUE
        int totalP = 0;
        string evChoque = $"<b>{GetTexto("titulo_ev_pared")}</b>\n\n";
        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesPorChoque[i];
            int val = valorZonaPorFase[i];
            totalP += (cant * val);
            evChoque += $"{GetTexto(clavesFases[i])}: {cant} ({val}×{cant}={cant * val})\n";
        }
        choqueEvolutionText.text = evChoque;
        choqueMonedasText.text = $"<b>{GetTexto("txt_total_pared")} {totalP} {txtMonedas}</b>";

        // 3. PROCESAR CARAMBOLA
        int totalC = 0;
        string evCarambola = $"<b>{GetTexto("titulo_ev_carambola")}</b>\n\n";
        for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesCarambola[i];
            int val = valorZonaPorFase[i];
            totalC += (cant * val);
            evCarambola += $"{GetTexto(clavesFases[i])}: {cant} ({val}×{cant}={cant * val})\n";
        }
        carambolaEvolutionText.text = evCarambola;
        carambolaMonedasText.text = $"<b>{GetTexto("txt_total_carambola")} {totalC} {txtMonedas}</b>";

        monedasTempPartida = monedasGanadas;
        monedasTempTotales = monedasTotales - monedasGanadas;

        // 4. TOTALES GENERALES
        monedasPartidaEtiqueta.text = $"<b>{GetTexto("titulo_monedas_ganadas")}:</b>";
        monedasTotalesEtiqueta.text = $"<b>{GetTexto("titulo_monedas_totales")}:</b>";

        monedasTempPartida = monedasGanadas;
        monedasTempTotales = monedasTotales - monedasGanadas;

        ActualizarTextosMonedas();
        panel.SetActive(true);
        if (buttonText != null) buttonText.text = "Continuar (F2)";
    }

    public void OnClickContinue()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
        LevelManager.instance.OnEndDayResultsFinished(0, 0);
    }

    private void ActualizarTextosMonedas()
    {
        monedasPartidaText.text = monedasTempPartida.ToString();
        monedasTotalesText.text = monedasTempTotales.ToString();
    }

    public void StartCoinTransfer(System.Action onComplete)
    {
        if (isTransferring) return;
        StartCoroutine(TransferRoutine(onComplete));
    }

    private IEnumerator TransferRoutine(System.Action onComplete)
    {
        isTransferring = true;

        while (monedasTempPartida > 0)
        {
            // ... (Lógica de steps y monedas igual)
            int step = 1;
            if (monedasTempPartida > 500) step = 13;
            else if (monedasTempPartida > 100) step = 5;
            if (step > monedasTempPartida) step = monedasTempPartida;

            monedasTempPartida -= step;
            monedasTempTotales += step;
            ActualizarTextosMonedas();

            // --- EFECTO FLASH ---
            if (flashImage != null)
            {
                // 1. FLASH: Subimos el alfa al máximo (o al valor de flashColor)
                // Manteniendo los colores RGB originales
                flashImage.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, flashColor.a);
            }

            float delay = Mathf.Lerp(0.08f, 0.01f, (float)monedasTempPartida / 100f);

            // El pico del destello
            yield return new WaitForSecondsRealtime(0.02f);

            if (flashImage != null)
            {
                // 2. RETORNO: Volvemos EXACTAMENTE al color y alfa que tenía en el inspector
                flashImage.color = colorOriginal;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0, delay - 0.02f));
        }

        isTransferring = false;
        onComplete?.Invoke();
    }


}