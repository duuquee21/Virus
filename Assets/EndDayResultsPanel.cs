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


    // Orden real: 0 HEX, 1 PENT, 2 CUAD, 3 TRI, 4 CIRC
    private readonly string[] clavesFases = { "fase_hex", "fase_pent", "fase_cuad", "fase_tri", "fase_circ", "fase_bola" };

    // Monedas base por fase que usa el panel (si tu juego usa otra tabla, cámbiala aquí)
    private readonly int[] valorZonaPorFase = { 1, 2, 3, 4, 5 };

    // CAMBIO 1: Apuntamos a la nueva tabla
    private string nombreTablaLocalization = "TextosUI";

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

    // CAMBIO 2: Mejoramos la función para que no falle si falta una clave
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTablaLocalization, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si no encuentra la traducción, muestra la clave
        return op;
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

        choqueMonedasText.text = $"<b>{GetTexto("txt_total_pared")} {totalP} {txtMonedas}</b>";


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


        carambolaMonedasText.text = $"<b>{GetTexto("txt_total_carambola")} {totalC} {txtMonedas}</b>";



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

        // Duración basada en la cantidad, pero con límites sanos
        float duration = Mathf.Clamp(totalAMover * 0.01f, 0.8f, 3.5f);
        float elapsed = 0f;

        // CONTROL MUSICAL
        float soundInterval = 0.08f; // Tiempo mínimo entre sonidos (ritmo)
        float lastSoundTime = -1f;
        int notasTocadas = 0;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Curva de progresión
            float curvedT = t * (2 - t);
            int actualMovido = Mathf.RoundToInt(curvedT * totalAMover);

            // --- LÓGICA DE SATISFACCIÓN MUSICAL ---
            // Solo suena si ha pasado el intervalo y si realmente hay monedas nuevas que mostrar
            if (elapsed - lastSoundTime >= soundInterval && actualMovido > (inicialPartida - monedasTempPartida))
            {
                if (audioSource != null && tickSound != null)
                {
                    // Pitch musical: Sube en semitonos (1.059 es la raíz doceava de 2)
                    // Esto hace que suene como una escala musical real en lugar de un motor acelerando
                    float step = notasTocadas % 12; // Reinicia la octava cada 12 notas
                    audioSource.pitch = Mathf.Pow(1.059f, step) * 0.9f;

                    // Un pequeño toque de variación de volumen según el progreso
                    float dynamicVolume = soundVolume * (0.8f + (t * 0.2f));
                    audioSource.PlayOneShot(tickSound, dynamicVolume);

                    lastSoundTime = elapsed;
                    notasTocadas++;

                    // Aceleramos el ritmo sutilmente a medida que avanza
                    soundInterval = Mathf.Max(0.04f, soundInterval * 0.98f);
                }

                // Partículas sincronizadas con el pulso musical
                if (coinParticles != null)
                {
                    // Emitimos un "chorro" dependiendo de la cantidad total
                    int burst = totalAMover > 50 ? 3 : 1;
                    coinParticles.Emit(burst);
                }
            }

            // Actualización visual de textos
            monedasTempPartida = inicialPartida - actualMovido;
            monedasTempTotales = inicialTotales + actualMovido;
            ActualizarTextosMonedas();

            // Feedback visual en el texto (pulso rítmico)
            float pulse = Mathf.Sin(t * Mathf.PI) * (maxScale - 1.0f);
            monedasTotalesText.transform.localScale = escalaOriginal + new Vector3(pulse, pulse, pulse);
            monedasTotalesText.color = Color.Lerp(colorNormal, colorPremio, t);

            yield return null;
        }

        // --- CIERRE FINAL ---
        // Sonido final de confirmación (un poco más grave y fuerte)
        if (audioSource != null && tickSound != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(tickSound, soundVolume * 1.2f);
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