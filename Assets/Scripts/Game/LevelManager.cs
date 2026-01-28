using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Referencias Importantes")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript;

    [Header("UI Referencias")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject gameOverPanel;

    [Header("Configuración Diálogo")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    [TextArea] public string[] introLines;
    private int dialogueIndex;

    [Header("Textos UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI menuTotalScoreText;

    [Header("Tiempo de partida")]
    public float gameDuration = 20f;
    private float currentTimer;

    [Header("Límite de contagios por ronda")]
    public int maxInfectionsPerRound = 5;

    public bool isGameActive = false;
    public int currentSessionInfected = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ShowMainMenu();
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;
        timerText.text = currentTimer.ToString("F1") + "s";

        if (currentTimer <= 0)
            EndSession();
    }

    public void TryStartGame()
    {
        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
            StartTutorial();
        else
            StartSession();
    }

    void StartTutorial()
    {
        menuPanel.SetActive(false);
        gameUI.SetActive(false);
        dialoguePanel.SetActive(true);

        if (virusPlayer != null)
        {
            virusPlayer.SetActive(true);
            virusPlayer.transform.position = Vector3.zero;
            if (virusMovementScript != null)
                virusMovementScript.enabled = false;
        }

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
        {
            EndTutorial();
        }
    }

    void EndTutorial()
    {
        dialoguePanel.SetActive(false);
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        if (virusMovementScript != null)
            virusMovementScript.enabled = true;

        StartSession();
    }

    public void StartSession()
    {
        CleanUpScene();

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        UpdateSessionUI();

        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dialoguePanel.SetActive(false);
        gameUI.SetActive(true);

        if (virusPlayer != null)
        {
            virusPlayer.SetActive(true);
            if (virusMovementScript != null)
                virusMovementScript.enabled = true;
        }
    }

    void EndSession()
    {
        isGameActive = false;

        if (Guardado.instance != null)
            Guardado.instance.AddTotalData(currentSessionInfected);

        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);

        if (virusPlayer != null)
            virusPlayer.SetActive(false);
    }

    public void Button_Restart()
    {
        StartSession();
    }

    public void Button_GoToMenu()
    {
        gameOverPanel.SetActive(false);
        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        dialoguePanel.SetActive(false);
        menuPanel.SetActive(true);

        if (virusPlayer != null)
            virusPlayer.SetActive(false);

        if (Guardado.instance != null)
            menuTotalScoreText.text = "TOTAL INFECTADOS: " + Guardado.instance.totalInfected;
    }

    void CleanUpScene()
    {
        GameObject[] survivors = GameObject.FindGameObjectsWithTag("Persona");
        foreach (GameObject person in survivors)
            Destroy(person);
    }

    public void RegisterInfection()
    {
        if (!isGameActive) return;

        if (currentSessionInfected >= maxInfectionsPerRound)
            return;

        currentSessionInfected++;
        UpdateSessionUI();

        // 👉 MISMO FINAL QUE POR TIEMPO
        if (currentSessionInfected >= maxInfectionsPerRound)
            EndSession();
    }

    void UpdateSessionUI()
    {
        sessionScoreText.text = "Infectados: " + currentSessionInfected + " / " + maxInfectionsPerRound;
    }
}
