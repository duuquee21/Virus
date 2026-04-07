using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public GameObject panelFinal;

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
    public GameObject shinyPanel;
    public GameObject zonePanel;
    public GameObject pausePanel;
    public GameObject pauseFirstSelectedButton;
    public GameObject settingsPanel;
    public GameObject settingsFirstSelectedButton;

    [Header("UI Text (Listas)")]
    public List<TextMeshProUGUI> timerTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> sessionScoreTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> contagionCoinsTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> shinyStoreTexts = new List<TextMeshProUGUI>();

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int monedasGanadasSesion;

    [Header("Configuración de Demo")]
    public bool esVersionDemo = false;
    public GameObject panelFinDemo;
    public RectTransform logoDemo; // 🌟 NUEVO: El logo que va a saltar
    public string urlSteam = "https://store.steampowered.com/app/4550150/Get_Rid_Of_Those_Corners?beta=0"; // 🌟 NUEVO: Tu enlace de Steam
    public string urlDiscord = "https://discord.gg/guwArTV7"; // 🌟 NUEVO: Tu enlace de Steam

    [Header("Configuración Inicial por Zona")]
    public int[] faseInicialPorMapa;

    [Header("Configuración Visual por Zona")]
    public Color[] coloresPorMapa;

    [HideInInspector] public bool isGameActive;
    [HideInInspector] public bool isTransitioning;
    [HideInInspector] public int currentSessionInfected;

    float currentTimer;
    public bool timerStarted = false;

    [Header("Transición")]
    public Collections.Shaders.ShapeTransition.ShapeTransition transitionScript;
    public float tiempoEsperaEnNegro = 0.2f;

    [Header("Extra Time Settings")]
    public GameObject extraTimeUI;
    private List<PersonaInfeccion> figurasCandidatas = new List<PersonaInfeccion>();
    private bool checkParaExtraTimeRealizado = false;

    [Header("Configuración de Cámara y Final")]
    public Camera mainCamera;
    public float defaultZoom = 14f;
    public float endSessionZoom = 25f;
    public float slowMotionDuration = 1.5f;

    private Coroutine animacionExtraTime;
    private Coroutine animacionTimer;
    private bool timerAnimando = false;

    public Image timerIcon;
    private Vector3 timerIconOriginalScale;

    private float autoSaveInterval = 10f;
    private float timeSinceLastAutoSave = 0f;

    private int lastActiveMapIndex = -1;
    private PopulationManager cachedPopManager;

    private float visualCoins;
    private Coroutine coinAnimationCoroutine;

    public bool IsTimeUp => currentTimer <= 0;
    private bool isSoftRestarting = false;
    public bool IsSoftRestarting => isSoftRestarting;

    private Queue<float> infectionTimestamps = new Queue<float>();

    private bool[] figuresCaughtInRun = new bool[5];

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); return; }

        cachedPopManager = Object.FindFirstObjectByType<PopulationManager>();
    }

    public void BloquearControlesUI()
    {
        if (EventSystem.current != null)
            EventSystem.current.enabled = false;
    }

    public void DesbloquearControlesUI()
    {
        if (EventSystem.current != null)
            EventSystem.current.enabled = true;
    }

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

        float volumenGuardado = PlayerPrefs.GetFloat("MasterVolume", PlayerPrefs.GetFloat("VolumenGlobal", 1f));
        AudioListener.volume = volumenGuardado;

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        if (Guardado.instance != null && !Guardado.instance.HasSavedGame())
        {
            ForceHardReset();
        }

        ShowMainMenu();

        if (AudioManager.instance != null)
            AudioManager.instance.SwitchToMenuMusic();

        if (timerIcon != null)
        {
            timerIconOriginalScale = timerIcon.rectTransform.localScale;
        }

        visualCoins = contagionCoins;
    }

    // 🛡️ ESCUDOS AÑADIDOS A LOS BOTONES 🛡️
    public void OpenShinyShop() { if (isTransitioning) return; if (shinyPanel != null) { shinyPanel.SetActive(true); UpdateUI(); } }

    public void CloseShinyShop()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionBackFromSkillTree());
    }

    private IEnumerator TransitionBackFromSkillTree()
    {
        isTransitioning = true; // Bloqueo anti-spam
        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        if (shinyPanel != null) shinyPanel.SetActive(false);
        if (zonePanel != null) zonePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(0.1f);

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
        }
        isTransitioning = false; // Desbloqueo
    }

    public void OpenZoneShop() { if (isTransitioning) return; if (zonePanel != null) { zonePanel.SetActive(true); UpdateUI(); } }
    public void CloseZoneShop() { if (isTransitioning) return; if (zonePanel != null) zonePanel.SetActive(false); }

    public void AddCoins(int amount)
    {
        ContagionCoins += amount;
        monedasGanadasSesion += amount;
        UpdateUI();
    }

    public int ContagionCoins
    {
        get => contagionCoins;
        set
        {
            contagionCoins = value;
            UpdateUI();
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

        // --- LÓGICA DE LOGROS DE VELOCIDAD ---
        float currentTime = Time.time;
        infectionTimestamps.Enqueue(currentTime);

        // Mantenemos solo las últimas 10 infecciones en la lista
        while (infectionTimestamps.Count > 10)
        {
            infectionTimestamps.Dequeue();
        }

        // Si tenemos exactamente 10, comparamos el tiempo de la primera con la última
        if (infectionTimestamps.Count == 10)
        {
            float timeElapsed = currentTime - infectionTimestamps.Peek();

            if (timeElapsed <= 5f)
            {
                SteamManagerCustom.Instance.UnlockAchievement("ACH_10IN5");
            }

            if (timeElapsed <= 10f)
            {
                SteamManagerCustom.Instance.UnlockAchievement("ACH_10IN10");
            }
        }
        // ---------------------------------------

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

        colorFinal.a = 1f;
        return colorFinal;
    }

    void ShowMainMenu()
    {
        gameUI.SetActive(false);
        SetMapsActive(false);
        UpdateCursorState(false);

        if (pausePanel) pausePanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);
        if (shinyPanel) shinyPanel.SetActive(false);
        if (panelFinal) panelFinal.SetActive(false);

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
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        if (Guardado.instance) Guardado.instance.ResetAllProgress();

        SteamManagerCustom.Instance.UnlockAchievement("ACH_NEWGAME_TOTAL");

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
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam
        LoadRunAndStart();
    }

    void LoadRunAndStart()
    {
        StartCoroutine(LoadRunRoutine());
    }

    private IEnumerator LoadRunRoutine()
    {
        System.Action cargaEnLaOscuridad = () =>
        {
            ResetSceneToNeutralState();

            float savedTimer = PlayerPrefs.GetFloat("Run_Timer", gameDuration);
            int savedCoins = PlayerPrefs.GetInt("Run_Coins", 0);
            int savedMapIndex = PlayerPrefs.GetInt("Run_MapIndex", 0);
            float savedPlanetHealth = PlayerPrefs.GetFloat("Run_PlanetHealth", -1f);

            ContagionCoins = savedCoins;

            if (Guardado.instance != null)
                Guardado.instance.LoadEvolutionData();

            PersonaInfeccion.LoadStats();

            RebuildSkillTree();
            SyncControllersWithSavedData();
            Movement.espacialGrid.Clear();

            StartLoadedSession(savedTimer, savedMapIndex, savedPlanetHealth);

            Debug.Log("<color=green>[LOAD]</color> Partida recuperada con éxito en la oscuridad.");
        };

        DoPanelTransition(menuPanel, null, cargaEnLaOscuridad);
        yield break;
    }

    public void NewGameFromMainMenu()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.HardResetPool();
        }

        ResetRunData();

        GameObject panelToClose = (settingsPanel != null && settingsPanel.activeSelf) ? settingsPanel : menuPanel;

        // LIMPIEZA CLAVE AQUÍ
    

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

    void SyncControllersWithSavedData()
    {
        if (Guardado.instance == null) return;

        if (SpeedUpgradeController.instance != null)
        {
            int speedLevel = Guardado.instance.speedLevel;
            SpeedUpgradeController.instance.SetLevel(speedLevel);
        }

        if (InfectionSpeedUpgradeController.instance != null)
        {
            int infectionSpeedLevel = Guardado.instance.infectionSpeedLevel;
            InfectionSpeedUpgradeController.instance.SetLevel(infectionSpeedLevel);
        }

        if (CapacityUpgradeController.instance != null)
        {
            int capacityLevel = Guardado.instance.capacityLevel;
            CapacityUpgradeController.instance.SetLevel(capacityLevel);
        }

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

        bool enMenuUI =
           (pausePanel != null && pausePanel.activeSelf) ||
           (settingsPanel != null && settingsPanel.activeSelf) ||
           (shinyPanel != null && shinyPanel.activeSelf) ||
           (zonePanel != null && zonePanel.activeSelf) ||
           (panelFinDemo != null && panelFinDemo.activeSelf) ||
           (panelFinal != null && panelFinal.activeSelf);

        if (isGameActive && !enMenuUI)
        {
            if (Guardado.instance != null)
            {
                if (Guardado.instance.inputType == Guardado.InputType.Keyboard ||
                    Guardado.instance.inputType == Guardado.InputType.Controller)
                {
                    if (Cursor.visible) Cursor.visible = false;
                }
                else
                {
                    if (!Cursor.visible) Cursor.visible = true;
                }
            }

            timeSinceLastAutoSave += Time.deltaTime;
            if (timeSinceLastAutoSave >= autoSaveInterval)
            {
                SaveCurrentRun();
                timeSinceLastAutoSave = 0f;
            }
        }
        else
        {
            if (!Cursor.visible) Cursor.visible = true;
        }
    }


    private IEnumerator AnimarRespiracionTimer()
    {
        while (timerAnimando)
        {
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
                if (animacionExtraTime != null) StopCoroutine(animacionExtraTime);
                animacionExtraTime = StartCoroutine(AnimarTextoExtraTime());
            }
        }
    }

    private IEnumerator AnimarTextoExtraTime()
    {
        RectTransform rect = extraTimeUI.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;

        float appearanceDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < appearanceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / appearanceDuration;
            float appearanceScale = Mathf.SmoothStep(0f, 1.0f, progress);
            float currentPulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f;
            float finalScale = appearanceScale * currentPulse;

            rect.localScale = new Vector3(finalScale, finalScale, 1f);
            yield return null;
        }

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
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        if (isGameActive && Guardado.instance != null)
        {
            SaveCurrentRun();
        }

        System.Action logicEnLaOscuridad = () => {
            if (AudioManager.instance != null)
                AudioManager.instance.SwitchToMenuMusic();

            SkillNode.ClearRuntimeState();
            ResetSceneToNeutralState();
            ShowMainMenu();

            if (panelFinal != null) panelFinal.SetActive(false);
            if (EndDayResultsPanel.instance != null && EndDayResultsPanel.instance.panel != null)
                EndDayResultsPanel.instance.panel.SetActive(false);

            // 🛑 ARREGLO: APAGAMOS EL PANEL DE LA DEMO PARA QUE NO ESTORBE
            if (panelFinDemo != null) panelFinDemo.SetActive(false);
        };

        // 🛑 ARREGLO: LE ENSEÑAMOS A LA TRANSICIÓN CUÁL ES EL PANEL QUE TIENE QUE CERRAR
        GameObject panelActual = gameUI;
        if (pausePanel != null && pausePanel.activeSelf) panelActual = pausePanel;
        else if (panelFinal != null && panelFinal.activeSelf) panelActual = panelFinal;
        else if (panelFinDemo != null && panelFinDemo.activeSelf) panelActual = panelFinDemo; // <- AQUÍ ESTÁ LA MAGIA

        DoPanelTransition(panelActual, menuPanel, logicEnLaOscuridad);
    }

    public void ActivateMap(int zoneID)
    {
        if (esVersionDemo && zoneID > 0)
        {
            MostrarFinDeDemo();
            return;
        }

        PlayerPrefs.SetInt("CurrentMapIndex", zoneID);

        if (cachedPopManager != null)
        {
            cachedPopManager.ClearAllPersonas();
            cachedPopManager.SelectPrefab(zoneID);
        }

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
        BlackSwordSpawner sword = Object.FindFirstObjectByType<BlackSwordSpawner>(FindObjectsInactive.Include);
        if (sword != null) sword.StopAllCoroutines();

        BlackHoleController hole = Object.FindFirstObjectByType<BlackHoleController>(FindObjectsInactive.Include);
        if (hole != null)
        {
            hole.StopAllCoroutines();
            hole.ClearActiveEffects();
        }

        GameObject[] efectosEnEscena = GameObject.FindGameObjectsWithTag("Efectos");
        foreach (GameObject e in efectosEnEscena)
        {
            Destroy(e);
        }

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
        PopulationManager.instance.HardResetPool();

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
        if (virusPlayer != null)
        {
            virusPlayer.SetActive(true);
            // 🌟 NUEVO: Resetear posición al centro
            virusPlayer.transform.position = Vector3.zero;
        }
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

        PersonaInfeccion.ResetearEstadisticas();
        PersonaInfeccion.ClearSavedStats();

        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset) ForceHardReset();
        if (Guardado.instance) Guardado.instance.ApplyPermanentInitialUpgrade();

        ContagionCoins = Guardado.instance != null ? Guardado.instance.startingCoins : 0;
        UpdateUI();
    }

    public void StartTimer()
    {
        if (!isGameActive) return;

        timerStarted = true;
        Debug.Log("<color=orange>[TIMER]</color> ¡Reloj activado!");
    }

    public void StartSession()
    {
        timerStarted = false;
        UpdateCursorState(true);
        checkParaExtraTimeRealizado = false;
        figurasCandidatas.Clear();
        timeSinceLastAutoSave = 0f;
        PopulationManager.instance.HardResetPool();

        if (TutorialManager.instance != null && VirusMovement.instance != null)
        {
            if (!TutorialManager.instance.HasSeenTutorial())
            {
                TutorialManager.instance.StartTutorial(VirusMovement.instance.transform);
            }
            else
            {
                timerStarted = false;
            }
        }
        else
        {
            timerStarted = false;
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

        foreach (var t in timerTexts) if (t != null) t.rectTransform.localScale = Vector3.one;

        checkParaExtraTimeRealizado = false;

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

        UpdateCursorState(true);

        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(0);

        gameUI.SetActive(true);
        virusPlayer.SetActive(true);

        // 🌟 NUEVO: Resetear posición al centro
        virusPlayer.transform.position = Vector3.zero;
        if (virusMovementScript != null) virusMovementScript.enabled = true;
        UpdateUI();
    }

    public void StartSessionWithTransition()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam
        StartCoroutine(TransitionToSession());
    }

    private IEnumerator TransitionToSession()
    {
        isTransitioning = true; // 🛡️ Bloqueo activado
        if (transitionScript != null)
        {
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        StartSession();

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f); // Tiempo de apertura
        }
        isTransitioning = false; // 🛡️ Bloqueo desactivado
    }

    public void ResumeSession()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

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
        isTransitioning = true; // 🛡️ Bloqueo durante la salida lenta
        float currentTime = 0f;
        if (mainCamera == null) mainCamera = Camera.main;
        if (virusMovementScript != null) virusMovementScript.enabled = false;

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

        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.6f);
        }

        CompleteEndSessionLogic();
    }

    private void CompleteEndSessionLogic()
    {
        UpdateCursorState(false);

        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.ClearAllPersonas();
        }

        CleanUpEffectsAndUI();
        StartCoroutine(ShowResultsWithTransition());
    }

    private IEnumerator ShowResultsWithTransition()
    {
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

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }
        isTransitioning = false; // 🛡️ Se acaba la transición de muerte, soltamos el bloqueo
    }

    public void OnEndDayResultsFinished(int earnings, int dummy)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        virusPlayer.SetActive(false);

        if (AudioManager.instance != null)
            AudioManager.instance.SwitchToMenuMusic();

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        UpdateUI();
    }

    public void GameOver()
    {
        if (Guardado.instance != null)
        {
            Guardado.instance.SaveRunState(currentTimer, contagionCoins, PlayerPrefs.GetInt("CurrentMapIndex", 0), 0f);
            Guardado.instance.SaveEvolutionData();
            Guardado.instance.SaveData();

            SkillNode[] nodes = FindObjectsOfType<SkillNode>(true);
            foreach (SkillNode node in nodes)
            {
                node.SaveNodeState();
            }

            PlayerPrefs.Save();
            Guardado.instance.ClearRunState();
        }
    }

    public void UpdateUI()
    {
        if (coinAnimationCoroutine != null) StopCoroutine(coinAnimationCoroutine);
        coinAnimationCoroutine = StartCoroutine(AnimateCoins());
    }

    private IEnumerator AnimateCoins()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        float startValue = visualCoins;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            visualCoins = Mathf.Lerp(startValue, contagionCoins, elapsed / duration);
            string coinText = $"{GetTexto("txt_monedas_ui")}: {Mathf.FloorToInt(visualCoins)}";

            foreach (var t in contagionCoinsTexts)
            {
                if (t != null) t.text = coinText;
            }
            yield return null;
        }

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

        StopAllActiveRunEffects();
    }

    public int GetTotalUnlockedZones()
    {
        int count = 1;
        for (int i = 1; i <= 10; i++) if (PlayerPrefs.GetInt("ZoneUnlocked_" + i, 0) == 1) count++;
        return count;
    }

    public void SoftRestartRun()
    {
        if (isTransitioning || isSoftRestarting) return; // 🛡️ Bloqueo anti-spam

        isSoftRestarting = true;

        GameObject panelARecerrar = (EndDayResultsPanel.instance != null && EndDayResultsPanel.instance.panel.activeSelf)
                                    ? EndDayResultsPanel.instance.panel
                                    : pausePanel;

        if (panelARecerrar == EndDayResultsPanel.instance.panel && EndDayResultsPanel.instance.TieneMonedasPendientes)
        {
            EndDayResultsPanel.instance.StartCoinTransfer(() =>
            {
                DoPanelTransition(panelARecerrar, null, () => EjecutarLogicaCargaSoftRestart());
            });
        }
        else
        {
            DoPanelTransition(panelARecerrar, null, () => EjecutarLogicaCargaSoftRestart());
        }
    }

    private void EjecutarLogicaCargaSoftRestart()
    {
        Debug.Log("<color=yellow>[SOFT RESTART]</color> Ejecutando limpieza en oscuridad total.");

        monedasGanadasSesion = 0;
        currentSessionInfected = 0;

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();

        if (shinyPanel != null) shinyPanel.SetActive(false);
        if (zonePanel != null) zonePanel.SetActive(false);

        if (MapSequenceManager.instance != null)
            MapSequenceManager.instance.ResetToFirstMap();

        if (Object.FindFirstObjectByType<ManualSetCycler>() != null)
            Object.FindFirstObjectByType<ManualSetCycler>().ResetCycler();

        for (int i = 0; i < mapList.Length; i++)
        {
            if (mapList[i] != null)
            {
                mapList[i].transform.rotation = Quaternion.identity;
                mapList[i].SetActive(i == 0);
            }
        }

        PlanetCrontrollator[] planetas = Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in planetas) p.ResetHealthToInitial();

        CleanUpScene();
        StartSession();

        isSoftRestarting = false;
    }

    private IEnumerator SoftRestartTransitionRoutine()
    {
        isTransitioning = true; // 🛡️ Bloqueo
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
        {
            transitionScript.OpenBlackScreen();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        isSoftRestarting = false;
        isTransitioning = false; // 🛡️ Desbloqueo
    }

    private void EjecutarSoftRestartLogica()
    {
        Debug.Log("Soft Restart ejecutado: Reiniciando planetas y rotaciones.");

        monedasGanadasSesion = 0;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();

        ManualSetCycler cycler = Object.FindFirstObjectByType<ManualSetCycler>();
        if (cycler != null)
        {
            cycler.ResetCycler();
        }

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

        PlanetCrontrollator[] todosLosPlanetas = Object.FindObjectsByType<PlanetCrontrollator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlanetCrontrollator planet in todosLosPlanetas)
        {
            if (planet != null)
            {
                planet.ResetHealthToInitial();
            }
        }

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
        yield return null;
        StartSession();
    }

    public void TogglePause()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        if (pausePanel == null) return;
        bool estaPausado = pausePanel.activeSelf;

        if (estaPausado)
        {
            pausePanel.SetActive(false);
            UpdateCursorState(true);
            Time.timeScale = 1f;
            if (virusMovementScript != null) virusMovementScript.enabled = true;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
        else
        {
            pausePanel.SetActive(true);
            UpdateCursorState(false);
            Time.timeScale = 0f;
            if (virusMovementScript != null) virusMovementScript.enabled = false;

            if (pauseFirstSelectedButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(pauseFirstSelectedButton);
            }
        }
    }

    public void NextMapTransition()
    {
        if (!isGameActive || isTransitioning) return; // 🛡️ Bloqueo anti-spam

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int nextMap = currentMap + 1;

        // 🛑 EL MURO DE LA DEMO 🛑
        // Lo ponemos AQUÍ ARRIBA, ¡antes de que el juego intente hacer el pantallazo blanco o girar la cámara!
        if (esVersionDemo && nextMap > 0)
        {
            MostrarFinDeDemo();
            return; // Cortamos en seco. El pantallazo blanco nunca llegará a ocurrir.
        }

        LevelTransitioner transitioner = Object.FindFirstObjectByType<LevelTransitioner>();

        if (transitioner != null)
        {
            transitioner.StartLevelTransition();
        }
        else
        {
            StartCoroutine(WaitAndChangeMap());
        }
    }
    private IEnumerator WaitAndChangeMap()
    {
        isTransitioning = true; // 🛡️ Bloqueo activado
        yield return new WaitForSecondsRealtime(0.5f);

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int nextMap = currentMap + 1;

        if (esVersionDemo && nextMap > 0)
        {
            MostrarFinDeDemo();
            // ❌ HEMOS BORRADO EL DESBLOQUEO AQUÍ. La transición de la demo se encarga ahora.
            yield break;
        }

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
        isTransitioning = false; // 🛡️ Desbloqueo normal
    }

    public void MostrarPuntosVoladores(Vector3 posicionPersona, int puntosGanados)
    {
        // 🛑 EVITAMOS que salgan números si estamos en los resultados del día...
        if (EndDayResultsPanel.instance != null && EndDayResultsPanel.instance.panel != null && EndDayResultsPanel.instance.panel.activeSelf)
            return;

        // 🛑 ... ¡O si estamos en la pantalla de Fin de Demo!
        if (panelFinDemo != null && panelFinDemo.activeSelf)
            return;

        AddCoins(puntosGanados); // Sumamos el dinero

        if (prefabTextoPuntos == null || canvasPrincipal == null || marcadorDestinoUI == null)
        {
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
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        System.Action logic = () => {
            RefreshSkillTreeVisualOnly();
            SkillTreeLinesUI lines = FindFirstObjectByType<SkillTreeLinesUI>();
            if (lines != null) { lines.ResetAllLinesVisuals(); lines.RefreshAllLinesFromNodes(); }
        };

        GameObject origin = (EndDayResultsPanel.instance.panel.activeSelf) ? EndDayResultsPanel.instance.panel : zonePanel;
        DoPanelTransition(origin, shinyPanel, logic);
    }

    private void EjecutarAbrirSkillTree()
    {
        Debug.Log("BOTON PULSADO - Abriendo Skill Tree");
        if (menuPanel) menuPanel.SetActive(false);
        if (gameUI) gameUI.SetActive(false);

        if (pausePanel) pausePanel.SetActive(false);
        if (shinyPanel) zonePanel.SetActive(false);

        if (zonePanel) shinyPanel.SetActive(true);

        RebuildSkillTree();

        Time.timeScale = 1f;
        foreach (var node in FindObjectsOfType<SkillNode>())
        {
            node.CheckIfShouldShow();
        }
    }

    public void RebuildSkillTree()
    {
        var nodes = FindObjectsOfType<SkillNode>();

        foreach (var node in nodes)
            node.LoadNodeState();

        foreach (var node in nodes)
            node.gameObject.SetActive(true);

        foreach (var node in nodes)
            node.CheckIfShouldShow();
    }

    public void ContinueCurrentMap()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

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

    public void ChangePanelWithTransition(GameObject panelToClose, GameObject panelToOpen)
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam
        DoPanelTransition(panelToClose, panelToOpen);
    }

    private void SetMapsActive(bool state)
    {
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

        if (virusPlayer != null) virusPlayer.SetActive(state);
        if (virusMovementScript != null) virusMovementScript.enabled = state;

        if (state == false)
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

    void OnApplicationQuit()
    {
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

        GameObject[] efectos = GameObject.FindGameObjectsWithTag("Efectos");
        foreach (GameObject efecto in efectos)
        {
            if (efecto != null) Destroy(efecto);
        }

        FloatingText[] textosEnPantalla = Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None);

        foreach (FloatingText texto in textosEnPantalla)
        {
            if (texto != null && texto.gameObject.activeSelf)
            {
                texto.gameObject.SetActive(false);
            }
        }

        Debug.Log("<color=cyan>[CLEANUP]</color> Textos y efectos limpiados correctamente.");
    }

    public void UpdateCursorState(bool isPlaying)
    {
        if (!isPlaying)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

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

    public void OpenSettingsPanel()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        if (isGameActive) { Time.timeScale = 0f; virusMovementScript.enabled = false; }

        GameObject origin = (pausePanel.activeSelf) ? pausePanel : menuPanel;
        DoPanelTransition(origin, settingsPanel, null, settingsFirstSelectedButton);
    }

    public void CloseSettingsPanel()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        if (isGameActive)
            DoPanelTransition(settingsPanel, pausePanel, null, pauseFirstSelectedButton);
        else
            DoPanelTransition(settingsPanel, menuPanel);
    }

    // =========================================================
    // 🛑 LÓGICA DE LA DEMO 🛑
    // =========================================================
    public void MostrarFinDeDemo()
    {
        // Evitamos que se ejecute dos veces
        if (panelFinDemo != null && panelFinDemo.activeSelf) return;

        // 1. APAGADO INSTANTÁNEO Y DESBLOQUEO
        isTransitioning = false;
        DesbloquearControlesUI();
        isGameActive = false;
        Time.timeScale = 0f;

        // 2. Limpieza radical del entorno (0 frames de espera)
        SetMapsActive(false);
        ResetCameraZoom();

        if (gameUI != null) gameUI.SetActive(false);
        if (zonePanel != null) zonePanel.SetActive(false);
        if (shinyPanel != null) shinyPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        if (virusPlayer != null) virusPlayer.SetActive(false);
        if (virusMovementScript != null) virusMovementScript.enabled = false;
        if (PopulationManager.instance != null) PopulationManager.instance.ClearAllPersonas();
        StopAllActiveRunEffects();

        FloatingScoreUI[] puntos = Object.FindObjectsByType<FloatingScoreUI>(FindObjectsSortMode.None);
        foreach (var p in puntos) if (p != null) Destroy(p.gameObject);

        FloatingText[] danos = Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None);
        foreach (var d in danos) if (d != null) d.gameObject.SetActive(false);

        // 3. ENCENDIDO INSTANTÁNEO DEL PANEL (¡BAM!)
        if (panelFinDemo != null)
        {
            panelFinDemo.SetActive(true);
            if (logoDemo != null) StartCoroutine(AnimarLogoDemo());
        }

        UpdateCursorState(false);

        // 4. Aseguramos el mando sin esperas largas
        StartCoroutine(SeleccionarBotonDemoSeguro());
    }

    // 🎮 Minicorrutina para evitar el "Bug del Frame 0" con el mando
    private IEnumerator SeleccionarBotonDemoSeguro()
    {
        yield return null; // Solo esperamos 1 frame para que los botones "nazcan"

        if (!MenuGamepadNavigator.usandoRaton && panelFinDemo != null)
        {
            Button btn = panelFinDemo.GetComponentInChildren<Button>();
            if (btn != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
            }
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // 🌟 ANIMACIÓN DEL LOGO (Efecto POP con rebote)
    private IEnumerator AnimarLogoDemo()
    {
        Vector3 escalaFinal = new Vector3(0.7f, 0.7f, 1f);
        logoDemo.localScale = Vector3.zero;

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progreso = elapsed / duration;

            float tension = 2.0f;
            float t = progreso - 1f;
            float rebote = 1f + (t * t * ((tension + 1f) * t + tension));

            logoDemo.localScale = escalaFinal * rebote;
            yield return null;
        }

        logoDemo.localScale = escalaFinal;
    }

    // 🌟 BOTÓN DE STEAM
    public void Boton_IrASteam()
    {
        // Abre el navegador del PC y lo lleva a tu página
        Application.OpenURL(urlSteam);
    }

    public void Boton_IrADC()
    {
        // Abre el navegador del PC y lo lleva a tu página
        Application.OpenURL(urlDiscord);
    }
    // =========================================================
    // 🌀 SISTEMA DE TRANSICIÓN UNIFICADO (HEXÁGONO)
    // =========================================================

    public void DoPanelTransition(GameObject panelToClose, GameObject panelToOpen, System.Action actionBeforeOpen = null, GameObject firstSelectable = null)
    {
        if (isTransitioning) return; // 🛡️ Evita solapamientos
        StartCoroutine(UniversalTransitionRoutine(panelToClose, panelToOpen, actionBeforeOpen, firstSelectable));
    }

    private IEnumerator UniversalTransitionRoutine(GameObject panelToClose, GameObject panelToOpen, System.Action actionBeforeOpen, GameObject firstSelectable)
    {
        isTransitioning = true; // 🛡️ ACTIVA EL ESCUDO

        if (transitionScript != null)
        {
            transitionScript.SetShape(1);
            transitionScript.CloseBlackScreen();
            yield return new WaitForSecondsRealtime(0.55f);

            if (tiempoEsperaEnNegro > 0)
            {
                yield return new WaitForSecondsRealtime(tiempoEsperaEnNegro);
            }
        }

        if (panelToClose != null) panelToClose.SetActive(false);
        if (panelToOpen != null) panelToOpen.SetActive(true);

        actionBeforeOpen?.Invoke();

        // 🛑 EL TRUCO MÁGICO: Esperamos 1 frame para que a Unity le dé tiempo a encender los botones
        yield return null;

        // 🎮 LÓGICA DE SELECCIÓN ARREGLADA
        if (!MenuGamepadNavigator.usandoRaton)
        {
            if (firstSelectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelectable);
            }
        }
        else
        {
            // 🖱️ Si usamos ratón, limpiamos la selección
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        yield return new WaitForEndOfFrame();

        if (transitionScript != null)
        {
            transitionScript.OpenBlackScreen();
            yield return new WaitForSecondsRealtime(0.55f);
        }

        isTransitioning = false; // 🛡️ DESACTIVA EL ESCUDO: YA PUEDEN CLICAR OTRA VEZ
    }

    public void CerrarJuego()
    {
        if (isTransitioning) return; // 🛡️ Bloqueo anti-spam

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        Debug.Log("El juego se ha cerrado");
    }

    public void RegisterFigureTypeInfected(int phase)
    {
        if (phase < 0 || phase >= figuresCaughtInRun.Length) return;

        figuresCaughtInRun[phase] = true;

        // Comprobar si ya tenemos todos
        bool allCaught = true;
        for (int i = 0; i < figuresCaughtInRun.Length; i++)
        {
            if (!figuresCaughtInRun[i])
            {
                allCaught = false;
                break;
            }
        }

        if (allCaught)
        {
            SteamManagerCustom.Instance.UnlockAchievement("ACH_1OFITS");
        }
    }
}