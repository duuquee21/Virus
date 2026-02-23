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

    [Header("Cálculos de Evolución (Título / Lista principal)")]
    public TextMeshProUGUI zonaEvolutionText;
    public TextMeshProUGUI choqueEvolutionText;
    public TextMeshProUGUI carambolaEvolutionText;

    [Header("Detalle Monedas por Fase (NUEVO)")]
    public TextMeshProUGUI zonaCoinsDetailText;
    public TextMeshProUGUI choqueCoinsDetailText;
    public TextMeshProUGUI carambolaCoinsDetailText;

    [Header("Detalle Daño por Fase (NUEVO)")]
    public TextMeshProUGUI zonaDamageDetailText;
    public TextMeshProUGUI choqueDamageDetailText;
    public TextMeshProUGUI carambolaDamageDetailText;

    [Header("Monedas por Habilidad (Totales)")]
    public TextMeshProUGUI zonaMonedasText;
    public TextMeshProUGUI choqueMonedasText;
    public TextMeshProUGUI carambolaMonedasText;

    [Header("Resumen General")]
    public TextMeshProUGUI monedasPartidaText;
    public TextMeshProUGUI monedasTotalesText;

    [Header("Resumen General (Nuevas Referencias)")]
    public TextMeshProUGUI monedasPartidaEtiqueta;
    public TextMeshProUGUI monedasTotalesEtiqueta;

    [Header("Daño Total")]
    public TextMeshProUGUI zonaDamageText;
    public TextMeshProUGUI choqueDamageText;
    public TextMeshProUGUI carambolaDamageText;

    // Orden real: 0 HEX, 1 PENT, 2 CUAD, 3 TRI, 4 CIRC
    private readonly string[] clavesFases = { "fase_hex", "fase_pent", "fase_cuad", "fase_tri", "fase_circ", "fase_bola" };

    // Monedas base por fase que usa el panel (si tu juego usa otra tabla, cámbiala aquí)
    private readonly int[] valorZonaPorFase = { 1, 2, 3, 4, 5 };

    private string nombreTablaLocalization = "MisTextos";

    [Header("Animación")]
    private bool isTransferring = false;
    private int monedasTempPartida;
    private int monedasTempTotales;
    public bool TieneMonedasPendientes => monedasTempPartida > 0;

    [Header("Feedback de Audio")]
    public AudioSource audioSource;
    public AudioClip tickSound;
    [Range(0f, 1f)] public float soundVolume = 0.5f;

    [Header("Feedback Visual")]
    public float maxScale = 1.3f;
    public Color colorNormal = Color.white;
    public Color colorPremio = Color.yellow;

    [Header("Feedback de Partículas")]
    public ParticleSystem coinParticles;
    public int maxParticlesPerFlash = 20;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    string GetTexto(string clave)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(nombreTablaLocalization, clave);
    }

    // -------------------------
    // BONUS MONEDAS POR FASE
    // Orden real: 0 HEX, 1 PENT, 2 CUAD, 3 TRI, 4 CIRC
    // -------------------------
    private int GetCoinBonusForPhase(int fase)
    {
        if (Guardado.instance == null) return 0;

        switch (fase)
        {
            case 0: return Guardado.instance.coinsExtraHexagono;
            case 1: return Guardado.instance.coinsExtraPentagono;
            case 2: return Guardado.instance.coinsExtraCuadrado;
            case 3: return Guardado.instance.coinsExtraTriangulo;
            case 4: return Guardado.instance.coinsExtraCirculo;
            default: return 0;
        }
    }

    // -------------------------
    // DAÑO POR HIT (BASE + BONUS) POR FASE
    // Base igual que PersonaInfeccion.dañoPorFasePredeterminado = {1,2,3,4,5}
    // -------------------------
    private float GetBaseDamageForPhase(int fase)
    {
        switch (fase)
        {
            case 0: return 1f; // HEX
            case 1: return 2f; // PENT
            case 2: return 3f; // CUAD
            case 3: return 4f; // TRI
            case 4: return 5f; // CIRC
            default: return 0f;
        }
    }

    private int GetDamageBonusForPhase(int fase)
    {
        if (Guardado.instance == null) return 0;

        switch (fase)
        {
            case 0: return Guardado.instance.dañoExtraHexagono;
            case 1: return Guardado.instance.dañoExtraPentagono;
            case 2: return Guardado.instance.dañoExtraCuadrado;
            case 3: return Guardado.instance.dañoExtraTriangulo;
            case 4: return Guardado.instance.dañoExtraCirculo;
            default: return 0;
        }
    }

    private float GetDamagePerHitForPhase(int fase)
    {
        return GetBaseDamageForPhase(fase) + GetDamageBonusForPhase(fase);
    }

    // ======================================================
    // MÉTODO PRINCIPAL (SE LLAMA AL TERMINAR EL DÍA)
    // ======================================================
    public void ShowResults(int monedasGanadas, int monedasTotales)
    {
        Time.timeScale = 0f;
        panel.SetActive(true);
        UpdateAllTexts(monedasGanadas, monedasTotales);
    }

    // ======================================================
    // MÉTODO PARA ACTUALIZAR EN VIVO
    // ======================================================
    public void RefreshResults()
    {
        if (!panel.activeSelf) return;

        int monedasTotales = LevelManager.instance.ContagionCoins;
        int monedasGanadas = LevelManager.instance.monedasGanadasSesion;

        UpdateAllTexts(monedasGanadas, monedasTotales);
    }

    // ======================================================
    // LÓGICA CENTRALIZADA DE ACTUALIZACIÓN
    // ======================================================
    private void UpdateAllTexts(int monedasGanadas, int monedasTotales)
    {
        string txtMonedas = GetTexto("monedas");

        // ===================== ZONA =====================
        int totalZ = 0;
        string tituloZona = $"<b>{GetTexto("titulo_ev_zona")}</b>\n\n";
        string zonaCoinsLines = "";
        string zonaDamageLines = "";

        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesEntreFases[i];

            int valBase = valorZonaPorFase[i];
            int coinBonus = GetCoinBonusForPhase(i);
            int valFinal = valBase + coinBonus;

            totalZ += cant * valFinal;

            string coinTxt = (coinBonus != 0)
                ? $"(({valBase}+{coinBonus})×{cant}={cant * valFinal})"
                : $"({valBase}×{cant}={cant * valFinal})";

            zonaCoinsLines += $"{GetTexto(clavesFases[i])}: {cant} {coinTxt}\n";

            float totalDmg = (i < PersonaInfeccion.dañoZonaPorFase.Length) ? PersonaInfeccion.dañoZonaPorFase[i] : 0f;

            float hitBase = GetBaseDamageForPhase(i);
            int hitBonus = GetDamageBonusForPhase(i);
            float hitFinal = GetDamagePerHitForPhase(i);

            string hitTxt = (hitBonus != 0)
                ? $"Hit: ({hitBase:F0}+{hitBonus}={hitFinal:F0})"
                : $"Hit: {hitFinal:F0}";

            zonaDamageLines += $"{GetTexto(clavesFases[i])}: {cant}  |  {hitTxt}  |  Total: {totalDmg:F0}\n";
        }

        zonaEvolutionText.text = tituloZona;
        if (zonaCoinsDetailText != null) zonaCoinsDetailText.text = zonaCoinsLines;
        if (zonaDamageDetailText != null) zonaDamageDetailText.text = zonaDamageLines;

        zonaMonedasText.text = $"<b>{GetTexto("txt_total_zona")} {totalZ} {txtMonedas}</b>";
        zonaDamageText.text = $"Daño total: {PersonaInfeccion.dañoTotalZona:F0}";


        // ===================== CHOQUE =====================
        int totalP = 0;
        string tituloChoque = $"<b>{GetTexto("titulo_ev_pared")}</b>\n\n";
        string choqueCoinsLines = "";
        string choqueDamageLines = "";

        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesPorChoque[i];

            int valBase = valorZonaPorFase[i];
            int coinBonus = GetCoinBonusForPhase(i);
            int valFinal = valBase + coinBonus;

            totalP += cant * valFinal;

            string coinTxt = (coinBonus != 0)
                ? $"(({valBase}+{coinBonus})×{cant}={cant * valFinal})"
                : $"({valBase}×{cant}={cant * valFinal})";

            choqueCoinsLines += $"{GetTexto(clavesFases[i])}: {cant} {coinTxt}\n";

            float totalDmg = (i < PersonaInfeccion.dañoChoquePorFase.Length) ? PersonaInfeccion.dañoChoquePorFase[i] : 0f;

            float hitBase = GetBaseDamageForPhase(i);
            int hitBonus = GetDamageBonusForPhase(i);
            float hitFinal = GetDamagePerHitForPhase(i);

            string hitTxt = (hitBonus != 0)
                ? $"Hit: ({hitBase:F0}+{hitBonus}={hitFinal:F0})"
                : $"Hit: {hitFinal:F0}";

            choqueDamageLines += $"{GetTexto(clavesFases[i])}: {cant}  |  {hitTxt}  |  Total: {totalDmg:F0}\n";
        }

        choqueEvolutionText.text = tituloChoque;
        if (choqueCoinsDetailText != null) choqueCoinsDetailText.text = choqueCoinsLines;
        if (choqueDamageDetailText != null) choqueDamageDetailText.text = choqueDamageLines;

        choqueMonedasText.text = $"<b>{GetTexto("txt_total_pared")} {totalP} {txtMonedas}</b>";
        choqueDamageText.text = $"Daño total: {PersonaInfeccion.dañoTotalChoque:F0}";


        // ===================== CARAMBOLA =====================
        int totalC = 0;
        string tituloCarambola = $"<b>{GetTexto("titulo_ev_carambola")}</b>\n\n";
        string carambolaCoinsLines = "";
        string carambolaDamageLines = "";

        for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesCarambola[i];

            int valBase = valorZonaPorFase[i];
            int coinBonus = GetCoinBonusForPhase(i);
            int valFinal = valBase + coinBonus;

            totalC += cant * valFinal;

            string coinTxt = (coinBonus != 0)
                ? $"(({valBase}+{coinBonus})×{cant}={cant * valFinal})"
                : $"({valBase}×{cant}={cant * valFinal})";

            carambolaCoinsLines += $"{GetTexto(clavesFases[i])}: {cant} {coinTxt}\n";

            float totalDmg = (i < PersonaInfeccion.dañoCarambolaPorFase.Length) ? PersonaInfeccion.dañoCarambolaPorFase[i] : 0f;

            float hitBase = GetBaseDamageForPhase(i);
            int hitBonus = GetDamageBonusForPhase(i);
            float hitFinal = GetDamagePerHitForPhase(i);

            string hitTxt = (hitBonus != 0)
                ? $"Hit: ({hitBase:F0}+{hitBonus}={hitFinal:F0})"
                : $"Hit: {hitFinal:F0}";

            carambolaDamageLines += $"{GetTexto(clavesFases[i])}: {cant}  |  {hitTxt}  |  Total: {totalDmg:F0}\n";
        }

        carambolaEvolutionText.text = tituloCarambola;
        if (carambolaCoinsDetailText != null) carambolaCoinsDetailText.text = carambolaCoinsLines;
        if (carambolaDamageDetailText != null) carambolaDamageDetailText.text = carambolaDamageLines;

        carambolaMonedasText.text = $"<b>{GetTexto("txt_total_carambola")} {totalC} {txtMonedas}</b>";
        carambolaDamageText.text = $"Daño total: {PersonaInfeccion.dañoTotalCarambola:F0}";


        // ===================== RESUMEN GENERAL =====================
        monedasTempPartida = monedasGanadas;
        monedasTempTotales = monedasTotales - monedasGanadas;

        monedasPartidaEtiqueta.text = $"<b>{GetTexto("titulo_monedas_ganadas")}:</b>";
        monedasTotalesEtiqueta.text = $"<b>{GetTexto("titulo_monedas_totales")}:</b>";

        ActualizarTextosMonedas();
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
        Vector3 escalaOriginal = monedasTotalesText.transform.localScale;

        int totalAMover = monedasTempPartida;
        int inicialPartida = monedasTempPartida;
        int inicialTotales = monedasTempTotales;

        float elapsed = 0f;
        float duration = Mathf.Clamp(totalAMover * 0.00005f + 1.0f, 1.2f, 5.0f);
        float lastSoundTime = 0f;

        float factorRiqueza = Mathf.Clamp01(totalAMover / 500f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curvedT = t * t * t * (t * (6f * t - 15f) + 10f);

            int movido = Mathf.RoundToInt(curvedT * totalAMover);
            movido = Mathf.Min(movido, totalAMover);

            monedasTempPartida = inicialPartida - movido;
            monedasTempTotales = inicialTotales + movido;
            ActualizarTextosMonedas();

            float intensity = Mathf.Sin(t * Mathf.PI);
            float pulse = intensity * (maxScale - 1.0f) * (0.5f + factorRiqueza * 0.5f);
            monedasTotalesText.transform.localScale = escalaOriginal + new Vector3(pulse, pulse, pulse);
            monedasTotalesText.color = Color.Lerp(colorNormal, colorPremio, intensity * factorRiqueza);

            float minInterval = Mathf.Lerp(0.2f, 0.05f, factorRiqueza);
            float maxInterval = Mathf.Lerp(0.4f, 0.15f, factorRiqueza);
            float currentTickSpeed = Mathf.Lerp(maxInterval, minInterval, intensity);

            if (elapsed - lastSoundTime > currentTickSpeed)
            {
                if (audioSource != null && tickSound != null)
                {
                    audioSource.pitch = 0.8f + (factorRiqueza * 0.4f) + (intensity * (0.4f + factorRiqueza * 0.4f));
                    audioSource.PlayOneShot(tickSound, soundVolume);
                }

                if (coinParticles != null)
                {
                    int count = Mathf.Max(1, Mathf.RoundToInt(maxParticlesPerFlash * intensity * factorRiqueza));
                    coinParticles.Emit(count);
                }

                lastSoundTime = elapsed;
            }

            yield return null;
        }

        monedasTempPartida = 0;
        monedasTempTotales = inicialTotales + totalAMover;
        ActualizarTextosMonedas();
        monedasTotalesText.transform.localScale = escalaOriginal;
        monedasTotalesText.color = colorNormal;

        isTransferring = false;
        onComplete?.Invoke();
    }
}