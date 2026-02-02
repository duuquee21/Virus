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

    [HideInInspector] public bool isGameActive;
    [HideInInspector] public int currentSessionInfected;
    [HideInInspector] public int contagionCoins;
    [HideInInspector] public bool isShinyCollectedInRun = false;

    float currentTimer;
    int daysRemaining;
    public int shinyDay;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void Start()
    {
        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        RecalculateTotalDaysUntilCure();
        ResetDays();
        ShowMainMenu();
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

    // --- NAVEGACIÓN DE PANELES ---
    public void TryStartGame() { ResetRun(); }

    public void OpenShop() { gameOverPanel.SetActive(false); shopPanel.SetActive(true); }
    public void CloseShop() { shopPanel.SetActive(false); gameOverPanel.SetActive(true); }

    public void OpenShinyShop() { gameOverPanel.SetActive(false); shinyPanel.SetActive(true); UpdateUI(); }
    public void CloseShinyShop() { shinyPanel.SetActive(false); gameOverPanel.SetActive(true); }
    public void OpenZoneShop() { gameOverPanel.SetActive(false); if(zonePanel != null) zonePanel.SetActive(true); UpdateUI(); 

    }
    
    public void CloseZoneShop() { if(zonePanel != null) zonePanel.SetActive(false); gameOverPanel.SetActive(true); }
    public void ReturnToMenu()
    {
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
        shinyPanel.SetActive(false);
        ShowMainMenu();
    }

    public void ActivateMap(int zoneID) 
    { 
        Debug.Log("Activando zona: " + zoneID);
        
        
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

    // --- GAMEPLAY ---
    public void ResetRun()
    {
        RecalculateTotalDaysUntilCure();
        ResetDays();
        isShinyCollectedInRun = false;
        shinyDay = Random.Range(1, totalDaysUntilCure + 1);

        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();

        if (Guardado.instance) Guardado.instance.ApplyPermanentInitialUpgrade();

        contagionCoins = Guardado.instance.startingCoins;
        StartSession();
    }

    public void StartSession()
    {
        // 1. CÁLCULO DE INGRESOS PASIVOS (Monedas y ADN Shiny)
        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();

            // Ingreso de Monedas normales
            if (Guardado.instance.coinsPerZoneDaily > 0)
            {
                int totalMonedas = numeroZonas * Guardado.instance.coinsPerZoneDaily;
                contagionCoins += totalMonedas;
                Debug.Log($"<color=green>Ingreso diario:</color> +{totalMonedas} monedas");
            }

            // --- NUEVO: Ingreso de ADN Shiny ---
            if (Guardado.instance.shinyPerZoneDaily > 0)
            {
                int totalShiny = numeroZonas * Guardado.instance.shinyPerZoneDaily;
                Guardado.instance.AddShinyDNA(totalShiny); // Sumamos directamente al guardado permanente
                Debug.Log($"<color=magenta>ADN Shiny pasivo:</color> +{totalShiny}");
            }
        }

        // 2. PREPARACIÓN DE LA ESCENA
        CleanUpScene();

        // Cargar el mapa guardado
        int savedMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        ActivateMap(savedMap);

        // 3. INICIO DE VARIABLES DE SESIÓN
        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        // Configurar la probabilidad de Shiny para la población
        PopulationManager pm = FindObjectOfType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(daysRemaining == shinyDay);

        // 4. ACTIVACIÓN DE INTERFAZ Y JUGADOR
        UpdateUI();
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameUI.SetActive(true);
        virusPlayer.SetActive(true);

        if (virusMovementScript != null)
            virusMovementScript.enabled = true;
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
        int zoneMultiplier = 1; 

        // 2. APLICAR BONUS SEGÚN EL MAPA
        if (mapIndex == 1) // Zona Roja
        {
            zoneMultiplier = 2; 
        }
        else if (mapIndex == 2) 
        {
            zoneMultiplier = 3; 
        }

        // 3. CALCULAR EL TOTAL (Infectados * Mejoras * Bonus de Zona)
        int earnings = currentSessionInfected * Guardado.instance.coinMultiplier * zoneMultiplier;
        
      
        contagionCoins += earnings;
        
        
        if (Guardado.instance != null)
        {
            Guardado.instance.AddTotalData(currentSessionInfected);
        }

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
        {
           
            zoneCurrencyText.text = "Tienes: " + contagionCoins + " Monedas";
        }
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
        
        PersonaInfeccion[] genteEnPantalla = FindObjectsOfType<PersonaInfeccion>();

        foreach (PersonaInfeccion persona in genteEnPantalla)
        {
            if (persona != null)
            {
                Destroy(persona.gameObject); 
            }
        }
    }

    public int GetTotalUnlockedZones()
    {
        // Esto asume que tienes un sistema de guardado para las zonas.
        // Si no, podemos contar cuántos ZoneItems tienen 'unlocked' en true.
        int count = 0;
        // Por ahora, como mínimo siempre tienes 1 (la inicial)
        count = 1;

        // Aquí deberías chequear tus PlayerPrefs de zonas:
        if (PlayerPrefs.GetInt("Zone_1_Unlocked", 0) == 1) count++;
        if (PlayerPrefs.GetInt("Zone_2_Unlocked", 0) == 1) count++;

        return count;
    }
}
