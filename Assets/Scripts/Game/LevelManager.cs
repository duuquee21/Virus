using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems; // <-- AÑADIDO PARA EL MANDO
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Selector de Modo (Menú Principal)")]
    public GameObject modeSelectionPanel;
    public Button continueButton;
    public TextMeshProUGUI continueInfoText;
    public GameObject jugadorVirus;
    public GameObject panelFinal; // Arrastra el panel aquí en el Inspector de este script



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
    public GameObject pauseFirstSelectedButton; // <-- NUEVO: EL BOTÓN QUE SELECCIONA EL MANDO AL PAUSAR
    public GameObject settingsPanel;
    public GameObject settingsFirstSelectedButton;
    [Header("UI Text (Listas)")]
    public List<TextMeshProUGUI> timerTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> sessionScoreTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> contagionCoinsTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> shinyStoreTexts = new List<TextMeshProUGUI>(); // Texto para mostrar ADN Shiny

    [Header("Gameplay")]
    public float gameDuration = 20f;

    public int monedasGanadasSesion;

    [Header("Configuración de Demo")]
    public bool esVersionDemo = false; // Márcarlo en Unity para exportar la demo
    public GameObject panelFinDemo; // El panel que dice "¡Gracias por jugar!"


    [Header("Configuración Inicial por Zona")]
    public int[] faseInicialPorMapa;

    [Header("Configuración Visual por Zona")]
    public Color[] coloresPorMapa; // Define aquí el color de cada nivel (Zona 0, Zona 1, etc.)


    [HideInInspector] public bool isGameActive;
    [HideInInspector] public bool isTransitioning;
    [HideInInspector] public int currentSessionInfected;

    float currentTimer;
    public bool timerStarted = false; // Nueva variable


    [Header("Transición")]
    public Collections.Shaders.ShapeTransition.ShapeTransition transitionScript;
    public float tiempoEsperaEnNegro = 0.2f; // <-- NUEVO: Tiempo que se queda la pantalla en negro total


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

    // Sistema de guardado automático de monedas
    private float autoSaveInterval = 10f; // Guardar cada 10 segundos
    private float timeSinceLastAutoSave = 0f;

    private int lastActiveMapIndex = -1;
    private PopulationManager cachedPopManager;

    // --- NUEVAS VARIABLES PARA LA ANIMACIÓN ---
    private float visualCoins; // El número que se ve actualmente
    private Coroutine coinAnimationCoroutine;

    // Añade esto en EndDayResultsPanel

    // Añade esto cerca de las otras variables de estado
    public bool IsTimeUp => currentTimer <= 0;


    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); return; }

        // Caché de componentes para evitar Find en cada transición
        cachedPopManager = Object.FindFirstObjectByType<PopulationManager>();
    }
    // Método para obtener el texto traducido
    string GetTexto(string clave)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedString("TextosJuego", clave);
        if (string.IsNullOrEmpty(op)) return clave;
        return op;
    }

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = PlayerPrefs.GetInt("FPSLimit", 120);

        // Alineamos la activación de audio con la clave principal del juego (MasterVolume)
        float volumenGuardado = PlayerPrefs.GetFloat("MasterVolume", PlayerPrefs.GetFloat("VolumenGlobal", 1f));
        AudioListener.volume = volumenGuardado;

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        // Solo resetear si NO hay una partida continuada guardada
        if (Guardado.instance != null && !Guardado.instance.HasSavedGame())
        {
            ForceHardReset();
        }

        ShowMainMenu();

        // Aseguramos que la música del menú suene desde el inicio del juego.
        if (AudioManager.instance != null)
            AudioManager.instance.SwitchToMenuMusic();

        if (timerIcon != null)
        {
            timerIconOriginalScale = timerIcon.rectTransform.localScale;
        }

        visualCoins = contagionCoins;
    }

    // --- FUNCIONES DE CONTROL DE PANELES (RESTAURADAS) ---
    public void OpenShinyShop() { if (shinyPanel != null) { shinyPanel.SetActive(true); UpdateUI(); } }
    public void CloseShinyShop()
    {
        StartCoroutine(TransitionBackFromSkillTree());
    }

    private IEnumerator TransitionBackFromSkillTree()
    {
        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        if (shinyPanel != null) shinyPanel.SetActive(false);

        // Volvemos al panel de zonas o resultados
        if (zonePanel != null) zonePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(0.1f);

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
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
            UpdateUI(); // Llamamos a UpdateUI que ahora gestiona la animación
        }
    }

    private void UpdateCoinsUI()
    {
        foreach (var t in contagionCoinsTexts)
            if (t != null)
                t.text = $"{GetTexto("txt_monedas_ui")}: {contagionCoins}";
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
        // IMPORTANTE: Quitamos el menuPanel.SetActive(true) de aquí si DoPanelTransition ya lo va a encender
        // menuPanel.SetActive(true); 

        gameUI.SetActive(false);
        SetMapsActive(false);
        UpdateCursorState(false);

        if (pausePanel) pausePanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);
        if (shinyPanel) shinyPanel.SetActive(false);
        if (panelFinal) panelFinal.SetActive(false);

        virusPlayer.SetActive(false);

        // Lógica del botón continuar...
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

        RebuildSkillTree();

        SkillTreeLinesUI lines = FindFirstObjectByType<SkillTreeLinesUI>();
        if (lines != null)
        {
            lines.ResetAllLinesVisuals();
            lines.RefreshAllLinesFromNodes();
        }

        NewGameFromMainMenu();
    }
    public void AddTimeToCurrentTimer(float seconds)
    {
        if (!isGameActive) return;

        currentTimer += seconds;
        UpdateUI();
    }

    public void Button_Continue()
    {
        LoadRunAndStart();
    }

    void LoadRunAndStart()
    {
        // Iniciamos la corrutina para que la carga respete la transición
        StartCoroutine(LoadRunRoutine());
    }

    private IEnumerator LoadRunRoutine()
    {
        // 1. Definimos TODA la lógica de carga como una acción que ocurrirá en la oscuridad total
        System.Action cargaEnLaOscuridad = () =>
        {
            // Limpiamos el estado actual por si acaso
            ResetSceneToNeutralState();

            // Recuperamos los datos guardados de PlayerPrefs
            float savedTimer = PlayerPrefs.GetFloat("Run_Timer", gameDuration);
            int savedCoins = PlayerPrefs.GetInt("Run_Coins", 0);
            int savedMapIndex = PlayerPrefs.GetInt("Run_MapIndex", 0);
            float savedPlanetHealth = PlayerPrefs.GetFloat("Run_PlanetHealth", -1f);

            // Aplicamos las monedas
            ContagionCoins = savedCoins;

            // Cargamos evoluciones y estadísticas
            if (Guardado.instance != null)
                Guardado.instance.LoadEvolutionData();

            PersonaInfeccion.LoadStats();

            // Reconstruimos el árbol de habilidades para que refleje lo cargado
            RebuildSkillTree();

            // Sincronizamos los controladores (velocidad, radio, etc)
            SyncControllersWithSavedData();

            // Limpiamos el sistema de rejilla espacial
            Movement.espacialGrid.Clear();

            // Iniciamos la sesión con los datos recuperados
            // Nota: StartLoadedSession activa el GameUI y desactiva el MenuPanel
            StartLoadedSession(savedTimer, savedMapIndex, savedPlanetHealth);

            Debug.Log("<color=green>[LOAD]</color> Partida recuperada con éxito en la oscuridad.");
        };

        // 2. Ejecutamos la transición unificada usando nuestro método maestro
        // Esto aplicará: Cierre Hexágono -> tiempoEsperaEnNegro -> Ejecutar cargaEnLaOscuridad -> Abrir Hexágono
        DoPanelTransition(menuPanel, null, cargaEnLaOscuridad);

        yield break; // Salimos de la corrutina vieja ya que DoPanelTransition toma el control
    }
    public void NewGameFromMainMenu()
    {
        // 1. Preparamos los datos de la nueva partida (borrar progreso previo de la run)
        ResetRunData();

        // 2. Identificamos qué panel cerrar (Ajustes si estaba abierto, o el Menú Principal)
        GameObject panelToClose = (settingsPanel != null && settingsPanel.activeSelf) ? settingsPanel : menuPanel;

        // 3. Usamos el método unificado
        // Pasamos 'null' en el panel a abrir porque 'StartSession' se encarga de encender el GameUI
        DoPanelTransition(panelToClose, null, () => {
            StartSession();
            Debug.Log("Sesión iniciada tras el tiempo de espera en negro.");
        });
    }
    void ForceHardReset()
    {
        if (VirusRadiusController.instance) VirusRadiusController.instance.ApplyScale();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();
    }

    // === SINCRONIZACIÓN DE CONTROLADORES AL REANUDAR PARTIDA ===
    void SyncControllersWithSavedData()
    {
        if (Guardado.instance == null) return;

        // 1. Sincronizar VirusRadiusController

        // 2. Sincronizar SpeedUpgradeController
        if (SpeedUpgradeController.instance != null)
        {
            int speedLevel = Guardado.instance.speedLevel;
            SpeedUpgradeController.instance.SetLevel(speedLevel);
        }

        // 3. Sincronizar InfectionSpeedUpgradeController
        if (InfectionSpeedUpgradeController.instance != null)
        {
            int infectionSpeedLevel = Guardado.instance.infectionSpeedLevel;
            InfectionSpeedUpgradeController.instance.SetLevel(infectionSpeedLevel);
        }

        // 4. Sincronizar CapacityUpgradeController (si existe)
        if (CapacityUpgradeController.instance != null)
        {
            int capacityLevel = Guardado.instance.capacityLevel;
            CapacityUpgradeController.instance.SetLevel(capacityLevel);
        }

        // 5. Sincronizar TimeUpgradeController (si existe)
        if (TimeUpgradeController.instance != null)
        {
            int timeLevel = Guardado.instance.timeLevel;
            TimeUpgradeController.instance.SetLevel(timeLevel);
        }

        Debug.Log($"<color=cyan>[SYNC]</color> ✓ Controladores sincronizados con datos guardados");
    }

    void Update()
    {
        if (!isGameActive) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsPanel();
            }
            else
            {
                TogglePause();
            }
            return;
        }

        if (pausePanel != null && pausePanel.activeSelf)
            return;

        if (timerStarted)
        {
            currentTimer -= Time.deltaTime;
        }

        foreach (var t in timerTexts)
        {
            if (t != null)
            {
                if (currentTimer > 0)
                {
                    t.text = currentTimer.ToString("F1") + "s";
                    t.rectTransform.localScale = Vector3.one;
                }
                else
                {
                    t.text = "0.0s";

                    if (!timerAnimando)
                    {
                        timerAnimando = true;
                        if (animacionTimer != null) StopCoroutine(animacionTimer);
                        animacionTimer = StartCoroutine(AnimarRespiracionTimer());
                    }
                }
            }
        }

        if (currentTimer <= 0 && !isTransitioning)
        {
            bool poseeMejora = Guardado.instance != null && Guardado.instance.hasExtraTimeUnlock;

            if (poseeMejora)
            {
                if (!checkParaExtraTimeRealizado)
                {
                    figurasCandidatas.Clear();
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

                if (figurasCandidatas.Count > 0)
                    ValidarEstadoTiempoExtra();
                else
                    EndSessionDay();
            }
            else
            {
                EndSessionDay();
            }
        }

        // --- LÓGICA DINÁMICA DEL CURSOR ---
        bool enMenuUI =
           (pausePanel != null && pausePanel.activeSelf) ||
           (settingsPanel != null && settingsPanel.activeSelf) ||
           (shinyPanel != null && shinyPanel.activeSelf) ||
           (zonePanel != null && zonePanel.activeSelf) ||
           (panelFinDemo != null && panelFinDemo.activeSelf) ||
           (panelFinal != null && panelFinal.activeSelf);

        if (isGameActive && !enMenuUI)
        {
            // Solo entramos aquí si estamos jugando y no hay menús abiertos
            if (Guardado.instance != null)
            {
                // Si el modo es Teclado o Mando, ocultamos el cursor
                if (Guardado.instance.inputType == Guardado.InputType.Keyboard ||
                    Guardado.instance.inputType == Guardado.InputType.Controller)
                {
                    if (Cursor.visible) Cursor.visible = false;
                }
                else // Si el modo es Mouse, lo mostramos
                {
                    if (!Cursor.visible) Cursor.visible = true;
                }
            }

            // Lógica de auto-guardado que ya tenías
            timeSinceLastAutoSave += Time.deltaTime;
            if (timeSinceLastAutoSave >= autoSaveInterval)
            {
                SaveCurrentRun();
                timeSinceLastAutoSave = 0f;
            }
        }
        else
        {
            // Si estamos en un menú o el juego no ha empezado, SIEMPRE mostrar cursor
            if (!Cursor.visible) Cursor.visible = true;
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
        // 1. Lógica de guardado previa si la partida está activa
        if (isGameActive && Guardado.instance != null)
        {
            SaveCurrentRun();
        }

        // 2. Definimos qué queremos que pase "en la oscuridad" (limpieza de escena)
        System.Action logicEnLaOscuridad = () => {
            // Detener música de juego y poner la de menú
            if (AudioManager.instance != null)
                AudioManager.instance.SwitchToMenuMusic();

            SkillNode.ClearRuntimeState();

            // Limpieza física de la escena (borrar enemigos, proyectiles, etc.)
            ResetSceneToNeutralState();

            // Configurar el panel de menú principal
            ShowMainMenu();

            // Aseguramos que el panel final de resultados se apague si venimos de ahí
            if (panelFinal != null) panelFinal.SetActive(false);
            if (EndDayResultsPanel.instance != null && EndDayResultsPanel.instance.panel != null)
                EndDayResultsPanel.instance.panel.SetActive(false);
        };

        // 3. Lanzamos la transición unificada
        // El 'panelToClose' será el de pausa o el de resultados, según cuál esté abierto
        GameObject panelActual = (pausePanel.activeSelf) ? pausePanel : (panelFinal != null && panelFinal.activeSelf ? panelFinal : gameUI);

        DoPanelTransition(panelActual, menuPanel, logicEnLaOscuridad);
    }
    public void ActivateMap(int zoneID)
    {
        // 🛑 EL MURO DEFINITIVO DE LA DEMO 🛑
        // Da igual si el juego intenta cargar el mapa por una transición, 
        // o si el jugador hace clic en un botón de la tienda de zonas.
        // Si intenta cargar el Pentágono (1) o superior, ¡ZAS!, pantalla de demo.
        if (esVersionDemo && zoneID > 0)
        {
            MostrarFinDeDemo();
            return; // Cortamos la ejecución, el mapa nunca llega a cargar.
        }

        PlayerPrefs.SetInt("CurrentMapIndex", zoneID);

        // Limpieza usando la caché
        if (cachedPopManager != null)
        {
            cachedPopManager.ClearAllPersonas();
            cachedPopManager.SelectPrefab(zoneID);
        }

        // OPTIMIZACIÓN: Solo apaga el anterior y enciende el nuevo (sin bucles)
        if (lastActiveMapIndex != -1 && lastActiveMapIndex < mapList.Length)
        {
            if (mapList[lastActiveMapIndex] != null) mapList[lastActiveMapIndex].SetActive(false);
        }

        if (zoneID < mapList.Length && mapList[zoneID] != null)
        {
            mapList[zoneID].SetActive(true);
            lastActiveMapIndex = zoneID;
        }
    }

    private void ResetSceneToNeutralState()
    {
        isGameActive = false;
        isTransitioning = false;
        timerStarted = false;
        checkParaExtraTimeRealizado = false;

        if (InfectionShaderController.instance != null)
        {
            InfectionShaderController.instance.ForzarReinicio();
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;


        StopAllActiveRunEffects();
        if (animacionExtraTime != null)
        {
            StopCoroutine(animacionExtraTime);
            animacionExtraTime = null;
        }

        LevelTransitioner transitioner = Object.FindFirstObjectByType<LevelTransitioner>();
        if (transitioner != null)
        {
            transitioner.ResetFinalLevelEffects();
        }

        if (animacionTimer != null)
        {
            StopCoroutine(animacionTimer);
            animacionTimer = null;
        }

        timerAnimando = false;
        ResetTimerVisuals();

        if (extraTimeUI != null) extraTimeUI.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (zonePanel != null) zonePanel.SetActive(false);
        if (shinyPanel != null) shinyPanel.SetActive(false);

        figurasCandidatas.Clear();

        if (cachedPopManager == null)
            cachedPopManager = Object.FindFirstObjectByType<PopulationManager>();

        if (cachedPopManager != null)
            cachedPopManager.ClearAllPersonas();

        CleanUpScene();

        ManualSetCycler cycler = Object.FindFirstObjectByType<ManualSetCycler>();
        if (cycler != null)
            cycler.ResetCycler();

        for (int i = 0; i < mapList.Length; i++)
        {
            if (mapList[i] != null)
            {
                mapList[i].transform.rotation = Quaternion.identity;
                mapList[i].transform.localPosition = Vector3.zero;
                mapList[i].transform.localScale = Vector3.one;
                mapList[i].SetActive(false);
            }
        }

        PlanetCrontrollator[] planetas =
            Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlanetCrontrollator p in planetas)
        {
            if (p != null)
            {
                p.ResetHealthToInitial();
                p.ClearPendingDamage();
                p.isInvulnerable = false;
            }
        }

        lastActiveMapIndex = -1;

        if (virusPlayer != null) virusPlayer.SetActive(false);
        if (virusMovementScript != null) virusMovementScript.enabled = false;
    }
    private void StopAllActiveRunEffects()
    {
        // 1. Paramos los generadores para que no creen NADA nuevo
        BlackSwordSpawner sword = Object.FindFirstObjectByType<BlackSwordSpawner>(FindObjectsInactive.Include);
        if (sword != null) sword.StopAllCoroutines();

        BlackHoleController hole = Object.FindFirstObjectByType<BlackHoleController>(FindObjectsInactive.Include);
        if (hole != null)
        {
            hole.StopAllCoroutines();
            hole.ClearActiveEffects();
        }

        // 2. BORRADO FÍSICO (Lo más importante para que no se quede pillado el sprite)
        // Buscamos todos los objetos que tengan tus scripts de efecto y los destruimos

        // Para los Tajos de la Espada (buscamos por el nombre del objeto o un Tag)
        // Si tus prefabs tienen un tag llamado "Efectos", esto es infalible:
        GameObject[] efectosEnEscena = GameObject.FindGameObjectsWithTag("Efectos");
        foreach (GameObject e in efectosEnEscena)
        {
            Destroy(e);
        }

        // Por si no usas Tags, podemos buscar por el nombre que Unity le da al clon
        // (Ajusta los nombres según cómo se llamen tus prefabs)
        GameObject[] todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject g in todos)
        {
            if (g.name.Contains("Slash") || g.name.Contains("BlackHole") || g.name.Contains("AgujeroNegro"))
            {
                Destroy(g);
            }
        }
    }
    private PlanetCrontrollator GetActivePlanet()
    {
        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        if (mapList != null && currentMap >= 0 && currentMap < mapList.Length && mapList[currentMap] != null)
        {
            PlanetCrontrollator p = mapList[currentMap].GetComponentInChildren<PlanetCrontrollator>(true);
            if (p != null) return p;
        }

        return Object.FindFirstObjectByType<PlanetCrontrollator>();
    }

    private void StartLoadedSession(float savedTimer, int savedMapIndex, float savedPlanetHealth)
    {
        checkParaExtraTimeRealizado = false;
        figurasCandidatas.Clear();
        timeSinceLastAutoSave = 0f;
        timerStarted = false;

        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (zonePanel != null) zonePanel.SetActive(false);
        if (shinyPanel != null) shinyPanel.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        ResetCameraZoom();
        LevelTransitioner transitioner = Object.FindFirstObjectByType<LevelTransitioner>();
        if (transitioner != null) transitioner.ResetFinalLevelEffects();
        if (extraTimeUI != null)
        {
            extraTimeUI.SetActive(false);
            RectTransform rt = extraTimeUI.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;
        }

        if (timerIcon != null)
            timerIcon.rectTransform.localScale = timerIconOriginalScale;

        if (animacionExtraTime != null) StopCoroutine(animacionExtraTime);
        if (animacionTimer != null) StopCoroutine(animacionTimer);

        animacionExtraTime = null;
        animacionTimer = null;

        timerAnimando = false;
        ResetTimerVisuals();

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) mainCamera.orthographicSize = defaultZoom;

        if (cachedPopManager == null)
            cachedPopManager = Object.FindFirstObjectByType<PopulationManager>();

        PlayerPrefs.SetInt("CurrentMapIndex", savedMapIndex);
        PlayerPrefs.Save();

        if (MapSequenceManager.instance != null)
            MapSequenceManager.instance.SetCurrentMapIndex(savedMapIndex, false);

        ActivateMap(savedMapIndex);
        SetMapsActive(true);
        currentSessionInfected = 0;
        monedasGanadasSesion = 0;

        float tiempoBase = gameDuration;
        if (Guardado.instance != null)
            tiempoBase += Guardado.instance.extraBaseTime;

        currentTimer = tiempoBase;

        if (cachedPopManager != null)
        {
            cachedPopManager.ClearAllPersonas();
            cachedPopManager.SelectPrefab(savedMapIndex);
            cachedPopManager.ConfigureRound(savedMapIndex);
        }

        PlanetCrontrollator planeta = GetActivePlanet();
        if (planeta != null)
        {
            planeta.ResetHealthToInitial();
            planeta.ClearPendingDamage();
            planeta.isInvulnerable = true;


            planeta.isInvulnerable = false;
        }

        isTransitioning = false;
        isGameActive = true;
       

        if (virusPlayer != null) virusPlayer.SetActive(true);
        if (virusMovementScript != null) virusMovementScript.enabled = true;

        if (AudioManager.instance != null)
            AudioManager.instance.SwitchToGameMusic();

        UpdateUI();
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

        if (MapSequenceManager.instance != null)
            MapSequenceManager.instance.ResetToFirstMap();

        // ---> ¡AQUÍ ES EL LUGAR PERFECTO! <---
        // Solo se limpiarán las estadísticas al iniciar una run desde cero.
        PersonaInfeccion.ResetearEstadisticas();
        PersonaInfeccion.ClearSavedStats();

        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset) ForceHardReset();
        if (Guardado.instance) Guardado.instance.ApplyPermanentInitialUpgrade();

        ContagionCoins = Guardado.instance != null ? Guardado.instance.startingCoins : 0;
        UpdateUI();
    }
    // Método para iniciar el timer manualmente (como OnFirstPhaseAdvanced)
    public void StartTimer()
    {
        if (!isGameActive) return; // Seguridad: No iniciar si no estamos en partida

        timerStarted = true;
        Debug.Log("<color=orange>[TIMER]</color> ¡Reloj activado!");

        // Opcional: Aquí podrías disparar algún sonido de "Go!" o feedback visual
    }
    public void StartSession()
    {
        timerStarted = false; // El timer siempre empieza en FALSE
        UpdateCursorState(true);
        checkParaExtraTimeRealizado = false;
        figurasCandidatas.Clear();
        timeSinceLastAutoSave = 0f;

        // --- LÓGICA DE TUTORIAL / INICIO PAUSADO ---
        if (TutorialManager.instance != null && VirusMovement.instance != null)
        {
            if (!TutorialManager.instance.HasSeenTutorial())
            {
                TutorialManager.instance.StartTutorial(VirusMovement.instance.transform);
                // timerStarted sigue siendo false aquí
            }
            else
            {
                // Incluso si ya vio el tutorial, lo dejamos en false. 
                // El timer empezará cuando infecte a la primera persona.
                timerStarted = false;
            }
        }
        else
        {
            timerStarted = false; // Sin tutorial también empieza pausado
        }

        if (menuPanel) menuPanel.SetActive(false);
        ResetCameraZoom();


        if (extraTimeUI != null)
        {
            extraTimeUI.SetActive(false);
            extraTimeUI.GetComponent<RectTransform>().localScale = Vector3.one;
        }
        if (timerIcon != null) if (timerIcon != null) timerIcon.rectTransform.localScale = timerIconOriginalScale;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (mainCamera == null) mainCamera = Camera.main;
        mainCamera.orthographicSize = defaultZoom;

        timerAnimando = false;
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

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        ActivateMap(0);
        SetMapsActive(true);

        PlanetCrontrollator planeta = GetActivePlanet();
        if (planeta != null)
        {
            planeta.ResetHealthToInitial();
            planeta.ClearPendingDamage();
        }

        isGameActive = true;
        currentSessionInfected = 0;
        monedasGanadasSesion = 0;

        PersonaInfeccion.ResetearEstadisticas();


        float tiempoTotal = gameDuration;

        if (Guardado.instance != null)
            tiempoTotal += Guardado.instance.extraBaseTime;

        currentTimer = tiempoTotal;



        // Configuramos el cursor basado SIEMPRE en la elección del usuario
        UpdateCursorState(true);

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
        if (!isGameActive || isTransitioning) return;

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
        if (mainCamera == null) mainCamera = Camera.main;
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        // 1. Efecto de Zoom y Slow Motion (Los enemigos SIGUEN AQUÍ)
        while (currentTime < slowMotionDuration)
        {
            currentTime += Time.unscaledDeltaTime;
            float t = currentTime / slowMotionDuration;
            float smoothT = t * t * (3f - 2f * t);

            Time.timeScale = Mathf.Lerp(1f, 0.05f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            if (mainCamera != null)
                mainCamera.orthographicSize = Mathf.Lerp(defaultZoom, endSessionZoom, smoothT);

            yield return null;
        }

        // 2. Iniciamos el cierre de pantalla (IRIS/NEGRO)
        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();

            // ESPERAMOS a que la animación de cierre termine (aprox 0.5s según tu código)
            // Mientras esperamos, los enemigos siguen en pantalla pero el jugador ya no los ve.
            yield return new WaitForSecondsRealtime(0.6f);
        }

        // 3. Ahora que estamos SEGUROS de que está en negro, ejecutamos la limpieza
        CompleteEndSessionLogic();
    }

    private void CompleteEndSessionLogic()
    {
        // Aquí la pantalla ya está en negro por la espera de la corrutina anterior
        UpdateCursorState(false);

        // Borrado fulminante de enemigos en la oscuridad
        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.ClearAllPersonas();
        }

        CleanUpEffectsAndUI();

        // Mostramos el panel de resultados
        StartCoroutine(ShowResultsWithTransition());
    }

    private IEnumerator ShowResultsWithTransition()
    {
        // Ya no esperamos 0.5s aquí porque ya lo hicimos antes
        Time.timeScale = 0f;
        SetMapsActive(false);
        gameUI.SetActive(false);

        int totalFinal = contagionCoins;

        if (EndDayResultsPanel.instance != null)
        {
            EndDayResultsPanel.instance.ShowResults(monedasGanadasSesion, totalFinal);
        }

        if (Guardado.instance != null)
            Guardado.instance.AddTotalData(currentSessionInfected);

        // Abrimos para mostrar el panel
        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
        yield return null;
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
        // Guardar los últimos datos antes de limpiar el estado de run
        if (Guardado.instance != null)
        {
            Guardado.instance.SaveRunState(currentTimer, contagionCoins, PlayerPrefs.GetInt("CurrentMapIndex", 0), 0f);
            Guardado.instance.SaveEvolutionData();
            Guardado.instance.SaveData();

            // También guardar estado de todos los nodos
            SkillNode[] nodes = FindObjectsOfType<SkillNode>(true);
            foreach (SkillNode node in nodes)
            {
                node.SaveNodeState();
            }

            PlayerPrefs.Save();

            // Limpiar el estado de run después de guardar
            Guardado.instance.ClearRunState();
        }
    }

    public void UpdateUI()
    {
        // Si ya hay una animación corriendo, la paramos para empezar la nueva
        if (coinAnimationCoroutine != null) StopCoroutine(coinAnimationCoroutine);
        coinAnimationCoroutine = StartCoroutine(AnimateCoins());
    }
    private IEnumerator AnimateCoins()
    {
        // Velocidad de la animación: puedes ajustar el '0.5f' 
        // Cuanto menor sea el número, más rápido llegará al destino
        float duration = 0.5f;
        float elapsed = 0f;
        float startValue = visualCoins;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled para que funcione aunque el juego esté en pausa o slow motion

            // Interpolación lineal entre el valor actual y el objetivo
            visualCoins = Mathf.Lerp(startValue, contagionCoins, elapsed / duration);

            // Actualizamos todos los textos con el valor redondeado
            string coinText = $"{GetTexto("txt_monedas_ui")}: {Mathf.FloorToInt(visualCoins)}";

            foreach (var t in contagionCoinsTexts)
            {
                if (t != null) t.text = coinText;
            }

            yield return null;
        }

        // Al terminar, nos aseguramos de que el valor sea exacto
        visualCoins = contagionCoins;
        foreach (var t in contagionCoinsTexts)
        {
            if (t != null) t.text = $"{GetTexto("txt_monedas_ui")}: {contagionCoins}";
        }
    }

    public void LostToMenu() { ResetRunData(); ShowMainMenu(); }

    void CleanUpScene()
    {
        PersonaInfeccion[] gente = Object.FindObjectsByType<PersonaInfeccion>(FindObjectsSortMode.None);
        foreach (PersonaInfeccion p in gente) if (p != null) Destroy(p.gameObject);

        // AÑADE ESTA LÍNEA para que jamás se olvide de los agujeros negros
        StopAllActiveRunEffects();
    }

    public int GetTotalUnlockedZones()
    {
        int count = 1;
        for (int i = 1; i <= 10; i++) if (PlayerPrefs.GetInt("ZoneUnlocked_" + i, 0) == 1) count++;
        return count;
    }
    private bool isSoftRestarting = false;
    public bool IsSoftRestarting => isSoftRestarting;

    public void SoftRestartRun()
    {
        if (isSoftRestarting) return;
        isSoftRestarting = true;

        // 1. Identificamos qué panel estamos cerrando (Resultados o Pausa)
        GameObject panelARecerrar = (EndDayResultsPanel.instance != null && EndDayResultsPanel.instance.panel.activeSelf)
                                    ? EndDayResultsPanel.instance.panel
                                    : pausePanel;

        // 2. Si hay monedas pendientes de animar en resultados, las transferimos primero
        if (panelARecerrar == EndDayResultsPanel.instance.panel && EndDayResultsPanel.instance.TieneMonedasPendientes)
        {
            EndDayResultsPanel.instance.StartCoinTransfer(() =>
            {
                DoPanelTransition(panelARecerrar, null, () => EjecutarLogicaCargaSoftRestart());
            });
        }
        else
        {
            // 3. Transición directa
            DoPanelTransition(panelARecerrar, null, () => EjecutarLogicaCargaSoftRestart());
        }
    }

    private void EjecutarLogicaCargaSoftRestart()
    {
        Debug.Log("<color=yellow>[SOFT RESTART]</color> Ejecutando limpieza en oscuridad total.");

        // A. Reset de variables de sesión
        monedasGanadasSesion = 0;
        currentSessionInfected = 0;

        // B. Reset de Mapas y Planetas
        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();

        // --- NUEVO: ASEGURAR QUE LOS PANELES DE TIENDA SE CIERREN ---
        if (shinyPanel != null) shinyPanel.SetActive(false); // <--- AÑADE ESTA LÍNEA
        if (zonePanel != null) zonePanel.SetActive(false);   // Por seguridad, esta también
                                                             // -----------------------------------------------------------

        if (MapSequenceManager.instance != null)
            MapSequenceManager.instance.ResetToFirstMap();

        if (Object.FindFirstObjectByType<ManualSetCycler>() != null)
            Object.FindFirstObjectByType<ManualSetCycler>().ResetCycler();

        // Resetear posiciones y estados de todos los mapas
        for (int i = 0; i < mapList.Length; i++)
        {
            if (mapList[i] != null)
            {
                mapList[i].transform.rotation = Quaternion.identity;
                mapList[i].SetActive(i == 0); // Solo activamos el primero
            }
        }

        // Resetear salud de todos los planetas
        PlanetCrontrollator[] planetas = Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in planetas) p.ResetHealthToInitial();

        // C. Limpieza de enemigos y efectos
        CleanUpScene();

        // D. Iniciar la nueva sesión
        StartSession();

        isSoftRestarting = false;
    }

    private IEnumerator SoftRestartTransitionRoutine()
    {
        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        if (shinyPanel != null) shinyPanel.SetActive(false);

        if (EndDayResultsPanel.instance != null &&
            EndDayResultsPanel.instance.panel != null &&
            EndDayResultsPanel.instance.panel.activeSelf)
        {
            EndDayResultsPanel.instance.panel.SetActive(false);
        }

        if (gameUI) gameUI.SetActive(false);

        Debug.Log("Soft Restart ejecutado bajo transición.");

        monedasGanadasSesion = 0;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();

        if (MapSequenceManager.instance != null)
            MapSequenceManager.instance.ResetToFirstMap();

        ManualSetCycler cycler = Object.FindFirstObjectByType<ManualSetCycler>();
        if (cycler != null)
            cycler.ResetCycler();

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

        PlanetCrontrollator[] todosLosPlanetas =
            Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlanetCrontrollator planet in todosLosPlanetas)
        {
            if (planet != null)
                planet.ResetHealthToInitial();
        }

        isGameActive = false;

        CleanUpScene();

        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.ClearAllPersonas();
            PopulationManager.instance.SelectPrefab(0);
        }

        UpdateUI();

        yield return null;
        yield return new WaitForSecondsRealtime(0.05f);

        StartSession();

        if (transitionScript != null)
            transitionScript.OpenBlackScreen();

        isSoftRestarting = false;
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

        if (estaPausado) // Vamos a reanudar
        {
            pausePanel.SetActive(false);
            UpdateCursorState(true); // <-- CAMBIAR A TRUE
            Time.timeScale = 1f;
            if (virusMovementScript != null) virusMovementScript.enabled = true;

            // Limpiamos la selección al quitar la pausa por seguridad
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
        else // Vamos a pausar
        {
            pausePanel.SetActive(true);
            UpdateCursorState(false); // <-- AQUÍ SE QUEDA EN FALSE
            Time.timeScale = 0f;
            if (virusMovementScript != null) virusMovementScript.enabled = false;

            // 🎮 MAGIA PARA EL MANDO: Seleccionar el botón automáticamente
            if (pauseFirstSelectedButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(pauseFirstSelectedButton);
            }
        }
    }
    // --- AÑADIR ESTO A TU LevelManager.cs ---

    // --- AÑADE O MODIFICA ESTO EN LevelManager.cs ---

    public void NextMapTransition()
    {
        if (!isGameActive) return;

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int nextMap = currentMap + 1;

        // 🛑 EL MURO DE LA DEMO (¡Puesto aquí, antes de cualquier transición!) 🛑
        if (esVersionDemo && nextMap > 0)
        {
            MostrarFinDeDemo();
            return; // Cortamos en seco, no hay transición que valga
        }

        // Buscamos el script de transición en la escena
        LevelTransitioner transitioner = Object.FindFirstObjectByType<LevelTransitioner>();

        if (transitioner != null)
        {
            // Si existe el script de las rotaciones, que él tome el control
            transitioner.StartLevelTransition();
        }
        else
        {
            // Si por alguna razón no está el script de giro, hace el cambio normal
            StartCoroutine(WaitAndChangeMap());
        }
    }

    private IEnumerator WaitAndChangeMap()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int nextMap = currentMap + 1;

        // 🛑 EL MURO DE LA DEMO 🛑
        // Si es la versión demo, y vamos a pasar a un mapa mayor que el 0 (Hexágono)...
        if (esVersionDemo && nextMap > 0)
        {
            MostrarFinDeDemo(); // Lanzamos el final épico
            yield break; // Cortamos la corrutina aquí mismo para que no cargue el siguiente mapa
        }

        // Si NO es demo, o seguimos en un mapa permitido, el juego continúa normal
        if (nextMap < mapList.Length)
        {
            ActivateMap(nextMap);
            yield return new WaitForEndOfFrame();

            PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
            if (pm != null)
            {
                pm.RefreshSpawnArea();
            }
        }
    }

    public void MostrarPuntosVoladores(Vector3 posicionPersona, int puntosGanados)
    {
        // No mostramos los números voladores si estamos en el panel de resultados
        if (EndDayResultsPanel.instance != null && EndDayResultsPanel.instance.panel != null && EndDayResultsPanel.instance.panel.activeSelf)
            return;

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
        System.Action logic = () => {
            RefreshSkillTreeVisualOnly();
            SkillTreeLinesUI lines = FindFirstObjectByType<SkillTreeLinesUI>();
            if (lines != null) { lines.ResetAllLinesVisuals(); lines.RefreshAllLinesFromNodes(); }
        };

        GameObject origin = (EndDayResultsPanel.instance.panel.activeSelf) ? EndDayResultsPanel.instance.panel : zonePanel;
        DoPanelTransition(origin, shinyPanel, logic);
    }

    private IEnumerator TransitionToSkillTree()
    {
        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        if (EndDayResultsPanel.instance != null)
            EndDayResultsPanel.instance.panel.SetActive(false);

        if (zonePanel != null) zonePanel.SetActive(false);

        if (shinyPanel != null)
        {
            shinyPanel.SetActive(true);
            UpdateUI();
        }
        RefreshSkillTreeVisualOnly();

        SkillTreeLinesUI lines = FindFirstObjectByType<SkillTreeLinesUI>();
        if (lines != null)
        {
            lines.ResetAllLinesVisuals();
            lines.RefreshAllLinesFromNodes();
        }

        yield return new WaitForSecondsRealtime(0.1f);

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
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

        float planetHealth = 0f;
        PlanetCrontrollator planet = FindFirstObjectByType<PlanetCrontrollator>();
        if (planet != null)
        {
            planetHealth = planet.GetCurrentHealth();
        }

        Guardado.instance.SaveRunState(
            currentTimer,
            contagionCoins,
            currentMap,
            planetHealth
        );
        Guardado.instance.SaveEvolutionData();
        Guardado.instance.SaveData();

        PersonaInfeccion.SaveStats();

        SkillNode[] nodes = FindObjectsOfType<SkillNode>(true);
        foreach (SkillNode node in nodes)
        {
            node.SaveNodeState();
        }

        PlayerPrefs.Save();

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
    public void RefreshSkillTreeVisualOnly()
    {
        var nodes = FindObjectsOfType<SkillNode>(true);

        foreach (var node in nodes)
            node.gameObject.SetActive(true);

        foreach (var node in nodes)
            node.CheckIfShouldShow();
    }

    // ========== GUARDADO AL CERRAR LA APLICACIÓN ==========
    void OnApplicationQuit()
    {
        // Si hay una partida activa, guardar todas las monedas y datos antes de cerrar
        if (isGameActive && Guardado.instance != null)
        {
            SaveCurrentRun();
            Debug.Log($"<color=yellow>[QUIT-SAVE]</color> Partida guardada antes de cerrar. Monedas: {contagionCoins}");
        }
    }

    private void CleanUpEffectsAndUI()
    {
        BlackHoleController hole = Object.FindFirstObjectByType<BlackHoleController>();
        if (hole != null)
        {
            hole.ClearActiveEffects();
        }
        // 1. Limpiar Sistemas de Partículas y Tajos (Físicos)
        GameObject[] efectos = GameObject.FindGameObjectsWithTag("Efectos");
        foreach (GameObject efecto in efectos)
        {
            if (efecto != null) Destroy(efecto);
        }

        // 2. Limpiar Textos Flotantes (Sistema de Pool)
        // Buscamos todos los objetos que tengan el script FloatingText, 
        // incluyendo los que estén activos en ese momento.
        FloatingText[] textosEnPantalla = Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None);

        foreach (FloatingText texto in textosEnPantalla)
        {
            if (texto != null && texto.gameObject.activeSelf)
            {
                // IMPORTANTE: No usamos Destroy porque romperíamos el Pool.
                // Simplemente lo devolvemos al estado "apagado".
                texto.gameObject.SetActive(false);
            }
        }

        Debug.Log("<color=cyan>[CLEANUP]</color> Textos y efectos limpiados correctamente.");
    }
    public void UpdateCursorState(bool isPlaying)
    {
        // Si estamos en un menú (isPlaying = false), el cursor siempre debe verse
        if (!isPlaying)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        // Si estamos jugando, dependemos del tipo de entrada
        if (Guardado.instance != null)
        {
            if (Guardado.instance.inputType == Guardado.InputType.Mouse)
            {
                Cursor.visible = true;
            }
            else
            {
                Cursor.visible = false;
            }
        }

        Cursor.lockState = CursorLockMode.None;
    }

    // =========================================================
    // ⚙️ FUNCIONES DEL MENÚ DE AJUSTES ⚙️
    // =========================================================

    // =========================================================
    // ⚙️ AJUSTES CON SINCRONIZACIÓN MILIMÉTRICA ⚙️
    // =========================================================

    public void OpenSettingsPanel()
    {
        // Si estamos en juego, pausamos el tiempo antes
        if (isGameActive) { Time.timeScale = 0f; virusMovementScript.enabled = false; }

        GameObject origin = (pausePanel.activeSelf) ? pausePanel : menuPanel;
        DoPanelTransition(origin, settingsPanel, null, settingsFirstSelectedButton);
    }

    public void CloseSettingsPanel()
    {
        if (isGameActive)
            DoPanelTransition(settingsPanel, pausePanel, null, pauseFirstSelectedButton);
        else
            DoPanelTransition(settingsPanel, menuPanel);
    }

    // =========================================================
    // 🛑 LÓGICA DE LA DEMO 🛑
    // =========================================================
    // =========================================================
    // 🛑 LÓGICA DE LA DEMO 🛑
    // =========================================================
    public void MostrarFinDeDemo()
    {
        // 1. Apagamos el estado del juego y el tiempo
        isGameActive = false;
        Time.timeScale = 0f;

        // 2. 🧽 LIMPIEZA TOTAL: Hacemos desaparecer al jugador y los controles
        if (virusPlayer != null) virusPlayer.SetActive(false);
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        // 3. 🧽 LIMPIEZA TOTAL: Borramos todos los enemigos de la pantalla
        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.ClearAllPersonas();
        }

        // 4. 🧽 LIMPIEZA TOTAL: Borramos tajos, agujeros negros, etc.
        StopAllActiveRunEffects();

        // 5. Ocultamos la interfaz normal
        if (gameUI != null) gameUI.SetActive(false);
        if (zonePanel != null) zonePanel.SetActive(false);
        if (shinyPanel != null) shinyPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // 6. ¡Mostramos el panel épico de Fin de Demo!
        if (panelFinDemo != null) panelFinDemo.SetActive(true);

        // 7. 🎮 Magia para el Mando
        if (!MenuGamepadNavigator.usandoRaton && panelFinDemo != null)
        {
            Button primerBotonDemo = panelFinDemo.GetComponentInChildren<Button>();
            if (primerBotonDemo != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(primerBotonDemo.gameObject);
            }
        }
        else if (MenuGamepadNavigator.usandoRaton && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        UpdateCursorState(false);
    }

