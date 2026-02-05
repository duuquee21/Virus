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

    // --- NUEVO: SISTEMA DE STOCK POR ZONA ---
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

        if (currentTimer <= 0) EndSessionDay();
    }

    public void OpenShinyShop() { shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); UpdateUI(); }
    public void OpenZoneShop() { if (zonePanel != null) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); }

    public void NewGameFromMainMenu()
    {
        ResetRunData();
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
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

    // --- NUEVO: INICIALIZAR EL STOCK ---
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

    public void TryStartGame() { NewGameFromMainMenu(); }

    public void StartSession()
    {
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
            // Intentamos sacar 1 + extras, limitado por el stock
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
        int mapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int zoneMultiplier = (mapIndex == 1) ? 2 : (mapIndex == 2) ? 3 : 1;
        int baseMultiplier = Guardado.instance != null ? Guardado.instance.coinMultiplier : 1;
        int sMultiplier = Guardado.instance != null ? Guardado.instance.shinyMultiplier : 1;

        if (EndDayResultsPanel.instance != null)
            EndDayResultsPanel.instance.ShowResults(currentSessionInfected, baseMultiplier, zoneMultiplier, shiniesCapturedToday, sMultiplier);

        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);

        daysRemaining--;
        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();
        gameUI.SetActive(false);
        virusPlayer.SetActive(false);

        if (daysRemaining <= 0) { daysRemaining = 0; GameOver(); }
        else
        {
            dayOverPanel.SetActive(true);
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

        // --- NUEVO: RESTAR DEL STOCK AL CAPTURAR ---
        int indexActual = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        if (stockShiniesZonas.ContainsKey(indexActual) && stockShiniesZonas[indexActual] > 0)
        {
            stockShiniesZonas[indexActual]--;
            Debug.Log($"Stock zona {indexActual} bajó a {stockShiniesZonas[indexActual]}");
        }
        // -------------------------------------------

        shiniesCapturedToday++;
        shinysThisDay.Remove(shiny);
        int cantidadFinal = Guardado.instance != null ? Guardado.instance.GetFinalShinyValue() : 1;
        Guardado.instance.AddShinyDNA(cantidadFinal);
        UpdateUI();
    }

    // Añade esto al final de LevelManager.cs
    public int GetStockRestante(int mapIndex)
    {
        // Si la partida está en curso y el diccionario tiene datos, los devolvemos
        if (stockShiniesZonas != null && stockShiniesZonas.ContainsKey(mapIndex))
        {
            return stockShiniesZonas[mapIndex];
        }

        // Si no ha empezado la sesión, calculamos el valor teórico: Base + Extras
        int extras = (Guardado.instance != null) ? Guardado.instance.extraShiniesPerRound : 0;
        int baseZona = (mapIndex < shiniesBasePorMapa.Length) ? shiniesBasePorMapa[mapIndex] : (mapIndex + 1);

        return baseZona + extras;
    }

    public void ActualizarStockPorCompraHabilidad()
    {
        // 1. Recorremos el diccionario actual de la partida
        // Usamos una lista temporal de llaves para evitar errores de modificación mientras recorremos
        List<int> keys = new List<int>(stockShiniesZonas.Keys);

        foreach (int i in keys)
        {
            stockShiniesZonas[i]++; // Sumamos +1 al stock actual (de 0 a 1, de 2 a 3, etc.)
            Debug.Log($"Habilidad aplicada: Zona {i} ahora tiene {stockShiniesZonas[i]} shinies.");
        }

        // 2. Refrescamos visualmente todos los botones de la tienda de zonas
        ZoneItem[] todosLosBotones = Object.FindObjectsByType<ZoneItem>(FindObjectsSortMode.None);
        foreach (ZoneItem boton in todosLosBotones)
        {
            boton.UpdateUI();
        }

        UpdateUI();
    }
}