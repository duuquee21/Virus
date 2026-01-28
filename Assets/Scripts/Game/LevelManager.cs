using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Referencias Importantes")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript; // Referencia al script de movimiento para bloquearlo

    [Header("UI Referencias")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject gameOverPanel;
    
    // --- NUEVO: UI DIÁLOGO ---
    [Header("Configuración Diálogo")]
    public GameObject dialoguePanel; // El panel del bocadillo
    public TextMeshProUGUI dialogueText; // El texto dentro del bocadillo
    [TextArea] public string[] introLines; // Aquí escribiremos las frases en el inspector
    private int dialogueIndex; // Para saber por qué frase vamos

    [Header("Textos UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI menuTotalScoreText;

    // Variables de lógica
    public float gameDuration = 20f;
    private float currentTimer;
    public bool isGameActive = false;
    public int currentSessionInfected = 0;

    void Awake() 
    { 
        if (instance == null) instance = this; 
    }

    void Start() 
    { 
        // Intentamos obtener el script de movimiento automáticamente si no lo asignas
        if(virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ShowMainMenu(); 
    }

    void Update()
    {
        if (isGameActive)
        {
            currentTimer -= Time.deltaTime;
            timerText.text = currentTimer.ToString("F1") + "s";
            if (currentTimer <= 0) EndSession();
        }
    }

    // --- NUEVA FUNCIÓN: INTENTO DE INICIO ---
    // ESTA ES LA QUE PONDREMOS EN EL BOTÓN JUGAR AHORA
    public void TryStartGame()
    {
        // 0 = No visto, 1 = Visto
        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
        {
            StartTutorial();
        }
        else
        {
            // Si ya lo vio, arranca normal
            StartSession();
        }
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
            if (virusMovementScript != null) virusMovementScript.enabled = false; // ¡Quieto!
        }

        dialogueIndex = 0;
        ShowNextLine();
    }

    public void ShowNextLine()
    {
        // frases
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
        
        // tutorial visto
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        
        if (virusMovementScript != null) virusMovementScript.enabled = true;
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
        dialoguePanel.SetActive(false); // Aseguramos que se quite
        gameUI.SetActive(true);

        if (virusPlayer != null) 
        {
            virusPlayer.SetActive(true);
            // Aseguramos que se pueda mover
            if (virusMovementScript != null) virusMovementScript.enabled = true;
        }
    }

    // ... RESTO DE TUS FUNCIONES (EndSession, Botones, etc) IGUAL QUE ANTES ...
    void EndSession()
    {
        isGameActive = false;
        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);
        
        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);
        if (virusPlayer != null) virusPlayer.SetActive(false);
    }
    
    public void Button_Restart() { StartSession(); } // Reiniciar no repite tutorial
    
    public void Button_GoToMenu() { 
        gameOverPanel.SetActive(false);
        ShowMainMenu(); 
    }

    void ShowMainMenu()
    {
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false); 
        dialoguePanel.SetActive(false);
        menuPanel.SetActive(true);
        
        if (virusPlayer != null) virusPlayer.SetActive(false);

        if (Guardado.instance != null)
            menuTotalScoreText.text = "TOTAL INFECTADOS: " + Guardado.instance.totalInfected;
    }

    void CleanUpScene()
    {
        GameObject[] survivors = GameObject.FindGameObjectsWithTag("Persona");
        foreach (GameObject person in survivors) Destroy(person);
    }

    public void RegisterInfection()
    {
        if (!isGameActive) return;
        currentSessionInfected++;
        UpdateSessionUI();
    }

    void UpdateSessionUI()
    {
        sessionScoreText.text = "Infectados: " + currentSessionInfected;
    }
}