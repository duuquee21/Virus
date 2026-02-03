using UnityEngine;
using TMPro;

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
    public GameObject gameOverPanel;
    public GameObject shopPanel;
    public GameObject shinyPanel;
    public GameObject zonePanel;

    [Header("UI Text")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI contagionCoinsText;
    public TextMeshProUGUI daysRemainingText;
    public TextMeshProUGUI shinyStoreText;
    public TextMeshProUGUI zoneCurrencyText;

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int maxInfectionsPerRound = 5;
    public int baseDaysUntilCure = 5;
    public int totalDaysUntilCure = 5;

    [Header("Configuración Shiny (75% Chance)")]
    public float shinyChance = 0.75f;
    private bool shinyAlreadySpawnedInRun = false;
    // CAMBIO: Ahora usamos un int para saber cuántos Shinies crear hoy
    private int shiniesToSpawnToday = 0;

    [HideInInspector] public bool isGameActive;
    [HideInInspector] public int currentSessionInfected;
    [HideInInspector] public int contagionCoins;
    [HideInInspector] public bool isShinyCollectedInRun = false;

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
        timerText.text = currentTimer.ToString("F1") + "s";
        if (currentTimer <= 0) EndSession();
    }

    public void TryStartGame() { ResetRun(); }
    public void OpenShop() { gameOverPanel.SetActive(false); shopPanel.SetActive(true); }
    public void CloseShop() { shopPanel.SetActive(false); gameOverPanel.SetActive(true); }
    public void OpenShinyShop() { gameOverPanel.SetActive(false); shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); gameOverPanel.SetActive(true); }
    public void OpenZoneShop() { gameOverPanel.SetActive(false); if (zonePanel != null) zonePanel.SetActive(true); UpdateUI(); }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); gameOverPanel.SetActive(true); }

    public void ReturnToMenu()
    {
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
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

    public void ResetRun()
    {
        RecalculateTotalDaysUntilCure();
        ResetDays();

        isShinyCollectedInRun = false;
        shinyAlreadySpawnedInRun = false;
        // CAMBIO: Reseteamos la cuenta de shinies
        shiniesToSpawnToday = 0;

        if (Guardado.instance == null || !Guardado.instance.keepUpgradesOnReset)
        {
            if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
            if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
            if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
            if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
            if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();

            Debug.Log("<color=red>Run Reset:</color> Niveles reiniciados al nivel 1 por falta de habilidad de persistencia.");
        }
        else
        {
            Debug.Log("<color=green>Habilidad Activa:</color> Se mantienen los niveles de la tienda.");
        }

        if (Guardado.instance) Guardado.instance.ApplyPermanentInitialUpgrade();

        contagionCoins = Guardado.instance.startingCoins;
        StartSession();
    }

    public void StartSession()
    {
        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;

            if (Guardado.instance.shinyPerZoneDaily > 0)
                Guardado.instance.AddShinyDNA(numeroZonas * Guardado.instance.shinyPerZoneDaily);
        }

        // --- LÓGICA DE DOBLE SHINY ---
        shiniesToSpawnToday = 0;
        if (!shinyAlreadySpawnedInRun)
        {
            float probabilidadActual = (Guardado.instance != null && Guardado.instance.guaranteedShiny) ? 1.0f : shinyChance;

            if (Random.value <= probabilidadActual)
            {
                // Si tiene la habilidad de doble shiny comprada
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
        // CAMBIO: Ahora pasamos el número exacto de shinies (0, 1 o 2)
        if (pm != null) pm.ConfigureRound(shiniesToSpawnToday);

        UpdateUI();
        menuPanel.SetActive(false);
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
        if (currentSessionInfected >= maxInfectionsPerRound) EndSession();
    }

    void EndSession()
    {
        isGameActive = false;
        int mapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int zoneMultiplier = (mapIndex == 1) ? 2 : (mapIndex == 2) ? 3 : 1;

        int earnings = currentSessionInfected * (Guardado.instance != null ? Guardado.instance.coinMultiplier : 1) * zoneMultiplier;
        contagionCoins += earnings;

        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);

        daysRemaining--;
        if (daysRemaining < 0) daysRemaining = 0;

        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);
        virusPlayer.SetActive(false);
        UpdateUI();
    }

    public void UpdateUI()
    {
        sessionScoreText.text = "Hoy: " + currentSessionInfected + " / " + maxInfectionsPerRound;
        contagionCoinsText.text = "Monedas: " + contagionCoins;
        daysRemainingText.text = "Quedan " + daysRemaining + " días";
        if (shinyStoreText != null && Guardado.instance != null)
            shinyStoreText.text = "ADN Shiny: " + Guardado.instance.shinyDNA;
        if (zoneCurrencyText != null)
            zoneCurrencyText.text = "Tienes: " + contagionCoins + " Monedas";
    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        virusPlayer.SetActive(false);
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
        return count;
    }
}