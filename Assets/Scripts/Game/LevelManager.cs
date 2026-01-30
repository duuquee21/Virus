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
    public GameObject zonePanel;

    [Header("Tutorial / Diálogo")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    [TextArea] public string[] introLines;
    private int dialogueIndex;

    [Header("UI Text")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI contagionCoinsText;
    public TextMeshProUGUI daysRemainingText;
    public TextMeshProUGUI shinyStoreText;

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int maxInfectionsPerRound = 5;
    public int totalDaysUntilCure = 5;

    [Header("Economía")]
    public int coinsPerInfection = 1;

    [Header("LogicaShiny")]
    public int shinyDay;
    public bool isShinyCollectedInRun = false;
    public GameObject indicadorMejoraVerde;

    public bool isGameActive;

    public int currentSessionInfected;
    public int contagionCoins;

    float currentTimer;
    int daysRemaining;

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
#if UNITY_EDITOR
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
#endif

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

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

    // -------------------- TUTORIAL --------------------

    public void TryStartGame()
    {
        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
            StartTutorial();
        else
            ResetRun();
    }

    void StartTutorial()
    {
        menuPanel.SetActive(false);
        dialoguePanel.SetActive(true);

        virusPlayer.SetActive(true);
        virusPlayer.transform.position = Vector3.zero;
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        dialogueIndex = 0;
        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (dialogueIndex < introLines.Length)
        {
            dialogueText.text = introLines[dialogueIndex];
            dialogueIndex++;
        }
        else
            EndTutorial();
    }

    void EndTutorial()
    {
        dialoguePanel.SetActive(false);

        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        if (virusMovementScript != null) virusMovementScript.enabled = true;

        ResetRun();
    }

    // -------------------- RUN --------------------

    public void StartSession()
    {
        CleanUpScene();

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        PopulationManager populationManager = FindObjectOfType<PopulationManager>();
        if (populationManager != null)
        {
            if (daysRemaining == shinyDay)
                populationManager.ConfigureRound(true);
            else
                populationManager.ConfigureRound(false);
        }

        UpdateUI();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
        shinyPanel.SetActive(false);
        gameUI.SetActive(true);

        virusPlayer.SetActive(true);
        if (virusMovementScript != null) virusMovementScript.enabled = true;
    }

    public void ActivateMap(int zoneID)
    {
        // De momento no hace nada, solo mantiene compatibilidad
    }


    void EndSession()
    {
        isGameActive = false;

        contagionCoins += currentSessionInfected * Guardado.instance.coinMultiplier;

        DecreaseDay();

        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);
        shopPanel.SetActive(false);
        shinyPanel.SetActive(false);

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
        ResetDays();
        isShinyCollectedInRun = false;

        shinyDay = Random.Range(1, totalDaysUntilCure + 1);

        // reset upgrades normales
        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();

        // reaplicar bonus permanente random
        if (Guardado.instance)
            Guardado.instance.ApplyPermanentInitialUpgrade();

        RecalculateCoinsPerInfection();

        // 👉 monedas iniciales del árbol
        contagionCoins = Guardado.instance.startingCoins;

        StartSession();
    }

    // -------------------- ECONOMÍA --------------------

    public void RecalculateCoinsPerInfection()
    {
        coinsPerInfection = Guardado.instance.coinMultiplier;
    }

    // -------------------- SHOP --------------------

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

    public void CloseShiny()
    {
        shinyPanel.SetActive(false);
        gameOverPanel.SetActive(true);
    }

    public void ShinyShop()
    {
        gameOverPanel.SetActive(false);
        shinyPanel.SetActive(true);
        UpdateUI();
    }

    // -------------------- INFECCIÓN --------------------

    public void RegisterInfection()
    {
        if (!isGameActive) return;
        if (currentSessionInfected >= maxInfectionsPerRound) return;

        currentSessionInfected++;
        UpdateUI();

        if (currentSessionInfected >= maxInfectionsPerRound)
            EndSession();
    }

    // -------------------- UI --------------------

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
        shopPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        virusPlayer.SetActive(false);
    }

    // -------------------- CLEAN --------------------

    void CleanUpScene()
    {
        GameObject[] survivors = GameObject.FindGameObjectsWithTag("Persona");
        foreach (GameObject person in survivors)
            Destroy(person);
    }
}
