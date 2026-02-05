using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

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

        // Reseteamos niveles de tienda de la RUN, pero NO el metaprogreso
        ForceHardReset();

        RecalculateTotalDaysUntilCure();
        ResetDays();
        ShowMainMenu();
    }

    void ForceHardReset()
    {
        // Solo resetea los niveles de las tiendas de la partida actual
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
            EndSessionDay();
        }
    }

    // --- MÉTODOS DE NAVEGACIÓN ---
    public void OpenShinyShop() { shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); UpdateUI(); }
    public void OpenZoneShop() { if (zonePanel != null) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); }

    public void NewGameFromMainMenu()
    {
        ResetRunData();

        // Desactivamos los paneles que NO queremos ver
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false); // <--- AÑADE ESTA LÍNEA AQUÍ

        // Activamos el panel de la tienda/preparación para la nueva run
        dayOverPanel.SetActive(true);

        UpdateUI();
    }
    public void ReturnToMenu()
    {
        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();
        gameOverPanel.SetActive(false);
        dayOverPanel.SetActive(false);
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

    // --- CORRECCIÓN CLAVE AQUÍ ---
    void ResetRunData()
    {
        RecalculateTotalDaysUntilCure();
        ResetDays();

        // --- LÓGICA DE LIMPIEZA DE ZONAS ---
        // Si NO tenemos la habilidad de persistencia, borramos las zonas compradas
        bool tieneHabilidadMeta = Guardado.instance != null && Guardado.instance.keepZonesUnlocked;

        if (!tieneHabilidadMeta)
        {
            Debug.Log("<color=red>Sin habilidad: Reseteando zonas compradas.</color>");
            for (int i = 1; i <= 10; i++)
            {
                PlayerPrefs.SetInt("ZoneUnlocked_" + i, 0);
            }
        }
        else
        {
            Debug.Log("<color=green>Habilidad activa: Se mantienen las zonas.</color>");
        }

        // Reseteamos el mapa actual al inicial
        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();
        ActivateMap(0);

        // Reseteo de mejoras de la tienda (si no tiene la habilidad de mantener mejoras)
        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset)
        {
            ForceHardReset();
        }

        if (Guardado.instance)
            Guardado.instance.ApplyPermanentInitialUpgrade();

        contagionCoins = Guardado.instance != null ? Guardado.instance.startingCoins : 0;
        UpdateUI();
    }
    // Para quitarte el error del MainMenuPanel:
    public void TryStartGame()
    {
        NewGameFromMainMenu();
    }

    public void StartSession()
    {
        dayOverPanel.SetActive(false);

        // Ingresos pasivos por zonas desbloqueadas
        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;

            if (Guardado.instance.shinyPerZoneDaily > 0)
                Guardado.instance.AddShinyDNA(numeroZonas * Guardado.instance.shinyPerZoneDaily);
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToGameMusic();

        // Preparar sesión
        shinysThisDay.Clear();
        shiniesCapturedToday = 0;

        // --- LÓGICA ACUMULATIVA: Si sale el 75%, sumamos el base + extras ---
        float probabilidadActual = (Guardado.instance != null && Guardado.instance.guaranteedShiny) ? 1.0f : shinyChance;

        if (Random.value <= probabilidadActual)
        {
            isShinyDayToday = true;

            // Empezamos con 1 (el premio base por ganar el sorteo)
            int cantidadFinal = 1;

            // Sumamos el valor de la variable extra (que ahora será un número: 1, 2, 3...)
            if (Guardado.instance != null)
            {
                cantidadFinal += Guardado.instance.extraShiniesPerRound;
            }

            shiniesToSpawnToday = cantidadFinal;
        }
        else
        {
            isShinyDayToday = false;
            shiniesToSpawnToday = 0; // Si falla el 75%, no sale ninguno
        }
        // ------------------------------------------------------------------

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

        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();
        gameUI.SetActive(false);
        virusPlayer.SetActive(false);

        if (daysRemaining <= 0)
        {
            daysRemaining = 0;
            GameOver();
        }
        else
        {
            dayOverPanel.SetActive(true);
            // Guardar estado de la Run
            if (Guardado.instance) Guardado.instance.SaveRunState(daysRemaining, contagionCoins, mapIndex);
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
        ShowMainMenu();
    }

    void CleanUpScene()
    {
        PersonaInfeccion[] gente = Object.FindObjectsByType<PersonaInfeccion>(FindObjectsSortMode.None);
        foreach (PersonaInfeccion p in gente) if (p != null) Destroy(p.gameObject);
    }

    // --- CORRECCIÓN DE LLAVES AQUÍ ---
    public int GetTotalUnlockedZones()
    {
        int count = 1; // La zona 0 inicial
        for (int i = 1; i <= 10; i++)
        {
            // Usamos el mismo nombre que en ZoneItem: "ZoneUnlocked_X"
            if (PlayerPrefs.GetInt("ZoneUnlocked_" + i, 0) == 1) count++;
        }
        return count;
    }

    public void OnEndDayResultsFinished(int earnings, int shinies)
    {
        contagionCoins += earnings;
        if (Guardado.instance != null) Guardado.instance.AddShinyDNA(shinies);

        if (daysRemaining <= 0) GameOver();
        else dayOverPanel.SetActive(true);

        UpdateUI();
    }

    public void RegisterShinyCapture(PersonaInfeccion shiny)
    {
        if (shiny == null || !shinysThisDay.Contains(shiny)) return;
        shiniesCapturedToday++;
        shinysThisDay.Remove(shiny);
        int cantidadFinal = Guardado.instance != null ? Guardado.instance.GetFinalShinyValue() : 1;
        Guardado.instance.AddShinyDNA(cantidadFinal);
        UpdateUI();
    }
}