// =========================================================
// 🌀 SISTEMA DE TRANSICIÓN UNIFICADO (HEXÁGONO)
// =========================================================

    /// <param name="panelToClose">Panel que se apaga al cerrarse el hexágono.</param>
    /// <param name="panelToOpen">Panel que se enciende antes de abrirse el hexágono.</param>
    /// <param name="actionBeforeOpen">Acción extra (ej. StartSession) a ejecutar en la oscuridad total.</param>
    /// <param name="firstSelectable">Botón para resaltar con el mando al abrir el nuevo panel.</param>
public void DoPanelTransition(GameObject panelToClose, GameObject panelToOpen, System.Action actionBeforeOpen = null, GameObject firstSelectable = null)
    {
        StartCoroutine(UniversalTransitionRoutine(panelToClose, panelToOpen, actionBeforeOpen, firstSelectable));
    }

    private IEnumerator UniversalTransitionRoutine(GameObject panelToClose, GameObject panelToOpen, System.Action actionBeforeOpen, GameObject firstSelectable)
    {
        if (transitionScript != null)
        {
            // 1. Configurar Hexágono y cerrar
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();

            // Esperamos a que la animación de cierre termine (0.55s es lo que tarda el shader)
            yield return new WaitForSecondsRealtime(0.55f);

            // --- NUEVO: TIEMPO EXTRA EN NEGRO ---
            // Aquí la pantalla ya está 100% negra. Se quedará así el tiempo que pongas en 'tiempoEsperaEnNegro'
            if (tiempoEsperaEnNegro > 0)
            {
                yield return new WaitForSecondsRealtime(tiempoEsperaEnNegro);
            }
        }

        // --- 🌑 ESTAMOS EN OSCURIDAD TOTAL (Y ya hemos esperado el tiempo extra) 🌑 ---

        // 2. Intercambio de Paneles
        if (panelToClose != null) panelToClose.SetActive(false);
        if (panelToOpen != null) panelToOpen.SetActive(true);

        // 3. Ejecutar lógica adicional
        actionBeforeOpen?.Invoke();

        // 4. Configurar selección de Mando
        if (firstSelectable != null && !MenuGamepadNavigator.usandoRaton)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectable);
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        yield return new WaitForEndOfFrame();

        // 5. Abrir Transición
        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
    }

    public void CerrarJuego()
    {
        // Esto cierra la aplicación una vez exportada (Build)
        Application.Quit();

        // Esto detiene el modo "Play" dentro del editor de Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        Debug.Log("El juego se ha cerrado");
    }
}
