using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Referencias")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript;

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject shopPanel;
    public GameObject shinyPanel;

    [Header("UI Text")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI contagionCoinsText;
    public TextMeshProUGUI daysRemainingText;
    public TextMeshProUGUI shinyStoreText;

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

    public void ReturnToMenu()
    {
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
        shinyPanel.SetActive(false);
        ShowMainMenu();
    }

    public void ActivateMap(int zoneID) { Debug.Log("Activando zona: " + zoneID); }

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
        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PopulationManager pm = FindObjectOfType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(daysRemaining == shinyDay);

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
        contagionCoins += currentSessionInfected * Guardado.instance.coinMultiplier;
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
    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        virusPlayer.SetActive(false);
    }
}