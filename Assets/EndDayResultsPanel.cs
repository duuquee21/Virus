using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class EndDayResultsPanel : MonoBehaviour
{
    public static EndDayResultsPanel instance;

    // Cooldown para prevenir doble clic en botones
    private System.Collections.Generic.Dictionary<string, float> lastClickTimes = new System.Collections.Generic.Dictionary<string, float>();
    private const float CLICK_COOLDOWN = 0.5f; // Aumentado a 500ms para mando

    private bool CanClick(string buttonName)
    {
        if (!lastClickTimes.ContainsKey(buttonName))
        {
            lastClickTimes[buttonName] = 0f;
        }

        if (Time.time - lastClickTimes[buttonName] < CLICK_COOLDOWN)
            return false;

        lastClickTimes[buttonName] = Time.time;
        return true;
    }

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

    // Monedas base por fase que usa el panel
    private readonly int[] valorZonaPorFase = { 1, 2, 3, 4, 5 };

    // ==========================================
    // CAMBIO AQUÍ: Nombre de la nueva tabla
    // ==========================================
    private string nombreTablaLocalization = "TextosJuego";

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

    [Header("Barras de Vida de Planetas")]
    public Transform barrasContainer;
    public GameObject barraVidaPrefab;

    [Header("UI - Botones")]
    public GameObject btnContinue;   // Arrastra el botón de Continuar
    public GameObject btnArbol;      // Arrastra el botón de Árbol
    public GameObject btnClaim;      // Arrastra el NUEVO botón de Claim Coins

    private int totalCuentaFinal;    // Para saber el valor final en caso de skip

    // Partículas de la partida (se pausan mientras se muestra el panel de resumen)
    private readonly System.Collections.Generic.List<ParticleSystem> pausedParticleSystems = new System.Collections.Generic.List<ParticleSystem>();

    private System.Collections.IEnumerator SelectContinueAfterDelay()
    {
        yield return null; // Esperar un frame para que los botones estén activos
        SetSelectedButton(btnContinue);
    }

    private void SetSelectedButton(GameObject buttonObject)
    {
        if (EventSystem.current == null || buttonObject == null) return;
        var selectable = buttonObject.GetComponent<Selectable>();
        if (selectable != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonObject);
            selectable.OnSelect(new BaseEventData(EventSystem.current));
        }
    }

    [Header("Configuración Jackpot")]
    public int jackpotThreshold = 100; // Define cuánto es un "Gran Jackpot"

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    // Método para obtener el texto traducido de la tabla "TextosJuego"
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString(nombreTablaLocalization, clave);
        if (string.IsNullOrEmpty(op)) return clave; // Si no encuentra la traducción, muestra la clave
        return op;
    }

    // -------------------------
    // BONUS MONEDAS POR FASE
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
        // Detenemos y limpiamos las partículas de la partida para que desaparezcan al abrir el panel
        StopGameplayParticles();

        // Eliminamos los números voladores y textos flotantes que puedan estar activos
        var floatingScores = FindObjectsOfType<FloatingScoreUI>(true);
        foreach (var fs in floatingScores)
        {
            if (fs != null)
                Destroy(fs.gameObject);
        }

        var floatingTexts = FindObjectsOfType<FloatingText>(true);
        foreach (var ft in floatingTexts)
        {
            if (ft != null)
                ft.gameObject.SetActive(false);
        }

        Time.timeScale = 0f;
        panel.SetActive(true);

        // NUEVO: Ocultar navegación y mostrar solo Claim
        btnClaim.SetActive(true);
        btnContinue.SetActive(false);
        btnArbol.SetActive(false);

        // Set selected button for controller navigation
        SetSelectedButton(btnClaim);

        totalCuentaFinal = monedasTotales; // Guardamos el total para el skip

        UpdateAllTexts(monedasGanadas, monedasTotales);
        GenerarBarraPlanetaActual();
    }
    public void OnClickClaim()
    {
        if (!CanClick("Claim")) return;

        if (btnClaim != null) btnClaim.SetActive(false); // Desaparece al pulsar
        if (btnContinue != null) btnContinue.SetActive(true);
        if (btnArbol != null) btnArbol.SetActive(true);

        // Seleccionar automáticamente Continue después de un pequeño delay para evitar doble activación
        StartCoroutine(SelectContinueAfterDelay());

        StartCoinTransfer(() =>
        {
            // Al terminar la animación, se muestran los otros dos
        });
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

        // ===================== ZONA Y DAÑO =====================
        int totalZ = 0;
        float totalDanioZonaCalculado = 0f;

        string tituloZona = $"<b>{GetTexto("titulo_ev_zona")}</b>\n\n";
        string zonaCoinsLines = "";
        string zonaDamageLines = "";

        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            // 1. MONEDAS
            int cantEvoluciones = PersonaInfeccion.evolucionesEntreFases[i];
            int valBase = valorZonaPorFase[i];
            int coinBonus = GetCoinBonusForPhase(i);
            int valFinal = valBase + coinBonus;

            totalZ += cantEvoluciones * valFinal;

            string coinTxt = (coinBonus != 0)
                ? $"Valor: {valFinal} <color=#55FF55>(+{coinBonus})</color>  |  Total: {cantEvoluciones * valFinal}"
                : $"Valor: {valFinal}  |  Total: {cantEvoluciones * valFinal}";

            zonaCoinsLines += $"{GetTexto(clavesFases[i])}: {cantEvoluciones}  |  {coinTxt}\n";

            // 2. DAÑO CALCULADO DIRECTAMENTE
            int cantGolpes = PersonaInfeccion.golpesAlPlanetaPorFase[i];

            float hitBase = GetBaseDamageForPhase(i);
            int hitBonus = GetDamageBonusForPhase(i);
            float hitFinal = GetDamagePerHitForPhase(i);

            float totalDmg = cantGolpes * hitFinal;
            totalDanioZonaCalculado += totalDmg;

            string hitTxt = (hitBonus != 0)
                ? $"Hit: {hitFinal:F0} <color=#55FF55>(+{hitBonus})</color>"
                : $"Hit: {hitFinal:F0}";

            zonaDamageLines += $"{GetTexto(clavesFases[i])}: {cantGolpes}  |  {hitTxt}  |  Total: {totalDmg:F0}\n";
        }

        // Si quieres, además sincronizas las estadísticas globales con lo calculado
        PersonaInfeccion.dañoTotalZona = totalDanioZonaCalculado;

        for (int i = 0; i < PersonaInfeccion.golpesAlPlanetaPorFase.Length; i++)
        {
            float hitFinal = GetDamagePerHitForPhase(i);
            PersonaInfeccion.dañoZonaPorFase[i] = PersonaInfeccion.golpesAlPlanetaPorFase[i] * hitFinal;
        }

        zonaEvolutionText.text = tituloZona;
        if (zonaCoinsDetailText != null) zonaCoinsDetailText.text = zonaCoinsLines;
        if (zonaDamageDetailText != null) zonaDamageDetailText.text = zonaDamageLines;

        zonaMonedasText.text = $"<b>{GetTexto("txt_total_zona")} \n{totalZ}</b>";
        zonaDamageText.text = $"{GetTexto("txt_dano_total")} \n{totalDanioZonaCalculado:F0}";
        // ===================== CHOQUE =====================
        int totalP = 0;
        string tituloChoque = $"<b>{GetTexto("titulo_ev_pared")}</b>\n\n";
        string choqueCoinsLines = "";

        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesPorChoque[i];
            int valBase = valorZonaPorFase[i];
            int coinBonus = GetCoinBonusForPhase(i);
            int valFinal = valBase + coinBonus;

            totalP += cant * valFinal;

            string coinTxt = (coinBonus != 0)
                ? $"Valor: {valFinal} <color=#55FF55>(+{coinBonus})</color>  |  Total: {cant * valFinal}"
                : $"Valor: {valFinal}  |  Total: {cant * valFinal}";

            choqueCoinsLines += $"{GetTexto(clavesFases[i])}: {cant}  |  {coinTxt}\n";
        }

        choqueEvolutionText.text = tituloChoque;
        if (choqueCoinsDetailText != null) choqueCoinsDetailText.text = choqueCoinsLines;
        choqueMonedasText.text = $"<b>{GetTexto("txt_total_pared")} \n{totalP}</b>";

        // ===================== CARAMBOLA =====================
        int totalC = 0;
        string tituloCarambola = $"<b>{GetTexto("titulo_ev_carambola")}</b>\n\n";
        string carambolaCoinsLines = "";

        for (int i = 0; i < PersonaInfeccion.evolucionesCarambola.Length; i++)
        {
            int cant = PersonaInfeccion.evolucionesCarambola[i];
            int valBase = valorZonaPorFase[i];
            int coinBonus = GetCoinBonusForPhase(i);
            int valFinal = valBase + coinBonus;

            totalC += cant * valFinal;

            string coinTxt = (coinBonus != 0)
                ? $"Valor: {valFinal} <color=#55FF55>(+{coinBonus})</color>  |  Total: {cant * valFinal}"
                : $"Valor: {valFinal}  |  Total: {cant * valFinal}";

            carambolaCoinsLines += $"{GetTexto(clavesFases[i])}: {cant}  |  {coinTxt}\n";
        }

        carambolaEvolutionText.text = tituloCarambola;
        if (carambolaCoinsDetailText != null) carambolaCoinsDetailText.text = carambolaCoinsLines;
        carambolaMonedasText.text = $"<b>{GetTexto("txt_total_carambola")} \n{totalC}</b>";

        // ===================== RESUMEN GENERAL =====================
        monedasTempPartida = monedasGanadas;
        monedasTempTotales = monedasTotales - monedasGanadas;

        monedasPartidaEtiqueta.text = $"<b>{GetTexto("titulo_monedas_ganadas")}:</b>";
        monedasTotalesEtiqueta.text = $"<b>{GetTexto("titulo_monedas_totales")}:</b>";

        ActualizarTextosMonedas();

        if (btnContinue != null)
        {
            TextMeshProUGUI textoContinue = btnContinue.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textoContinue != null) textoContinue.text = GetTexto("btn_continuar");
        }

        if (btnClaim != null)
        {
            TextMeshProUGUI textoClaim = btnClaim.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textoClaim != null) textoClaim.text = GetTexto("btn_claim");
        }

        if (btnArbol != null)
        {
            TextMeshProUGUI textoArbol = btnArbol.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textoArbol != null) textoArbol.text = GetTexto("btn_arbol");
        }
    }

    public void OnClickContinue()
    {
        if (LevelManager.instance == null) return;
        if (LevelManager.instance.IsSoftRestarting) return;

        FinalizarConSkip();
        LevelManager.instance.SoftRestartRun();
    }

    public void OnClickArbol()
    {
        if (!CanClick("Arbol")) return;

        FinalizarConSkip();
        LevelManager.instance.OpenSkillTreePanel();
    }

    private void FinalizarConSkip()
    {
        if (isTransferring)
        {
            StopAllCoroutines();
            isTransferring = false;
        }

        // Forzamos valores finales de lógica
        monedasTempPartida = 0;
        monedasTempTotales = totalCuentaFinal;
        ActualizarTextosMonedas();

        // --- CORRECCIÓN AQUÍ: Resetear escala y color visualmente ---
        if (monedasTotalesText != null)
        {
            monedasTotalesText.transform.localScale = Vector3.one; // Forzar escala 1,1,1
            monedasTotalesText.color = colorNormal;
        }

        Time.timeScale = 1f;
        ResumeGameplayParticles();
    }

    private void ActualizarTextosMonedas()
    {
        monedasPartidaText.text = monedasTempPartida.ToString();
        monedasTotalesText.text = monedasTempTotales.ToString();
    }

    private void StopGameplayParticles()
    {
        pausedParticleSystems.Clear();
        var allParticles = FindObjectsOfType<ParticleSystem>(true);
        foreach (var ps in allParticles)
        {
            if (ps == null) continue;
            if (!ps.isPlaying) continue;
            if (ps.transform.IsChildOf(panel.transform)) continue; // No apagamos las partículas del panel de resultados

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pausedParticleSystems.Add(ps);
        }
    }

    private void ResumeGameplayParticles()
    {
        for (int i = 0; i < pausedParticleSystems.Count; i++)
        {
            var ps = pausedParticleSystems[i];
            if (ps != null) ps.Play(true);
        }
        pausedParticleSystems.Clear();
    }

    void GenerarBarraPlanetaActual()
    {
        if (barrasContainer == null || barraVidaPrefab == null) return;
        if (MapSequenceManager.instance == null) return;

        foreach (Transform child in barrasContainer)
            Destroy(child.gameObject);

        var maps = MapSequenceManager.instance.maps;
        if (maps == null || maps.Count == 0) return;

        int current = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        current = Mathf.Clamp(current, 0, maps.Count - 1);

        GameObject barra = Instantiate(barraVidaPrefab, barrasContainer);

        RectTransform barraRect = barra.GetComponent<RectTransform>();
        if (barraRect != null)
        {
            RectTransform prefabRect = barraVidaPrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                barraRect.sizeDelta = prefabRect.sizeDelta;
                barraRect.anchoredPosition = prefabRect.anchoredPosition;
                barraRect.localScale = prefabRect.localScale;
            }
        }

        var barraUI = barra.GetComponent<PlanetHealthBarUI>();
        if (barraUI == null) return;

        float porcentaje = 1f;
        if (maps[current].maxHealth > 0f)
            porcentaje = maps[current].currentHealth / maps[current].maxHealth;

        porcentaje = Mathf.Clamp01(porcentaje);

        barraUI.Setup(maps[current].mapName, porcentaje);
    }

    public void StartCoinTransfer(System.Action onComplete)
    {
        if (isTransferring) return;
        StartCoroutine(TransferRoutine(onComplete));
    }

    private IEnumerator TransferRoutine(System.Action onComplete)
    {
        isTransferring = true;
        monedasTotalesText.transform.localScale = Vector3.one;
        Vector3 escalaOriginal = Vector3.one;



        int totalAMover = monedasTempPartida;
        int inicialPartida = monedasTempPartida;
        int inicialTotales = monedasTempTotales;

        // Comprobamos si esta partida merece el color dorado
        bool esGrandJackpot = totalAMover >= jackpotThreshold;

        float duration = Mathf.Clamp(totalAMover * 0.01f, 0.8f, 3.5f);
        float elapsed = 0f;

        float soundInterval = 0.08f;
        float lastSoundTime = -1f;
        int notasTocadas = 0;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float curvedT = t * (2 - t);
            int actualMovido = Mathf.RoundToInt(curvedT * totalAMover);

            if (elapsed - lastSoundTime >= soundInterval && actualMovido > (inicialPartida - monedasTempPartida))
            {
                if (audioSource != null && tickSound != null)
                {
                    float step = notasTocadas % 12;
                    audioSource.pitch = Mathf.Pow(1.059f, step) * 0.9f;
                    float dynamicVolume = soundVolume * (0.8f + (t * 0.2f));
                    audioSource.PlayOneShot(tickSound, dynamicVolume);

                    lastSoundTime = elapsed;
                    notasTocadas++;
                    soundInterval = Mathf.Max(0.04f, soundInterval * 0.98f);
                }

                if (coinParticles != null)
                {
                    int burst = totalAMover > 50 ? 3 : 1;
                    coinParticles.Emit(burst);
                }
            }

            monedasTempPartida = inicialPartida - actualMovido;
            monedasTempTotales = inicialTotales + actualMovido;
            ActualizarTextosMonedas();

            float pulse = Mathf.Sin(t * Mathf.PI) * (maxScale - 1.0f);
            monedasTotalesText.transform.localScale = escalaOriginal + new Vector3(pulse, pulse, pulse);

            // --- CAMBIO AQUÍ: Color condicional ---
            if (esGrandJackpot)
            {
                monedasTotalesText.color = Color.Lerp(colorNormal, colorPremio, t);
            }
            else
            {
                monedasTotalesText.color = colorNormal;
            }

            yield return null;
        }

        // --- CIERRE FINAL ---
        if (audioSource != null && tickSound != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(tickSound, soundVolume * 1.2f);
        }

        monedasTempPartida = 0;
        monedasTempTotales = inicialTotales + totalAMover;
        ActualizarTextosMonedas();
        monedasTotalesText.transform.localScale = escalaOriginal;

        monedasTotalesText.transform.localScale = Vector3.one;
        monedasTotalesText.color = colorNormal;

        isTransferring = false;
        onComplete?.Invoke();
    }
}