using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Selector de Modo (Menú Principal)")]
    // modeSelectionPanel ya no se usa, pero lo dejo por si tienes referencias perdidas
    public GameObject modeSelectionPanel; 
    public Button continueButton;
    public TextMeshProUGUI continueInfoText;

    [Header("Sistema de Zonas")]
    public GameObject[] mapList;

    [Header("Referencias")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript;

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject dayOverPanel;
    public GameObject gameOverPanel;
    public GameObject shinyPanel;
    public GameObject zonePanel;
    public GameObject pausePanel;

    [Header("UI Text (Listas)")]
    public List<TextMeshProUGUI> timerTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> sessionScoreTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> contagionCoinsTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> daysRemainingTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> shinyStoreTexts = new List<TextMeshProUGUI>();

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int maxInfectionsPerRound = 5;
    public int baseDaysUntilCure = 5;
    public int totalDaysUntilCure = 5;

    [Header("Configuración Shiny")]
    public float shinyChance = 0.75f;
    private bool isShinyDayToday = false;
    private int shiniesToSpawnToday = 0;
    private List<PersonaInfeccion> shinysThisDay = new List<PersonaInfeccion>();
    private int shiniesCapturedToday = 0;

    [Header("Persistencia de Shinies por Zona")]
    private Dictionary<int, int> stockShiniesZonas = new Dictionary<int, int>();
    public int[] shiniesBasePorMapa = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [HideInInspector] public bool isGameActive;
    [HideInInspector] public int currentSessionInfected;
    [HideInInspector] public int contagionCoins;

    float currentTimer;
    int daysRemaining;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void Start()
    {
        // 1. CORRECCIÓN DE VOLUMEN (Recuperada)
        float volumenGuardado = PlayerPrefs.GetFloat("VolumenGlobal", 1f);
        AudioListener.volume = volumenGuardado;

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ForceHardReset();
        RecalculateTotalDaysUntilCure();
        
        // Al iniciar, mostramos menú principal y configuramos el botón continuar
        ShowMainMenu();
    }

    // --- NUEVA FUNCIÓN: SUMAR MONEDAS SIN GASTAR CAPACIDAD ---
    public void AddCoins(int amount)
    {
        contagionCoins += amount;
        UpdateUI();
    }

    // --- REGISTRAR INFECCIÓN (CAPACIDAD DEL DÍA) ---
    public void RegisterInfection()
    {
        if (!isGameActive || currentSessionInfected >= maxInfectionsPerRound) return;
        currentSessionInfected++;
        UpdateUI();
        if (currentSessionInfected >= maxInfectionsPerRound) EndSessionDay();
    }

    // ---------------------------------------------------------
    // --- SISTEMA DE MENÚ (CORREGIDO) ---
    // ---------------------------------------------------------

    // Esta función controla qué botones se ven y si están activos
    void ShowMainMenu()
    {
        menuPanel.SetActive(true);

        // Ocultar resto de paneles
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        shinyPanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);
        if (modeSelectionPanel) modeSelectionPanel.SetActive(false); // Por si acaso
        
        virusPlayer.SetActive(false);

        // --- LÓGICA BOTÓN CONTINUAR ---
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);

            if (Guardado.instance != null && Guardado.instance.HasSavedGame())
            {
                // HAY PARTIDA: Botón activo
                continueButton.interactable = true;
                if (continueInfoText)
                {
                    continueInfoText.text = Guardado.instance.GetContinueDetails();
                    continueInfoText.alpha = 1f;
                }
            }
            else
            {
                // NO HAY PARTIDA: Botón gris
                continueButton.interactable = false;
                if (continueInfoText)
                {
                    continueInfoText.text = "Sin datos";
                    continueInfoText.alpha = 0.5f;
                }
            }
        }
    }

    // 1. BOTÓN "NUEVA PARTIDA"
    public void Button_NewGame()
    {
        // Borramos todo el progreso permanentemente
        if (Guardado.instance) 
        {
            Guardado.instance.ResetAllProgress();
        }

        NewGameFromMainMenu(); 
    }

    // 2. BOTÓN "CONTINUAR"
    public void Button_Continue()
    {
        LoadRunAndStart();
    }

    // Lógica para cargar los datos (CORREGIDA PARA NO EMPEZAR DIRECTO)
    void LoadRunAndStart()
    {
        daysRemaining = PlayerPrefs.GetInt("Run_Day", totalDaysUntilCure);
        contagionCoins = PlayerPrefs.GetInt("Run_Coins", 0);
        int savedMap = PlayerPrefs.GetInt("Run_Map", 0);

        PlayerPrefs.SetInt("CurrentMapIndex", savedMap);
        PlayerPrefs.Save();

        if (stockShiniesZonas.Count == 0) InicializarStockDeShinies();

        // --- AQUÍ ESTÁ EL CAMBIO ---
        // NO llamamos a StartSession(). Vamos al panel de preparación (DayOverPanel)
        menuPanel.SetActive(false);
        gameUI.SetActive(false);
        dayOverPanel.SetActive(true); // <--- MOSTRAMOS EL PANEL
        
        UpdateUI(); // Actualizamos visualmente
    }

    // Lógica nueva partida (CORREGIDA PARA NO EMPEZAR DIRECTO)
    public void NewGameFromMainMenu()
    {
        ResetRunData();
        
        // --- AQUÍ ESTÁ EL CAMBIO ---
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameUI.SetActive(false);
        dayOverPanel.SetActive(true); // <--- MOSTRAMOS EL PANEL
        
        UpdateUI();
    }

    // ---------------------------------------------------------
    // --- FIN SISTEMA DE MENÚ ---
    // ---------------------------------------------------------

    void ForceHardReset()
    {
        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();
    }

    public void RecalculateTotalDaysUntilCure()
    {
        int bonus = (Guardado.instance != null) ? Guardado.instance.bonusDaysPermanent : 0;
        int previousTotal = totalDaysUntilCure;
        totalDaysUntilCure = baseDaysUntilCure + bonus;

        if (totalDaysUntilCure > previousTotal && !Guardado.instance.HasSavedGame())
            daysRemaining += (totalDaysUntilCure - previousTotal);

        if (totalDaysUntilCure < 1) totalDaysUntilCure = 1;
        UpdateUI();
    }

    void ResetDays() { daysRemaining = totalDaysUntilCure; }

    void Update()
    {
        if (!isGameActive) return;

        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();

        currentTimer -= Time.deltaTime;
        foreach (var t in timerTexts)
        {
            if (t != null) t.text = currentTimer.ToString("F1") + "s";
        }

        if (currentTimer <= 0) EndSessionDay();
    }

    public void OpenShinyShop() { shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); UpdateUI(); }
    public void OpenZoneShop() { if (zonePanel != null) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (Guardado.instance && daysRemaining > 0)
        {
            int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
            Guardado.instance.SaveRunState(daysRemaining, contagionCoins, currentMap);
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

        gameOverPanel.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(false);
        shinyPanel.SetActive(false);
        if (modeSelectionPanel) modeSelectionPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

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

    public void InicializarStockDeShinies()
    {
        stockShiniesZonas.Clear();
        int extrasHabilidad = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;

        for (int i = 0; i < mapList.Length; i++)
        {
            int baseZona = (i < shiniesBasePorMapa.Length) ? shiniesBasePorMapa[i] : (i + 1);
            stockShiniesZonas[i] = baseZona + extrasHabilidad;
        }
    }

    void ResetRunData()
    {
        RecalculateTotalDaysUntilCure();
        ResetDays();
        InicializarStockDeShinies();

        bool tieneHabilidadMeta = Guardado.instance != null && Guardado.instance.keepZonesUnlocked;
        if (!tieneHabilidadMeta)
        {
            for (int i = 1; i <= 10; i++) PlayerPrefs.SetInt("ZoneUnlocked_" + i, 0);
        }

        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();
        ActivateMap(0);

        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset) ForceHardReset();
        if (Guardado.instance) Guardado.instance.ApplyPermanentInitialUpgrade();

        contagionCoins = Guardado.instance != null ? Guardado.instance.startingCoins : 0;
        UpdateUI();
    }

    // Métodos legacy/sobrantes que he dejado para evitar errores de referencias perdidas
    public void OpenModeSelection() { /* Ya no se usa */ }
    public void CloseModeSelection() { /* Ya no se usa */ }
    public void TryStartGame() { /* Ya no se usa */ }

    // --- START SESSION CORREGIDO ---
    public void StartSession()
    {
        // 1. Cerrar todos los paneles previos
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (modeSelectionPanel) modeSelectionPanel.SetActive(false);
        if (menuPanel) menuPanel.SetActive(false);

        Time.timeScale = 1f;

        // 3. Ingresos Pasivos
        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;

            if (Guardado.instance.shinyPerZoneDaily > 0)
                Guardado.instance.AddShinyDNA(numeroZonas * Guardado.instance.shinyPerZoneDaily);
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToGameMusic();

        shinysThisDay.Clear();
        shiniesCapturedToday = 0;

        int indexActual = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        if (!stockShiniesZonas.ContainsKey(indexActual)) InicializarStockDeShinies();
        int stockDisponible = stockShiniesZonas.ContainsKey(indexActual) ? stockShiniesZonas[indexActual] : 0;

        float probabilidadActual = (Guardado.instance != null && Guardado.instance.guaranteedShiny) ? 1.0f : shinyChance;

        if (Random.value <= probabilidadActual && stockDisponible > 0)
        {
            isShinyDayToday = true;
            int extrasHabilidad = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;
            shiniesToSpawnToday = Mathf.Min(1 + extrasHabilidad, stockDisponible);
        }
        else
        {
            isShinyDayToday = false;
            shiniesToSpawnToday = 0;
        }

        CleanUpScene();

        int savedMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        ActivateMap(savedMap);

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(shiniesToSpawnToday);

        PersonaInfeccion[] allPersonas = Object.FindObjectsByType<PersonaInfeccion>(FindObjectsSortMode.None);
        foreach (var p in allPersonas) if (p.isShiny) shinysThisDay.Add(p);

        gameUI.SetActive(true);
        virusPlayer.SetActive(true);
        if (virusMovementScript != null) virusMovementScript.enabled = true;
        UpdateUI();
    }

    void EndSessionDay()
    {
        isGameActive = false;
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        gameUI.SetActive(false);
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        int mapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int zoneMultiplier = (mapIndex == 1) ? 2 : (mapIndex == 2) ? 3 : 1;
        int baseMultiplier = Guardado.instance != null ? Guardado.instance.coinMultiplier : 1;
        int sMultiplier = Guardado.instance != null ? Guardado.instance.shinyMultiplier : 1;

        if (EndDayResultsPanel.instance != null)
        {
            EndDayResultsPanel.instance.ShowResults(currentSessionInfected, baseMultiplier, zoneMultiplier, shiniesCapturedToday, sMultiplier);
        }

        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);
        daysRemaining--;
    }

    public void OnEndDayResultsFinished(int earnings, int shinies)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        contagionCoins += earnings;
        if (Guardado.instance != null) Guardado.instance.AddShinyDNA(shinies);

        virusPlayer.SetActive(false);
        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

        if (daysRemaining > 0)
        {
            int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
            if (Guardado.instance) Guardado.instance.SaveRunState(daysRemaining, contagionCoins, currentMap);
            dayOverPanel.SetActive(true);
        }
        else
        {
            GameOver();
        }
        UpdateUI();
    }

    public void GameOver()
    {
        dayOverPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        if (Guardado.instance) Guardado.instance.ClearRunState();
    }

    public void UpdateUI()
    {
        foreach (var t in sessionScoreTexts) if (t != null) t.text = "Hoy: " + currentSessionInfected + " / " + maxInfectionsPerRound;
        foreach (var t in contagionCoinsTexts) if (t != null) t.text = "Monedas: " + contagionCoins;
        foreach (var t in daysRemainingTexts) if (t != null) t.text = "Quedan " + daysRemaining + " días";
        if (Guardado.instance != null)
        {
            foreach (var t in shinyStoreTexts) if (t != null) t.text = "ADN Shiny: " + Guardado.instance.shinyDNA;
        }
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

    public void RegisterShinyCapture(PersonaInfeccion shiny)
    {
        if (shiny == null || !shinysThisDay.Contains(shiny)) return;

        int indexActual = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        if (stockShiniesZonas.ContainsKey(indexActual) && stockShiniesZonas[indexActual] > 0)
        {
            stockShiniesZonas[indexActual]--;
        }

        shiniesCapturedToday++;
        shinysThisDay.Remove(shiny);

        int cantidadFinal = Guardado.instance != null ? Guardado.instance.GetFinalShinyValue() : 1;
        Guardado.instance.AddShinyDNA(cantidadFinal);

        if (VirusEvolverController.instance != null) VirusEvolverController.instance.RegisterShiny();

        UpdateUI();
    }

    public int GetStockRestante(int mapIndex)
    {
        if (stockShiniesZonas != null && stockShiniesZonas.ContainsKey(mapIndex))
            return stockShiniesZonas[mapIndex];

        int extras = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;
        int baseZona = (mapIndex < shiniesBasePorMapa.Length) ? shiniesBasePorMapa[mapIndex] : (mapIndex + 1);
        return baseZona + extras;
    }

    public void ActualizarStockPorCompraHabilidad()
    {
        List<int> keys = new List<int>(stockShiniesZonas.Keys);
        foreach (int i in keys) stockShiniesZonas[i]++;

        ZoneItem[] todosLosBotones = Object.FindObjectsByType<ZoneItem>(FindObjectsSortMode.None);
        foreach (ZoneItem boton in todosLosBotones) boton.UpdateUI();

        UpdateUI();
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
}