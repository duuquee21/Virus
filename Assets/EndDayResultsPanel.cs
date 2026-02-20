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

    [Header("Button Config")]
    public Button continueButton;
    public TextMeshProUGUI buttonText;

    private readonly string[] clavesFases = { "fase_hex", "fase_pent", "fase_cuad", "fase_tri", "fase_circ", "fase_bola" };
    private readonly int[] valorZonaPorFase = { 1, 2, 3, 4, 5 };
    private string nombreTablaLocalization = "MisTextos";

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
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
            evZona += $"{GetTexto(clavesFases[i])} → {GetTexto(clavesFases[i + 1])}: {cant} ({val}×{cant}={cant * val})\n";
        }
        zonaEvolutionText.text = evZona;
        zonaMonedasText.text = $"<b>{GetTexto("txt_total_zona")}: {totalZ} {txtMonedas}</b>";

        // 2. PROCESAR PARED/CHOQUE
        int totalP = 0;
        string evChoque = $"<b>{GetTexto("titulo_ev_pared")}</b>\n\n";
        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesPorChoque[i];
            int val = valorZonaPorFase[i];
            totalP += (cant * val);
            evChoque += $"{GetTexto(clavesFases[i])} → {GetTexto(clavesFases[i + 1])}: {cant} ({val}×{cant}={cant * val})\n";
        }
        choqueEvolutionText.text = evChoque;
        choqueMonedasText.text = $"<b>{GetTexto("")}: {totalP} {txtMonedas}</b>";

        // 3. PROCESAR CARAMBOLA
        int totalC = 0;
        string evCarambola = $"<b>{GetTexto("titulo_ev_carambola")}</b>\n\n";
        for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesCarambola[i];
            int val = valorZonaPorFase[i];
            totalC += (cant * val);
            evCarambola += $"{GetTexto(clavesFases[i])} → {GetTexto(clavesFases[i + 1])}: {cant} ({val}×{cant}={cant * val})\n";
        }
        carambolaEvolutionText.text = evCarambola;
        carambolaMonedasText.text = $"<b>{GetTexto("txt_total_carambola")}: {totalC} {txtMonedas}</b>";

        // 4. TOTALES GENERALES
        monedasPartidaText.text = $"<b>{GetTexto("titulo_monedas_ganadas")}:</b> {monedasGanadas}";
        monedasTotalesText.text = $"<b>{GetTexto("titulo_monedas_totales")}:</b> {monedasTotales}";

        // Mostrar Panel y configurar botón
        panel.SetActive(true);
        if (buttonText != null) buttonText.text = "Continuar (F2)";
    }

    public void OnClickContinue()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
        LevelManager.instance.OnEndDayResultsFinished(0, 0);
    }
}