using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Referencias Importantes")]
    public GameObject virusPlayer; // <--- NUEVA VARIABLE: Arrastra aquí a tu Virus

    [Header("Configuración de Partida")]
    public float gameDuration = 20f;
    private float currentTimer;
    public bool isGameActive = false;
    public int currentSessionInfected = 0;

    [Header("UI Referencias")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI menuTotalScoreText;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        if (isGameActive)
        {
            currentTimer -= Time.deltaTime;
            timerText.text = currentTimer.ToString("F1") + "s";

            if (currentTimer <= 0)
            {
                EndSession();
            }
        }
    }

    public void StartSession()
    {
        // 1. LIMPIEZA TOTAL: Antes de nada, borramos a la gente de la partida anterior
        CleanUpScene(); 

        isGameActive = true;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        UpdateSessionUI();
        
        menuPanel.SetActive(false);
        gameUI.SetActive(true);

        if (virusPlayer != null) 
        {
            virusPlayer.transform.position = Vector3.zero; 
            virusPlayer.SetActive(true);
        }
    }

    // --- NUEVA FUNCIÓN DE LIMPIEZA ---
    void CleanUpScene()
    {
        // Buscamos a TODOS los objetos con el Tag "Persona"
        GameObject[] survivors = GameObject.FindGameObjectsWithTag("Persona");

        // Los destruimos uno a uno
        foreach (GameObject person in survivors)
        {
            Destroy(person);
        }
    }

    void EndSession()
    {
        isGameActive = false;

        if (Guardado.instance != null)
        {
            Guardado.instance.AddTotalData(currentSessionInfected);
        }

        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        gameUI.SetActive(false);
        menuPanel.SetActive(true);

        // Apagamos al jugador usando la referencia directa
        if (virusPlayer != null) virusPlayer.SetActive(false);

        if (Guardado.instance != null)
        {
            menuTotalScoreText.text = "TOTAL INFECTADOS: " + Guardado.instance.totalInfected;
        }
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