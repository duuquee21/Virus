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
    public GameObject shinyPanel; // Tu panel de habilidades/tienda
    public GameObject zonePanel;
    public GameObject pausePanel;

    [Header("UI Text (Listas)")]
    public List<TextMeshProUGUI> timerTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> sessionScoreTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> contagionCoinsTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> daysRemainingTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> shinyStoreTexts = new List<TextMeshProUGUI>(); // Para mostrar monedas en la tienda

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int maxInfectionsPerRound = 5;
    public int baseDaysUntilCure = 5;
    public int totalDaysUntilCure = 5;

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
        float volumenGuardado = PlayerPrefs.GetFloat("VolumenGlobal", 1f);
        AudioListener.volume = volumenGuardado;

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ForceHardReset();
        RecalculateTotalDaysUntilCure();
        ShowMainMenu();
    }

    public void AddCoins(int amount)
    {
        contagionCoins += amount;
        UpdateUI();
    }

    public void RegisterInfection()
    {
        if (!isGameActive || currentSessionInfected >= maxInfectionsPerRound) return;
        currentSessionInfected++;
        UpdateUI();
        if (currentSessionInfected >= maxInfectionsPerRound) EndSessionDay();
    }

    // --- SISTEMA DE MENÚ ---
    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (shinyPanel) shinyPanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);

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

    public void Button_Continue()
    {
        LoadRunAndStart();
    }

    void LoadRunAndStart()
    {
        daysRemaining = PlayerPrefs.GetInt("Run_Day", totalDaysUntilCure);
        contagionCoins = PlayerPrefs.GetInt("Run_Coins", 0);
        int savedMap = PlayerPrefs.GetInt("Run_Map", 0);

        PlayerPrefs.SetInt("CurrentMapIndex", savedMap);
        PlayerPrefs.Save();

        menuPanel.SetActive(false);
        gameUI.SetActive(false);
        dayOverPanel.SetActive(true);
        UpdateUI();
    }

    public void NewGameFromMainMenu()
    {
        ResetRunData();
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameUI.SetActive(false);
        dayOverPanel.SetActive(true);
        UpdateUI();
    }

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

    // --- FUNCIONES DE BOTONES DE PANEL ---
    public void OpenShinyShop() { if (shinyPanel) shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { if (shinyPanel) shinyPanel.SetActive(false); UpdateUI(); }
    public void OpenZoneShop() { if (zonePanel) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel) zonePanel.SetActive(false); }

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
        if (pausePanel != null) pausePanel.SetActive(false);
        if (shinyPanel) shinyPanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);

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
        RecalculateTotalDaysUntilCure();
        ResetDays();

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

    public void StartSession()
    {
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (menuPanel) menuPanel.SetActive(false);

        Time.timeScale = 1f;

        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToGameMusic();

        CleanUpScene();

        int savedMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        ActivateMap(savedMap);

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(0);

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

        if (EndDayResultsPanel.instance != null)
        {
            EndDayResultsPanel.instance.ShowResults(currentSessionInfected, baseMultiplier, zoneMultiplier, 0, 1);
        }

        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);
        daysRemaining--;
    }

    public void OnEndDayResultsFinished(int earnings, int dummy)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        contagionCoins += earnings;

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

        // Mantenemos esto para que los paneles de tienda no se queden vacíos
        foreach (var t in shinyStoreTexts) if (t != null) t.text = "Monedas: " + contagionCoins;
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

    // --- FUNCIONES MANTENIDAS PARA EVITAR ERRORES DE MISSING EN BOTONES ---
    public void RegisterShinyCapture(PersonaInfeccion p) { /* Ya no hay shinies */ }
    public int GetStockRestante(int mapIndex) => 0;
    public void ActualizarStockPorCompraHabilidad() { /* Ya no hay stock */ }

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