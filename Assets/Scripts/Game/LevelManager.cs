using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Referencias")]
    public GameObject virusPlayer;

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject shopPanel;

    [Header("UI Text")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI contagionCoinsText;
    public TextMeshProUGUI daysRemainingText;

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int maxInfectionsPerRound = 5;
    public int totalDaysUntilCure = 5;

    public bool isGameActive;

    public int currentSessionInfected;
    public int contagionCoins;

    private float currentTimer;
    private int daysRemaining;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start()
    {
        ResetDays();
        ShowMainMenu();
    }

    void ResetDays()
    {
        daysRemaining = totalDaysUntilCure;
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;
        timerText.text = currentTimer.ToString("F1") + "s";

        if (currentTimer <= 0)
            EndSession();
    }

    // ------------------ GAME FLOW ------------------

    public void StartSession()
    {
        CleanUpScene();

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        UpdateUI();

        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
        gameUI.SetActive(true);

        virusPlayer.SetActive(true);
    }

    void EndSession()
    {
        isGameActive = false;

        contagionCoins += currentSessionInfected;
        DecreaseDay();

        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);
        shopPanel.SetActive(false);

        virusPlayer.SetActive(false);

        UpdateUI();
    }

    void DecreaseDay()
    {
        daysRemaining--;
        if (daysRemaining < 0) daysRemaining = 0;
    }

    public void RestartDay()
    {
        StartSession();
    }

    public void ResetRun()
    {
        contagionCoins = 0;
        ResetDays();

        VirusRadiusController.instance.ResetUpgrade();
        CapacityUpgradeController.instance.ResetUpgrade();
        SpeedUpgradeController.instance.ResetUpgrade();
        TimeUpgradeController.instance.ResetUpgrade();
        InfectionSpeedUpgradeController.instance.ResetUpgrade();

        StartSession();
    }


    // ------------------ SHOP ------------------

    public void OpenShop()
    {
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        gameOverPanel.SetActive(true);
    }

    // ------------------ INFECTION ------------------

    public void RegisterInfection()
    {
        if (!isGameActive) return;
        if (currentSessionInfected >= maxInfectionsPerRound) return;

        currentSessionInfected++;
        UpdateUI();

        if (currentSessionInfected >= maxInfectionsPerRound)
            EndSession();
    }

    // ------------------ UI ------------------

    public void UpdateUI()
    {
        sessionScoreText.text = "Hoy: " + currentSessionInfected + " / " + maxInfectionsPerRound;
        contagionCoinsText.text = "Monedas: " + contagionCoins;
        daysRemainingText.text = "Quedan " + daysRemaining + " días";
    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);

        virusPlayer.SetActive(false);
    }

    // ------------------ CLEAN ------------------

    void CleanUpScene()
    {
        GameObject[] survivors = GameObject.FindGameObjectsWithTag("Persona");
        foreach (GameObject person in survivors)
            Destroy(person);
    }
}
