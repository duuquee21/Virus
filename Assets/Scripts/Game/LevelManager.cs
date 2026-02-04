using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Sistema de Zonas")]
    public GameObject[] mapList;
    
    [Header("Selector de Modo")]
    public GameObject modeSelectionPanel;
    public UnityEngine.UI.Button continueButton; // Arrastra el BOTÓN ENTERO
    public TextMeshProUGUI
        continueInfoText;
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

    [Header("Configuración Shiny (75% Chance)")]
    public float shinyChance = 0.75f;
    private bool shinyAlreadySpawnedInRun = false;
    // FIJAR: Hemos añadido de nuevo esta variable para evitar el error
    private bool isShinyDayToday = false;
    private int shiniesToSpawnToday = 0;

    [HideInInspector] public bool isGameActive;
    [HideInInspector] public int currentSessionInfected;
    [HideInInspector] public int contagionCoins;
    [HideInInspector] public bool isShinyCollectedInRun = false;
    [HideInInspector] public bool shinyCaughtToday = false;

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
        ResetDays();
        ShowMainMenu();
    }

    void ForceHardReset()
    {
        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();

        Debug.Log("<color=cyan>Seguridad:</color> Todos los niveles de la tienda reiniciados para nueva sesión.");
    }

    public void RecalculateTotalDaysUntilCure()
    {
        int bonus = (Guardado.instance != null) ? Guardado.instance.bonusDaysPermanent : 0;
        int previousTotal = totalDaysUntilCure;
        totalDaysUntilCure = baseDaysUntilCure + bonus;

        if (totalDaysUntilCure > previousTotal)
            daysRemaining += (totalDaysUntilCure - previousTotal);

        if (totalDaysUntilCure < 1) totalDaysUntilCure = 1;
        UpdateUI();
    }

    void ResetDays() { daysRemaining = totalDaysUntilCure; }

    void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;
        foreach (var t in timerTexts)
        {
            if (t != null)
                t.text = currentTimer.ToString("F1") + "s";
        }

        if (currentTimer <= 0)
        {
            EndSessionDay(); // Esta función ahora decidirá si es GameOver o no
        }
      
    }

    public void TryStartGame() { ResetRunData(); }
    public void OpenShinyShop() { shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); UpdateUI(); }
    public void OpenZoneShop() { gameOverPanel.SetActive(false); dayOverPanel.SetActive(false); if (zonePanel != null) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); dayOverPanel.SetActive(true); gameOverPanel.SetActive(false); }


    public void NewGameFromMainMenu()
    {
        ResetRunData();

        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameUI.SetActive(false);
        virusPlayer.SetActive(false);

        dayOverPanel.SetActive(true);   // 👈 Se abre directamente
        UpdateUI();
    }
    public void ReturnToMenu()
    {
        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

        gameOverPanel.SetActive(false);
        dayOverPanel.SetActive(false); 
      
        shinyPanel.SetActive(false);

        ShowMainMenu();
    }

    public void ActivateMap(int zoneID)
    {
        PlayerPrefs.SetInt("CurrentMapIndex", zoneID);
        PlayerPrefs.Save();

        foreach (GameObject map in mapList)
        {
            if (map != null) map.SetActive(false);
        }

        if (zoneID >= 0 && zoneID < mapList.Length)
        {
            if (mapList[zoneID] != null) mapList[zoneID].SetActive(true);
        }
    }

    void ResetRunData()
    {
        RecalculateTotalDaysUntilCure();
        ResetDays();

        isShinyCollectedInRun = false;
        shinyAlreadySpawnedInRun = false;
        isShinyDayToday = false;
        shiniesToSpawnToday = 0;

        PlayerPrefs.SetInt("CurrentMapIndex", 0);

        for (int i = 1; i <= 10; i++)
            PlayerPrefs.SetInt("ZoneUnlocked_" + i, 0);

        PlayerPrefs.Save();
        ActivateMap(0);

        // --- Persistencia ---
        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset)
        {
            if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
            if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
            if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
            if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
            if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();
        }

        if (Guardado.instance)
            Guardado.instance.ApplyPermanentInitialUpgrade();

        contagionCoins = Guardado.instance != null ? Guardado.instance.startingCoins : 0;
    }

    public void StartSession()
    {
        menuPanel.SetActive(false);
        dayOverPanel.SetActive(false);

        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;

            if (Guardado.instance.shinyPerZoneDaily > 0)
                Guardado.instance.AddShinyDNA(numeroZonas * Guardado.instance.shinyPerZoneDaily);
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToGameMusic();

        // --- LÓGICA DE DOBLE SHINY ---
        shiniesToSpawnToday = 0;
        isShinyDayToday = false;
        shinyCaughtToday = false;// Resetear el estado para el nuevo día

        if (!shinyAlreadySpawnedInRun)
        {
            float probabilidadActual = (Guardado.instance != null && Guardado.instance.guaranteedShiny) ? 1.0f : shinyChance;

            if (Random.value <= probabilidadActual)
            {
                isShinyDayToday = true; //
                if (Guardado.instance != null && Guardado.instance.extraShiniesPerRound > 0)
                {
                    shiniesToSpawnToday = 2;
                    Debug.Log("<color=yellow>¡Sorteo Shiny Ganado: Aparecerán 2!</color>");
                }
                else
                {
                    shiniesToSpawnToday = 1;
                    Debug.Log("<color=yellow>Sorteo Shiny Ganado: Aparecerá 1.</color>");
                }
                shinyAlreadySpawnedInRun = true;
            }
        }

        CleanUpScene();
        int savedMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        ActivateMap(savedMap);

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(shiniesToSpawnToday); // Usamos shiniesToSpawnToday

        UpdateUI();
        menuPanel.SetActive(false);
        dayOverPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameUI.SetActive(true);
        virusPlayer.SetActive(true);

        if (virusMovementScript != null) virusMovementScript.enabled = true;
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

        int mapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int zoneMultiplier = (mapIndex == 1) ? 2 : (mapIndex == 2) ? 3 : 1;
        int shinyBonusMultiplier = shinyCaughtToday ? 3 : 1;
        int earnings = currentSessionInfected 
                       * (Guardado.instance != null ? Guardado.instance.coinMultiplier : 1) 
                       * zoneMultiplier 
                       * shinyBonusMultiplier;        
        contagionCoins += earnings;

        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);

        // Restamos el día
        daysRemaining--;

        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();
        gameUI.SetActive(false);
        virusPlayer.SetActive(false);

        // LÓGICA DE DECISIÓN:
        if (daysRemaining <= 0)
        {
            daysRemaining = 0;
            GameOver(); // Si no quedan días, saltamos a GameOver
        }
        else
        {
            dayOverPanel.SetActive(true); // Si quedan días, mostramos panel de tienda/siguiente día
        }
        
        if (daysRemaining > 0) // Solo guardamos si sigues vivo
        {
            int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
            if (Guardado.instance) 
                Guardado.instance.SaveRunState(daysRemaining, contagionCoins, currentMap);
        }
        else 
        {
            // Si es el último día o Game Over, borramos la partida guardada
            if (Guardado.instance) Guardado.instance.ClearRunState();
        }

        UpdateUI();
    }

   public void GameOver()
    {
        // Ya no necesitas recalcular monedas aquí porque EndSessionDay ya lo hizo
        dayOverPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        Debug.Log("Game Over: Se acabaron los días.");
    }

    public void UpdateUI()
    {
        foreach (var t in sessionScoreTexts)
            if (t != null)
                t.text = "Hoy: " + currentSessionInfected + " / " + maxInfectionsPerRound;

        foreach (var t in contagionCoinsTexts)
            if (t != null)
                t.text = "Monedas: " + contagionCoins;

        foreach (var t in daysRemainingTexts)
            if (t != null)
                t.text = "Quedan " + daysRemaining + " días";

        if (Guardado.instance != null)
        {
            foreach (var t in shinyStoreTexts)
                if (t != null)
                    t.text = "ADN Shiny: " + Guardado.instance.shinyDNA;
        }

    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        virusPlayer.SetActive(false);
    }

    public void LostToMenu()
    {
        ResetRunData();
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        virusPlayer.SetActive(false);

    }
    // Añade esto al final de LevelManager.cs
    public void SetCurrentTimerDebug(float newTime)
    {
        currentTimer = newTime;
    }

    // Método para que el Debug Menu cambie el tiempo base
    public void SetGameDuration(float newDuration)
    {
        gameDuration = newDuration;
        Debug.Log($"<color=yellow>LevelManager:</color> Duración de partida cambiada a {newDuration}s");
    }

    // Método para forzar el tiempo actual (si la partida ya ha empezado)
    public void ForceCurrentTimer(float secondsLeft)
    {
        if (isGameActive)
        {
            currentTimer = secondsLeft;
        }
    }
    void CleanUpScene()
    {
        PersonaInfeccion[] genteEnPantalla = Object.FindObjectsByType<PersonaInfeccion>(FindObjectsSortMode.None);
        foreach (PersonaInfeccion persona in genteEnPantalla)
        {
            if (persona != null) Destroy(persona.gameObject);
        }
    }

    public int GetTotalUnlockedZones()
    {
        int count = 1;
        if (PlayerPrefs.GetInt("Zone_1_Unlocked", 0) == 1) count++;
        if (PlayerPrefs.GetInt("Zone_2_Unlocked", 0) == 1) count++;
        UpdateUI();
        return count;
    }
    
    // --- LÓGICA DE NUEVA PARTIDA / CONTINUAR ---

    // 1. ESTO SE PONE EN EL BOTÓN "JUGAR" DEL MENÚ PRINCIPAL
    public void OpenModeSelection()
    {
        menuPanel.SetActive(false); // Ocultamos menú principal
        modeSelectionPanel.SetActive(true); // Mostramos el selector

        // Comprobamos si hay partida guardada
        if (Guardado.instance != null && Guardado.instance.HasSavedGame())
        {
            continueButton.interactable = true; // Activamos botón
            continueInfoText.text = Guardado.instance.GetContinueDetails();
        }
        else
        {
            continueButton.interactable = false; // Bloqueamos botón (se ve gris)
            continueInfoText.text = "Sin datos";
        }
    }

    // 2. ESTO SE PONE EN EL BOTÓN "NUEVA PARTIDA"
    public void Button_NewGame()
    {
        // Borramos cualquier save anterior para empezar de 0
        if (Guardado.instance) Guardado.instance.ClearRunState();
        
        modeSelectionPanel.SetActive(false);
        ResetRun(); // Tu función que resetea todo y empieza
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

    // Lógica interna para cargar los datos
    void LoadRunAndStart()
    {
        // Cargamos datos del disco a la memoria RAM
        daysRemaining = PlayerPrefs.GetInt("Run_Day", totalDaysUntilCure);
        contagionCoins = PlayerPrefs.GetInt("Run_Coins", 0);
        int savedMap = PlayerPrefs.GetInt("Run_Map", 0);
        
        PlayerPrefs.SetInt("CurrentMapIndex", savedMap);
        PlayerPrefs.Save();

        // Iniciamos la sesión sin resetear nada
        StartSession();
    }

    public void OnShinyCaptured()
    {
        shinyCaughtToday = true;
        isShinyCollectedInRun = true;
        Debug.Log("¡Shiny Cazado! Multiplicador x3 activado para hoy.");
    }
}