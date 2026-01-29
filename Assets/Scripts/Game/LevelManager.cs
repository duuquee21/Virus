using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Referencias")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript; // <--- Referencia al script de movimiento (para congelarlo)

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject shopPanel;
    public GameObject shinyPanel;

    // --- NUEVO: REFERENCIAS DEL TUTORIAL ---
    [Header("Tutorial / Diálogo")]
    public GameObject dialoguePanel; 
    public TextMeshProUGUI dialogueText;
    [TextArea] public string[] introLines;
    private int dialogueIndex;
    // ---------------------------------------

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

    [Header("LogicaShiny")] 
    public int shinyDay;
    public bool isShinyCollectedInRun = false;
    public TextMeshProUGUI shinyStoreText;
    
    public GameObject indicadorMejoraVerde;

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
        PlayerPrefs.DeleteAll();   // Limpia progreso SOLO al probar en Unity
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

    // ------------------ LOGICA DEL TUTORIAL (NUEVO) ------------------

    // 1. ESTA ES LA FUNCIÓN QUE PONES AHORA EN EL BOTÓN "JUGAR" DEL MENÚ
    public void TryStartGame()
    {
        // Si no ha visto el tutorial (valor 0), lo iniciamos
        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
        {
            StartTutorial();
        }
        else
        {
            // Si ya lo vio, empieza la run normal (ResetRun limpia todo y empieza)
            ResetRun();
        }
    }

    void StartTutorial()
    {
        menuPanel.SetActive(false); // Ocultar menú
        dialoguePanel.SetActive(true); // Mostrar bocadillo

        // Mostramos al jugador para que "hable", pero lo congelamos
        virusPlayer.SetActive(true);
        virusPlayer.transform.position = Vector3.zero;
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        dialogueIndex = 0;
        ShowNextLine();
    }

    // Pon esta función en el botón invisible del bocadillo
    public void ShowNextLine()
    {
        if (dialogueIndex < introLines.Length)
        {
            dialogueText.text = introLines[dialogueIndex];
            dialogueIndex++;
        }
        else
        {
            EndTutorial();
        }
    }

    void EndTutorial()
    {
        dialoguePanel.SetActive(false);
        
        // Marcamos tutorial como visto
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        // Descongelamos movimiento
        if (virusMovementScript != null) virusMovementScript.enabled = true;

        // EMPEZAMOS LA RUN REAL
        ResetRun();
    }

    // ------------------ GAME FLOW ORIGINAL ------------------

    public void StartSession()
    {
        CleanUpScene();

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;
        
        //logica shiny;
        
        PopulationManager populationManager = FindObjectOfType<PopulationManager>();

        if (populationManager != null)
        {
            if (daysRemaining == shinyDay)
            {
                populationManager.ConfigureRound(true);
            }
            else
            {
                populationManager.ConfigureRound(false);
            }
        }
        
        
        if (PlayerPrefs.GetInt("ShinyLuck", 0) > 0)
        {
            // Si es mayor que 0, está comprada -> ENCENDEMOS el cuadrado
            if (indicadorMejoraVerde != null) indicadorMejoraVerde.SetActive(true);
        }
        else
        {
            // Si es 0, no la tiene -> APAGAMOS el cuadrado
            if (indicadorMejoraVerde != null) indicadorMejoraVerde.SetActive(false);
        }
        // ------------------------------------------------------------------

        UpdateUI();
        
        

        // Aseguramos que el tutorial no estorbe
        if(dialoguePanel != null) dialoguePanel.SetActive(false);

        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
        shinyPanel.SetActive(false);
        gameUI.SetActive(true);

        virusPlayer.SetActive(true);
        // Aseguramos que se pueda mover al empezar la sesión
        if (virusMovementScript != null) virusMovementScript.enabled = true;
        
       
    }

    void EndSession()
    {
        isGameActive = false;

        contagionCoins += currentSessionInfected;
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
        contagionCoins = 0;
        ResetDays();
        isShinyCollectedInRun = false;
        
        //dia con shiny aleatorio
        
        shinyDay = Random.Range(1, totalDaysUntilCure+ 1);

        // NOTA: Asegúrate de que estos singletons existan en tu escena o dará error
        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();

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

        // --- ESTO ES LO QUE FALTABA AQUÍ ---
        if (shinyStoreText != null && Guardado.instance != null)
        {
            shinyStoreText.text = "ADN Shiny: " + Guardado.instance.shinyDNA;
        }
    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        shopPanel.SetActive(false);
        if(dialoguePanel != null) dialoguePanel.SetActive(false); // Ocultar dialogo por si acaso

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