using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Selector de Modo (Menú Principal)")]
    public GameObject modeSelectionPanel;
    public Button continueButton;
    public TextMeshProUGUI continueInfoText;
    public GameObject jugadorVirus;



    [Header("Sistema de Zonas")]
    public GameObject[] mapList;

    [Header("Referencias")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript;

    [Header("Feedback Visual Puntos")]
    public GameObject prefabTextoPuntos;
    public RectTransform marcadorDestinoUI;
    public Canvas canvasPrincipal;
    [SerializeField] public int contagionCoins;
    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameUI;
   
    
    public GameObject shinyPanel; // Panel de la Tienda de ADN/Mejoras
    public GameObject zonePanel;  // Panel de Selección de Mapas
    public GameObject pausePanel;

    [Header("UI Text (Listas)")]
    public List<TextMeshProUGUI> timerTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> sessionScoreTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> contagionCoinsTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> shinyStoreTexts = new List<TextMeshProUGUI>(); // Texto para mostrar ADN Shiny

    [Header("Gameplay")]
    public float gameDuration = 20f;
   
     public int monedasGanadasSesion;


    [Header("Configuración Inicial por Zona")]
    public int[] faseInicialPorMapa;

    [Header("Configuración Visual por Zona")]
    public Color[] coloresPorMapa; // Define aquí el color de cada nivel (Zona 0, Zona 1, etc.)


    [HideInInspector] public bool isGameActive;
    [HideInInspector] public int currentSessionInfected;

    float currentTimer;
    public bool timerStarted = false; // Nueva variable


    [Header("Transición")]
    public Collections.Shaders.ShapeTransition.ShapeTransition transitionScript;

    [Header("Extra Time Settings")]
    public GameObject extraTimeUI; // Arrastra aquí el objeto de texto "Extra Time"
    private List<PersonaInfeccion> figurasCandidatas = new List<PersonaInfeccion>();
    private bool checkParaExtraTimeRealizado = false;

    [Header("Configuración de Cámara y Final")]
    public Camera mainCamera;
    public float defaultZoom = 14f;
    public float endSessionZoom = 25f; // Cuanto más alto, más lejos se verá
    public float slowMotionDuration = 1.5f; // Segundos reales que dura el efecto

    private Coroutine animacionExtraTime;
    private Coroutine animacionTimer;
    private bool timerAnimando = false; // Para evitar que la corrutina se dispare mil veces

    public Image timerIcon;
    private Vector3 timerIconOriginalScale;

    // Añade esto en EndDayResultsPanel

    void Awake()
    {

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = PlayerPrefs.GetInt("FPSLimit", 120);

        float volumenGuardado = PlayerPrefs.GetFloat("VolumenGlobal", 1f);
        AudioListener.volume = volumenGuardado;

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ForceHardReset();
        ShowMainMenu();
        if (timerIcon != null)
        {
            timerIconOriginalScale = timerIcon.rectTransform.localScale;
        }
    }

    // --- FUNCIONES DE CONTROL DE PANELES (RESTAURADAS) ---
    public void OpenShinyShop() { if (shinyPanel != null) { shinyPanel.SetActive(true); UpdateUI(); } }
    public void CloseShinyShop()
    {
        if (shinyPanel != null)
            shinyPanel.SetActive(false);
        zonePanel.SetActive(true); // Volvemos al panel de fin de día al cerrar la tienda de ADN/Mejoras

    }

    public void OpenZoneShop() { if (zonePanel != null) { zonePanel.SetActive(true); UpdateUI(); } }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); }

    public void AddCoins(int amount)
    {
        ContagionCoins += amount;        // Total acumulado
        monedasGanadasSesion += amount;  // Solo esta partida
        UpdateUI();
    }
    public int ContagionCoins
    {
        get => contagionCoins;
        set
        {
            contagionCoins = value;
            UpdateCoinsUI();
        }
    }

    private void UpdateCoinsUI()
    {
        foreach (var t in contagionCoinsTexts)
            if (t != null)
                t.text = "Monedas: " + contagionCoins;
    }


    public void RegisterInfection()
    {

        currentSessionInfected++;
        UpdateUI();
    }

    public Color GetCurrentLevelColor()
    {
        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        Color colorFinal = Color.white;

        if (coloresPorMapa != null && currentMap < coloresPorMapa.Length)
        {
            colorFinal = coloresPorMapa[currentMap];
        }

        // Forzamos el Alfa a 1 para que no sea transparente
        colorFinal.a = 1f;
        return colorFinal;
    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        SetMapsActive(false); // <--- AÑADIR ESTA LÍNEA

        if (pausePanel) pausePanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);
        if (shinyPanel) shinyPanel.SetActive(false);

        virusPlayer.SetActive(false);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            if (Guardado.instance != null && Guardado.instance.HasSavedGame())
            {
                continueButton.interactable = true;
                if (continueInfoText)
                {
                    continueInfoText.text = Guardado.instance.GetContinueDetails();
                    continueInfoText.alpha = 1f;
                }
            }
            else
            {
                continueButton.interactable = false;
                if (continueInfoText)
                {
                    continueInfoText.text = "Sin datos";
                    continueInfoText.alpha = 0.5f;
                }
            }
        }
    }

    public void Button_NewGame()
    {
        if (Guardado.instance) Guardado.instance.ResetAllProgress();
        NewGameFromMainMenu();
    }

    public void AddTimeToCurrentTimer(float seconds)
    {
        if (!isGameActive) return;

        currentTimer += seconds;
        UpdateUI(); // para que el texto del timer refleje el cambio si lo muestras
    }

    public void Button_Continue()
    {
        LoadRunAndStart();
    }

    void LoadRunAndStart()
    {
        ContagionCoins = PlayerPrefs.GetInt("Run_Coins", 0);
        int savedMap = PlayerPrefs.GetInt("Run_Map", 0);

        Guardado.instance.LoadEvolutionData();
        int monedasGanadas = PlayerPrefs.GetInt("Run_MonedasGanadas", 0);

        EndDayResultsPanel.instance.ShowResults(
            monedasGanadas,
            contagionCoins
        );

        PlayerPrefs.SetInt("CurrentMapIndex", savedMap);
        PlayerPrefs.Save();

        // CAMBIO: Ahora usamos la transición para ir del menú al panel de zona
     
            if (transitionScript != null) transitionScript.SetShape(1);

            StartCoroutine(TransitionRoutine(menuPanel, gameUI));
        
    }

    public void NewGameFromMainMenu()
    {
        ResetRunData();
        // Inyectamos el Hexágono (1)
        if (transitionScript != null) transitionScript.SetShape(1);

        StartCoroutine(TransitionRoutine(menuPanel, null, true));
    }
    void ForceHardReset()
    {
        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();
    }

    void Update()
    {
        if (!isGameActive) return;
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();

        if (timerStarted)
        {
            currentTimer -= Time.deltaTime;
        }

        // Actualizar textos del timer
        // Actualizar textos del timer
        foreach (var t in timerTexts)
        {
            if (t != null)
            {
                if (currentTimer > 0)
                {
                    t.text = currentTimer.ToString("F1") + "s";
                    // Aseguramos que si el tiempo vuelve a ser > 0 (por un powerup), la escala sea normal
                    t.rectTransform.localScale = Vector3.one;
                }
                else
                {
                    t.text = "0.0s"; // Forzamos el 0.0 visual

                    // Si el timer llega a 0 y no hemos empezado a animar, arrancamos el "latido"
                    if (!timerAnimando)
                    {
                        timerAnimando = true;
                        if (animacionTimer != null) StopCoroutine(animacionTimer);
                        animacionTimer = StartCoroutine(AnimarRespiracionTimer());
                    }
                }
            }
        }

        // --- LÓGICA DE TIEMPO EXTRA Y FIN DE SESIÓN ---
        if (currentTimer <= 0)
        {
            // 1. Justo al llegar a 0, guardamos una "foto" de quiénes están dentro
            if (!checkParaExtraTimeRealizado)
            {
                figurasCandidatas.Clear();
                // Buscamos solo las personas que están en el radio en este instante
                PersonaInfeccion[] todas = Object.FindObjectsByType<PersonaInfeccion>(FindObjectsSortMode.None);
                foreach (var p in todas)
                {
                    if (p.IsInsideZone && !p.alreadyInfected)
                    {
                        figurasCandidatas.Add(p);
                    }
                }
                checkParaExtraTimeRealizado = true;
            }

            // 2. Verificamos si alguna de esas figuras candidatas sigue siendo válida
            if (figurasCandidatas.Count > 0)
            {
                ValidarEstadoTiempoExtra();
            }
            else
            {

                EndSessionDay();
            }
        }
    }
    private IEnumerator AnimarRespiracionTimer()
    {
        while (timerAnimando)
        {
            // Usamos un factor común basado en el tiempo global del juego
            // 5f es la velocidad, 0.15f es la amplitud (qué tanto crece)
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f;

            foreach (var t in timerTexts)
            {
                if (t != null) t.rectTransform.localScale = Vector3.one * pulse;
            }

            if (timerIcon != null)
            {
                timerIcon.rectTransform.localScale = timerIconOriginalScale * pulse;
            }

            yield return null;
        }

        // Reset al terminar
        ResetTimerVisuals();
    }
    private void ValidarEstadoTiempoExtra()
    {
        for (int i = figurasCandidatas.Count - 1; i >= 0; i--)
        {
            var p = figurasCandidatas[i];
            if (p == null || p.alreadyInfected || !p.IsInsideZone)
            {
                figurasCandidatas.RemoveAt(i);
            }
        }

        if (figurasCandidatas.Count > 0)
        {
            if (extraTimeUI != null && !extraTimeUI.activeSelf)
            {
                extraTimeUI.SetActive(true);
                // Iniciamos la animación solo si no estaba ya activa
                if (animacionExtraTime != null) StopCoroutine(animacionExtraTime);
                animacionExtraTime = StartCoroutine(AnimarTextoExtraTime());
            }
        }
        
    }

    private IEnumerator AnimarTextoExtraTime()
    {
        RectTransform rect = extraTimeUI.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;

        float appearanceDuration = 0.4f; // Tiempo que tarda en aparecer
        float elapsed = 0f;

        // 1. Fase de aparición con mezcla suave hacia el latido
        while (elapsed < appearanceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / appearanceDuration;

            // Curva de aparición (de 0 a 1)
            float appearanceScale = Mathf.SmoothStep(0f, 1.0f, progress);

            // Calculamos el pulso actual del "metrónomo" global
            float currentPulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f;

            // Mezclamos la aparición con el pulso para que cuando llegue a 1.0 
            // ya esté vibrando al mismo ritmo que el timer.
            float finalScale = appearanceScale * currentPulse;

            rect.localScale = new Vector3(finalScale, finalScale, 1f);
            yield return null;
        }

        // 2. Bucle infinito sincronizado (sin interrupciones)
        while (extraTimeUI.activeSelf)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f;
            rect.localScale = new Vector3(pulse, pulse, 1f);
            yield return null;
        }
    }

    private void ResetTimerVisuals()
    {
        foreach (var t in timerTexts)
        {
            if (t != null) t.rectTransform.localScale = Vector3.one;
        }
        if (timerIcon != null)
        {
            timerIcon.rectTransform.localScale = timerIconOriginalScale;
        }
    }
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (Guardado.instance)
        {
            int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

     
   
        if (pausePanel != null) pausePanel.SetActive(false);
        if (shinyPanel != null) shinyPanel.SetActive(false);

        ShowMainMenu();
    }

    public void ActivateMap(int zoneID)
    {
        PlayerPrefs.SetInt("CurrentMapIndex", zoneID);
        PlayerPrefs.Save();

        for (int i = 0; i < mapList.Length; i++)
        {
            if (mapList[i] != null) mapList[i].SetActive(i == zoneID);
        }

        PopulationManager popManager = Object.FindFirstObjectByType<PopulationManager>();
        if (popManager != null) popManager.SelectPrefab(zoneID);
    }

    void ResetRunData()
    {
        bool tieneHabilidadMeta = Guardado.instance != null && Guardado.instance.keepZonesUnlocked;
        if (!tieneHabilidadMeta)
        {
            for (int i = 1; i <= 10; i++) PlayerPrefs.SetInt("ZoneUnlocked_" + i, 0);
        }

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();
        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset) ForceHardReset();
        if (Guardado.instance) Guardado.instance.ApplyPermanentInitialUpgrade();

        ContagionCoins = Guardado.instance != null ? Guardado.instance.startingCoins : 0;
        UpdateUI();
    }

    public void StartSession()
    {
        timerStarted = false;
        checkParaExtraTimeRealizado = false; // <--- AÑADE ESTO AQUÍ
        figurasCandidatas.Clear();
        if (menuPanel) menuPanel.SetActive(false);
        ResetCameraZoom(); // <--- IMPORTANTE: Volver al zoom de 14
                           // RESET DE CÁMARA Y TIEMPO

        if (extraTimeUI != null)
        {
            extraTimeUI.SetActive(false);
            extraTimeUI.GetComponent<RectTransform>().localScale = Vector3.one;
        }
        if (timerIcon != null) if (timerIcon != null) timerIcon.rectTransform.localScale = timerIconOriginalScale;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.002f;
        if (mainCamera == null) mainCamera = Camera.main;
        mainCamera.orthographicSize = defaultZoom;

        timerAnimando = false; // <-- RESETEAR ESTO
        if (animacionTimer != null) StopCoroutine(animacionTimer);

        // Resetear escala de los textos por si acaso
        foreach (var t in timerTexts) if (t != null) t.rectTransform.localScale = Vector3.one;

        checkParaExtraTimeRealizado = false;

        // Si no hay nadie dentro o ya terminaron, cerramos la sesión
        if (extraTimeUI != null) extraTimeUI.SetActive(false);
        if (animacionExtraTime != null) StopCoroutine(animacionExtraTime);


        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToGameMusic();

        CleanUpScene();

        int savedMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        SetMapsActive(true);

        isGameActive = true;
        currentSessionInfected = 0;
        monedasGanadasSesion = 0;


        // Reset estadísticas de evolución entre fases
        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            PersonaInfeccion.evolucionesEntreFases[i] = 0;
        }
        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            PersonaInfeccion.evolucionesPorChoque[i] = 0;
        }


        float tiempoTotal = gameDuration;

        if (Guardado.instance != null)
            tiempoTotal += Guardado.instance.extraBaseTime;

        currentTimer = tiempoTotal;


        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(0);

        gameUI.SetActive(true);
        virusPlayer.SetActive(true);
        if (virusMovementScript != null) virusMovementScript.enabled = true;
        UpdateUI();
    }
    public void StartSessionWithTransition()
    {
        StartCoroutine(TransitionToSession());
    }

    private IEnumerator TransitionToSession()
    {
        if (transitionScript != null)
        {
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // Llamamos a la lógica normal de inicio de sesión
        StartSession();

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
    }
    public void ResumeSession()
    {
      
        if (menuPanel) menuPanel.SetActive(false);

        Time.timeScale = 1f;

        isGameActive = true;
        currentSessionInfected = 0;
        monedasGanadasSesion = 0;
        currentTimer = gameDuration;

        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null)
        {
            pm.ClearAllPersonas();
            pm.ConfigureRound(0);
        }

        gameUI.SetActive(true);
        virusPlayer.SetActive(true);
        if (virusMovementScript != null)
            virusMovementScript.enabled = true;

        UpdateUI();
    }


    void EndSessionDay()
    {
        // Evitamos que se llame varias veces si el timer llega a 0 y hay lag
        if (!isGameActive) return;

        isGameActive = false;
        StartCoroutine(SlowMotionExitRoutine());
    }
    public void ResetCameraZoom()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        mainCamera.orthographicSize = defaultZoom;
    }
    private IEnumerator SlowMotionExitRoutine()
    {
        float currentTime = 0f;
        float startScale = 1f;
        float endScale = 0.05f; // Casi detenido al final

        // Asegurar referencia a la cámara
        if (mainCamera == null) mainCamera = Camera.main;

        // Desactivamos el movimiento del virus para evitar que "patine" por el lag físico
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        while (currentTime < slowMotionDuration)
        {
            // Usamos unscaledDeltaTime porque el timeScale estará bajando
            currentTime += Time.unscaledDeltaTime;
            float t = currentTime / slowMotionDuration;

            // Suavizado de la curva (SmoothStep)
            float smoothT = t * t * (3f - 2f * t);

            // 1. EFECTO CÁMARA LENTA (Slow Motion)
            // Bajamos el timeScale de 1.0 a 0.05
            Time.timeScale = Mathf.Lerp(startScale, endScale, t);

            // Ajustamos el tiempo físico para que las animaciones no den tirones
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // 2. EFECTO ZOOM OUT
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = Mathf.Lerp(defaultZoom, endSessionZoom, smoothT);
            }

            yield return null;
        }

        // Final de la transición
        CompleteEndSessionLogic();
    }

    private void CompleteEndSessionLogic()
    {
        // Iniciamos la transición a negro antes de mostrar el panel
        if (transitionScript != null)
        {
            transitionScript.SetShape(1); // O la forma que prefieras
            transitionScript.CloseBlackScreen();
        }

        StartCoroutine(ShowResultsWithTransition());
    }

    private IEnumerator ShowResultsWithTransition()
    {
        // Esperamos a que la pantalla esté en negro (ajusta el tiempo según tu shader)
        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 0f;
        SetMapsActive(false);
        gameUI.SetActive(false);

        int totalAntes = contagionCoins - monedasGanadasSesion;
        int totalFinal = totalAntes + monedasGanadasSesion;

        if (EndDayResultsPanel.instance != null)
        {
            EndDayResultsPanel.instance.ShowResults(monedasGanadasSesion, totalFinal);
        }

        if (Guardado.instance != null)
            Guardado.instance.AddTotalData(currentSessionInfected);

        // Abrimos el iris/forma para mostrar el panel de resultados
        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
    }

    public void OnEndDayResultsFinished(int earnings, int dummy)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        virusPlayer.SetActive(false);

        if (AudioManager.instance != null)
            AudioManager.instance.SwitchToMenuMusic();

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        if (Guardado.instance)

      

        UpdateUI();
    }

    public void GameOver()
    {
   
       
        if (Guardado.instance) Guardado.instance.ClearRunState();
    }

    public void UpdateUI()
    {
       
        foreach (var t in contagionCoinsTexts) if (t != null) t.text = "Monedas: " + contagionCoins;
    }

    public void LostToMenu() { ResetRunData(); ShowMainMenu(); }

    void CleanUpScene()
    {
        PersonaInfeccion[] gente = Object.FindObjectsByType<PersonaInfeccion>(FindObjectsSortMode.None);
        foreach (PersonaInfeccion p in gente) if (p != null) Destroy(p.gameObject);
    }

    public int GetTotalUnlockedZones()
    {
        int count = 1;
        for (int i = 1; i <= 10; i++) if (PlayerPrefs.GetInt("ZoneUnlocked_" + i, 0) == 1) count++;
        return count;
    }
    public void SoftRestartRun()
    {
       

        // 1. Si el panel de resultados tiene monedas pendientes, procesar animación de monedas
        if (EndDayResultsPanel.instance.panel.activeSelf && EndDayResultsPanel.instance.TieneMonedasPendientes)
        {
            EndDayResultsPanel.instance.StartCoinTransfer(() => {
                Debug.Log("Animación de monedas terminada.");
            });
            return;
        }

        // 2. Si ya no hay monedas o el panel no estaba, ejecutar el reinicio con transición
        StartCoroutine(SoftRestartTransitionRoutine());
    }

    private IEnumerator SoftRestartTransitionRoutine()
    {
        // A. Inicio de la transición (Pantalla a negro)
        if (transitionScript != null)
        {
            transitionScript.SetShape(1); // Usar Hexágono o la forma que prefieras
            transitionScript.CloseBlackScreen();
          
            yield return new WaitForSecondsRealtime(0.5f);
           
        }
        if (shinyPanel != null) shinyPanel.SetActive(false);
        // B. Limpieza de UI
        if (shinyPanel != null) shinyPanel.SetActive(false);
        if (EndDayResultsPanel.instance.panel.activeSelf)
        {
            EndDayResultsPanel.instance.panel.SetActive(false);
        }
     
        if (gameUI) gameUI.SetActive(false);

        // C. Lógica de Reinicio (Extraída de EjecutarSoftRestartLogica)
        Debug.Log("Soft Restart ejecutado bajo transición.");
        monedasGanadasSesion = 0;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();

        if (MapSequenceManager.instance != null)
        {
            MapSequenceManager.instance.ResetToFirstMap();
        }

        ManualSetCycler cycler = Object.FindFirstObjectByType<ManualSetCycler>();
        if (cycler != null) cycler.ResetCycler();

        // Resetear Mapas
        for (int i = 0; i < mapList.Length; i++)
        {
            if (mapList[i] != null)
            {
                mapList[i].transform.rotation = Quaternion.identity;
                mapList[i].transform.localPosition = Vector3.zero;
                mapList[i].transform.localScale = Vector3.one;
                mapList[i].SetActive(i == 0);
            }
        }

        // Resetear Salud de Planetas
        PlanetCrontrollator[] todosLosPlanetas = Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlanetCrontrollator planet in todosLosPlanetas)
        {
            if (planet != null) planet.ResetHealthToInitial();
        }

        isGameActive = false;
        CleanUpScene();

        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.ClearAllPersonas();
            PopulationManager.instance.SelectPrefab(0);
        }

        UpdateUI();

        // D. Iniciar la nueva sesión (Aún en negro)
        yield return new WaitForSecondsRealtime(0.1f);
        StartSession();

        // E. Fin de la transición (Volver a mostrar el juego)
        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
    }
    private void EjecutarSoftRestartLogica()
    {
        Debug.Log("Soft Restart ejecutado: Reiniciando planetas y rotaciones.");

        // 1. Resetear Datos de Sesión
        monedasGanadasSesion = 0;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        // 2. Volver al Mapa 0
        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();

        ManualSetCycler cycler = Object.FindFirstObjectByType<ManualSetCycler>();
        if (cycler != null)
        {
            cycler.ResetCycler();
        }

        // 3. Resetear Mapas y Rotaciones
        for (int i = 0; i < mapList.Length; i++)
        {
            if (mapList[i] != null)
            {
                mapList[i].transform.rotation = Quaternion.identity;
                mapList[i].transform.localPosition = Vector3.zero;
                mapList[i].transform.localScale = Vector3.one;
                mapList[i].SetActive(i == 0);
            }
        }
        // Buscamos TODOS los scripts de planetas, incluso los que están desactivados
        PlanetCrontrollator[] todosLosPlanetas = Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlanetCrontrollator planet in todosLosPlanetas)
        {
            if (planet != null)
            {
                planet.ResetHealthToInitial();
                // Opcional: Si el planeta está en un mapa que no es el inicial, 
                // asegúrate de que su objeto padre esté desactivado si es necesario
            }
        }

        // 5. Limpieza y UI
        isGameActive = false;
        CleanUpScene();
        if (gameUI) gameUI.SetActive(false);

        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.ClearAllPersonas();
            PopulationManager.instance.SelectPrefab(0);
        }

        UpdateUI();
        StartCoroutine(RestartNextFrame());
    }

    IEnumerator RestartNextFrame()
    {
        yield return null; // Espera 1 frame completo
        StartSession();
    }


    public void TogglePause()
    {
        if (pausePanel == null) return;
        bool estaPausado = pausePanel.activeSelf;
        if (estaPausado)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
            if (virusMovementScript != null) virusMovementScript.enabled = true;
        }
        else
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
            if (virusMovementScript != null) virusMovementScript.enabled = false;
        }
    }
    // --- AÑADIR ESTO A TU LevelManager.cs ---

    // --- AÑADE O MODIFICA ESTO EN LevelManager.cs ---

    public void NextMapTransition()
    {
        if (!isGameActive) return;

        // Buscamos el script de transición en la escena
        LevelTransitioner transitioner = Object.FindFirstObjectByType<LevelTransitioner>();

        if (transitioner != null)
        {
            // Si existe el script de las rotaciones, que él tome el control
            transitioner.StartLevelTransition();
        }
        else
        {
            // Si por alguna razón no está el script de giro, 
            // hace el cambio normal que tenías antes para no romper el juego
            StartCoroutine(WaitAndChangeMap());
        }
    }

    private IEnumerator WaitAndChangeMap()
    {
        // En lugar de isGameActive = false (que detiene el spawn), 
        // podrías dejarlo activo si quieres que sigan naciendo durante la transición.

        yield return new WaitForSecondsRealtime(0.5f);

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int nextMap = currentMap + 1;

        if (nextMap < mapList.Length)
        {
            ActivateMap(nextMap);
            yield return new WaitForEndOfFrame();

            PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
            if (pm != null)
            {
                pm.RefreshSpawnArea(); // Ahora los nuevos nacerán en el nuevo mapa
            }

            // No reseteamos currentSessionInfected si quieres que sea una sola carrera continua
            // currentSessionInfected = 0; 
        }
    }

    public void MostrarPuntosVoladores(Vector3 posicionPersona, int puntosGanados)
    {
        AddCoins(puntosGanados); // Sumamos el dinero

        // --- ESTA LÍNEA TIENE QUE ESTAR ASÍ (Con los == null) ---
        if (prefabTextoPuntos == null || canvasPrincipal == null || marcadorDestinoUI == null)
        {
            // Si falta algo, avisamos pero NO petamos el juego
            Debug.LogWarning("Faltan referencias en LevelManager para el texto volador");
            return;
        }

        GameObject nuevoTexto = Instantiate(prefabTextoPuntos, canvasPrincipal.transform);
        FloatingScoreUI scriptVuelo = nuevoTexto.GetComponent<FloatingScoreUI>();

        if (scriptVuelo != null)
        {
            scriptVuelo.IniciarViaje(puntosGanados, posicionPersona, marcadorDestinoUI, canvasPrincipal);
        }
    }

    public void OpenSkillTreePanel()
    {
        // 1. Si el panel está abierto y hay monedas...
        if (EndDayResultsPanel.instance.panel.activeSelf && EndDayResultsPanel.instance.TieneMonedasPendientes)
        {
            EndDayResultsPanel.instance.StartCoinTransfer(() => {
                Debug.Log("Animación terminada. Pulsa otra vez para ir al Skill Tree.");
            });
            return; // Obliga a una segunda pulsación
        }

        // 2. Si ya se contaron las monedas...
        if (EndDayResultsPanel.instance.panel.activeSelf)
        {
            EndDayResultsPanel.instance.panel.SetActive(false);
        }

        EjecutarAbrirSkillTree();
    }

    private void EjecutarAbrirSkillTree()
    {
        Debug.Log("BOTON PULSADO - Abriendo Skill Tree");
        // Cerrar todos los paneles primero
        if (menuPanel) menuPanel.SetActive(false);
        if (gameUI) gameUI.SetActive(false);

       
        if (pausePanel) pausePanel.SetActive(false);
        if (shinyPanel) zonePanel.SetActive(false);

        // Abrir panel de habilidades
        if (zonePanel) shinyPanel.SetActive(true);

        RebuildSkillTree();

        Time.timeScale = 1f; // aseguramos que no esté pausado
        foreach (var node in FindObjectsOfType<SkillNode>())
        {
            node.CheckIfShouldShow();
        }


    }



    public void RebuildSkillTree()
    {
        var nodes = FindObjectsOfType<SkillNode>();

        // 1️⃣ Cargar estado de todos
        foreach (var node in nodes)
            node.LoadNodeState();

        // 2️⃣ Forzar activación base (mostrar todos inicialmente)
        foreach (var node in nodes)
            node.gameObject.SetActive(true);

        // 3️⃣ Ahora evaluar jerarquía correctamente
        foreach (var node in nodes)
            node.CheckIfShouldShow();
    }


    public void ContinueCurrentMap()
    {
        Time.timeScale = 1f;

        isGameActive = true;
        currentTimer = gameDuration;
        currentSessionInfected = 0;

        PopulationManager.instance.ClearAllPersonas();
        PopulationManager.instance.ConfigureRound(0);

        if (virusPlayer != null)
            virusPlayer.SetActive(true);

        if (virusMovementScript != null)
            virusMovementScript.enabled = true;

        if (gameUI != null)
            gameUI.SetActive(true);

        UpdateUI();
    }


    IEnumerator ContinueNextFrame()
    {
        yield return null;

        isGameActive = true;

        PopulationManager.instance.ConfigureRound(0);

        UpdateUI();
    }
    public void SaveCurrentRun()
    {
        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        Guardado.instance.SaveRunState(
            0,
            contagionCoins,
            currentMap
        );

        Guardado.instance.SaveEvolutionData();

        Debug.Log("PARTIDA GUARDADA COMPLETA");
    }

    public void StartSessionWithoutResettingPlanet()
    {
        isGameActive = true;

        currentTimer = gameDuration;


        PopulationManager.instance.ConfigureRound(0);

        UpdateUI();
    }

    // Método genérico para cambiar entre paneles con transición
    public void ChangePanelWithTransition(GameObject panelToClose, GameObject panelToOpen)
    {
        StartCoroutine(TransitionRoutine(panelToClose, panelToOpen));
    }

    private IEnumerator TransitionRoutine(GameObject panelToClose, GameObject panelToOpen, bool isStartingSession = false)
    {
        if (transitionScript != null)
        {
            // 1. Cerramos el círculo (Pantalla a negro)
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 2. CAMBIO DE PANELES
        if (panelToClose != null) panelToClose.SetActive(false);
        if (panelToOpen != null) panelToOpen.SetActive(true);

        // 3. SI ES UN NUEVO JUEGO, LANZAMOS LA SESIÓN AQUÍ (En el momento de oscuridad total)
        if (isStartingSession)
        {
            StartSession();
        }

        yield return new WaitForEndOfFrame();

        if (transitionScript != null)
        {
            // 4. Abrimos el círculo (Vuelve la imagen)
            transitionScript.OpenBlackScreen();
        }
    }

    private void SetMapsActive(bool state)
    {
        // 1. Control de Mapas
        if (mapList != null)
        {
            for (int i = 0; i < mapList.Length; i++)
            {
                if (mapList[i] != null)
                {
                    if (state)
                    {
                        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
                        mapList[i].SetActive(i == currentMap);
                    }
                    else
                    {
                        mapList[i].SetActive(false);
                    }
                }
            }
        }

        // 2. Control del Jugador
        if (virusPlayer != null) virusPlayer.SetActive(state);
        if (virusMovementScript != null) virusMovementScript.enabled = state;

        // 3. NUEVO: Limpieza de Personas y Corales si estamos desactivando el juego
        if (state == false) // Si state es false, significa que salimos al menú o terminó el día
        {
            if (PopulationManager.instance != null)
            {
                PopulationManager.instance.ClearAllPersonas();
            }
        }
    }



}