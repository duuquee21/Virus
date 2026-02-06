using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // <--- NECESARIO PARA EL BOTÓN

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Selector de Modo (NUEVO)")]
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

    // --- SISTEMA DE STOCK POR ZONA ---
    [Header("Persistencia de Shinies por Zona")]
    private Dictionary<int, int> stockShiniesZonas = new Dictionary<int, int>();
    public int[] shiniesBasePorMapa = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    // ----------------------------------------

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
        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ForceHardReset();
        RecalculateTotalDaysUntilCure();
        // Nota: No llamamos a ResetDays() aquí para no sobrescribir si vamos a continuar
        ShowMainMenu();
    }

    // ---------------------------------------------------------
    // --- NUEVO: SISTEMA DE MENÚ (NUEVA / CONTINUAR) ---
    // ---------------------------------------------------------

    // 1. ESTO SE PONE EN EL BOTÓN "JUGAR" DEL MENÚ PRINCIPAL
    public void OpenModeSelection()
    {
        menuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);

        // Preguntamos a Guardado si hay una partida a medias
        if (Guardado.instance != null && Guardado.instance.HasSavedGame())
        {
            continueButton.interactable = true;
            continueInfoText.text = Guardado.instance.GetContinueDetails();
        }
        else
        {
            continueButton.interactable = false;
            continueInfoText.text = "Sin datos";
        }
    }

    // 2. ESTO SE PONE EN EL BOTÓN "NUEVA PARTIDA"
    public void Button_NewGame()
    {
       
        if (Guardado.instance) 
        {
            Guardado.instance.ResetAllProgress();
        }

        modeSelectionPanel.SetActive(false);
        
        // Iniciamos el juego (que ahora cargará todo a 0 porque acabamos de borrarlo)
        NewGameFromMainMenu(); 
    }

    // 3. ESTO SE PONE EN EL BOTÓN "CONTINUAR"
    public void Button_Continue()
    {
        modeSelectionPanel.SetActive(false);
        LoadRunAndStart();
    }

    // 4. ESTO SE PONE EN EL BOTÓN "CERRAR (X)"
    public void CloseModeSelection()
    {
        modeSelectionPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    // Lógica para cargar los datos y seguir jugando
    void LoadRunAndStart()
    {
        // Recuperamos datos de PlayerPrefs a través de Guardado o directamente
        daysRemaining = PlayerPrefs.GetInt("Run_Day", totalDaysUntilCure);
        contagionCoins = PlayerPrefs.GetInt("Run_Coins", 0);
        int savedMap = PlayerPrefs.GetInt("Run_Map", 0);

        // Establecemos el mapa y guardamos
        PlayerPrefs.SetInt("CurrentMapIndex", savedMap);
        PlayerPrefs.Save();

        // Aseguramos que el stock esté inicializado (si se perdió la referencia en memoria)
        if (stockShiniesZonas.Count == 0) InicializarStockDeShinies();

        StartSession();
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

        // Solo sumamos días si NO estamos en medio de una partida (para no regalar días al volver al menú)
        if (totalDaysUntilCure > previousTotal && !Guardado.instance.HasSavedGame())
            daysRemaining += (totalDaysUntilCure - previousTotal);

        if (totalDaysUntilCure < 1) totalDaysUntilCure = 1;
        UpdateUI();
    }

    void ResetDays() { daysRemaining = totalDaysUntilCure; }

    void Update()
    {
        if (!isGameActive) return;
        if (!isGameActive) return; 

        // --- LÓGICA DE PAUSA ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        currentTimer -= Time.deltaTime;
        foreach (var t in timerTexts)
        {
            if (t != null)
                t.text = currentTimer.ToString("F1") + "s";
        }

        if (currentTimer <= 0) EndSessionDay();
    }

    public void OpenShinyShop() { shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); UpdateUI(); }
    public void OpenZoneShop() { if (zonePanel != null) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); }

    // Esta función ahora solo se llama desde "NUEVA PARTIDA"
    public void NewGameFromMainMenu()
    {
        ResetRunData();
        // StartSession se llama al final de ResetRunData -> UpdateUI -> pero aquí lo forzamos o dejamos que fluya
        // ResetRunData ya prepara todo, pero NO inicia la sesión automáticamente en tu código original,
        // lo dejaba en el menú "DayOver". Vamos a hacer que inicie sesión para fluidez o vaya al mapa.
        
        // En tu lógica original:
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dayOverPanel.SetActive(false); // Ojo: saltamos directo a la acción o al mapa
        
        StartSession(); 
    }

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
        if(dayOverPanel) dayOverPanel.SetActive(false);
        
        shinyPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        
        // --- AQUÍ ESTÁ LA SOLUCIÓN ---
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
    }

    public void InicializarStockDeShinies()
    {
        stockShiniesZonas.Clear();
        int extrasHabilidad = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;

        for (int i = 0; i < mapList.Length; i++)
        {
            int baseZona = (i < shiniesBasePorMapa.Length) ? shiniesBasePorMapa[i] : (i + 1);
            stockShiniesZonas[i] = baseZona + extrasHabilidad;
            Debug.Log($"Zona {i} inicializada con {stockShiniesZonas[i]} shinies.");
        }
    }

    void ResetRunData()
    {
        RecalculateTotalDaysUntilCure();
        ResetDays();

        // Limpieza de stock de shinies al empezar nueva partida
        InicializarStockDeShinies();

        bool tieneHabilidadMeta = Guardado.instance != null && Guardado.instance.keepZonesUnlocked;
        if (!tieneHabilidadMeta)
        {
            Debug.Log("<color=red>Sin habilidad: Reseteando zonas.</color>");
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

    // Modificado para abrir el selector en vez de empezar directo
    public void TryStartGame() 
    { 
        OpenModeSelection(); 
    }

    public void StartSession()
    {
        dayOverPanel.SetActive(false);
        modeSelectionPanel.SetActive(false); // Aseguramos que se cierra

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

        // --- LÓGICA DE STOCK Y PROBABILIDAD ---
        int indexActual = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        if (!stockShiniesZonas.ContainsKey(indexActual)) InicializarStockDeShinies();
        int stockDisponible = stockShiniesZonas[indexActual];

        float probabilidadActual = (Guardado.instance != null && Guardado.instance.guaranteedShiny) ? 1.0f : shinyChance;

        if (Random.value <= probabilidadActual && stockDisponible > 0)
        {
            isShinyDayToday = true;
            int extrasHabilidad = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;
            int intencionSpawn = 1 + extrasHabilidad;

            shiniesToSpawnToday = Mathf.Min(intencionSpawn, stockDisponible);
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

    public void RegisterInfection()
    {
        if (!isGameActive || currentSessionInfected >= maxInfectionsPerRound) return;
        currentSessionInfected++;
        UpdateUI();
        if (currentSessionInfected >= maxInfectionsPerRound) EndSessionDay();
    }

    void EndSessionDay()
    {
        isGameActive = false;

        // Efecto Cámara Lenta
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

    // Se llama cuando cierras el panel de resultados
    public void OnEndDayResultsFinished(int earnings, int shinies)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        contagionCoins += earnings;
        if (Guardado.instance != null) Guardado.instance.AddShinyDNA(shinies);

        virusPlayer.SetActive(false);
        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

        // --- LÓGICA DE GUARDADO ---
        if (daysRemaining > 0)
        {
            // Si seguimos vivos, guardamos
            int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
            if(Guardado.instance) Guardado.instance.SaveRunState(daysRemaining, contagionCoins, currentMap);
            
            dayOverPanel.SetActive(true);
        }
        else
        {
            GameOver(); // Si morimos, GameOver y borrar save
        }
        
        UpdateUI();
    }

    public void GameOver()
    {
        dayOverPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        // Borramos el save al perder
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

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        virusPlayer.SetActive(false);
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
            Debug.Log($"Stock zona {indexActual} bajó a {stockShiniesZonas[indexActual]}");
        }

        shiniesCapturedToday++;
        shinysThisDay.Remove(shiny);

        int cantidadFinal = Guardado.instance != null ? Guardado.instance.GetFinalShinyValue() : 1;
        Guardado.instance.AddShinyDNA(cantidadFinal);

        if (VirusEvolverController.instance != null)
            VirusEvolverController.instance.RegisterShiny();

        UpdateUI();
    }

    public int GetStockRestante(int mapIndex)
    {
        if (stockShiniesZonas != null && stockShiniesZonas.ContainsKey(mapIndex))
        {
            return stockShiniesZonas[mapIndex];
        }

        int extras = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;
        int baseZona = (mapIndex < shiniesBasePorMapa.Length) ? shiniesBasePorMapa[mapIndex] : (mapIndex + 1);

        return baseZona + extras;
    }

    public void ActualizarStockPorCompraHabilidad()
    {
        List<int> keys = new List<int>(stockShiniesZonas.Keys);

        foreach (int i in keys)
        {
            stockShiniesZonas[i]++; 
        }

        ZoneItem[] todosLosBotones = Object.FindObjectsByType<ZoneItem>(FindObjectsSortMode.None);
        foreach (ZoneItem boton in todosLosBotones)
        {
            boton.UpdateUI();
        }

        UpdateUI();
    }
    
    public void TogglePause()
    {
        // Si el panel no está asignado, salimos para evitar errores
        if (pausePanel == null) return;

        bool estaPausado = pausePanel.activeSelf; // ¿Está visible ahora?

        if (estaPausado)
        {
            // --- DESPAUSAR (VOLVER AL JUEGO) ---
            pausePanel.SetActive(false);
            Time.timeScale = 1f; // Tiempo normal
            if (virusMovementScript != null) virusMovementScript.enabled = true; // Activar movimiento
        }
        else
        {
            // --- PAUSAR (CONGELAR TODO) ---
            pausePanel.SetActive(true);
            Time.timeScale = 0f; // Tiempo congelado
            if (virusMovementScript != null) virusMovementScript.enabled = false; // Bloquear movimiento
        }
    }
}