using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Selector de Modo (Menú Principal)")]
    public GameObject modeSelectionPanel;
    public Button continueButton;
    public TextMeshProUGUI continueInfoText;
    public GameObject jugadorVirus;



    [Header("Sistema de Zonas")]
    public GameObject[] mapList;

    [Header("Referencias")]
    public GameObject virusPlayer;
    public VirusMovement virusMovementScript;
    
    [Header("Feedback Visual Puntos")]
    public GameObject prefabTextoPuntos; 
    public RectTransform marcadorDestinoUI;
    public Canvas canvasPrincipal; 

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameUI;
    public GameObject dayOverPanel;
    public GameObject gameOverPanel;
    public GameObject shinyPanel; // Panel de la Tienda de ADN/Mejoras
    public GameObject zonePanel;  // Panel de Selección de Mapas
    public GameObject pausePanel;

    [Header("UI Text (Listas)")]
    public List<TextMeshProUGUI> timerTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> sessionScoreTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> contagionCoinsTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> shinyStoreTexts = new List<TextMeshProUGUI>(); // Texto para mostrar ADN Shiny

    [Header("Gameplay")]
    public float gameDuration = 20f;
    public int maxInfectionsPerRound = 5;
    [HideInInspector] public int monedasGanadasSesion;


    [Header("Configuración Inicial por Zona")]
    public int[] faseInicialPorMapa;


    [HideInInspector] public bool isGameActive;
    [HideInInspector] public int currentSessionInfected;
    [HideInInspector] public int contagionCoins;

    float currentTimer;

    void Awake()
    {
        
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        float volumenGuardado = PlayerPrefs.GetFloat("VolumenGlobal", 1f);
        AudioListener.volume = volumenGuardado;

        if (virusPlayer != null && virusMovementScript == null)
            virusMovementScript = virusPlayer.GetComponent<VirusMovement>();

        ForceHardReset();
        ShowMainMenu();
    }

    // --- FUNCIONES DE CONTROL DE PANELES (RESTAURADAS) ---
    public void OpenShinyShop() { if (shinyPanel != null) { shinyPanel.SetActive(true); UpdateUI(); } }
    public void CloseShinyShop() { 
        if (shinyPanel != null) 
        shinyPanel.SetActive(false); 
        zonePanel.SetActive(true); // Volvemos al panel de fin de día al cerrar la tienda de ADN/Mejoras

    }

    public void OpenZoneShop() { if (zonePanel != null) { zonePanel.SetActive(true); UpdateUI(); } }
    public void CloseZoneShop() { if (zonePanel != null) zonePanel.SetActive(false); }

    public void AddCoins(int amount)
    {
        contagionCoins += amount;        // Total acumulado
        monedasGanadasSesion += amount;  // Solo esta partida
        UpdateUI();
    }


    public void RegisterInfection()
    {
        if (!isGameActive || currentSessionInfected >= maxInfectionsPerRound) return;
        currentSessionInfected++;
        UpdateUI();
        if (currentSessionInfected >= maxInfectionsPerRound) EndSessionDay();
    }

    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);
        if (shinyPanel) shinyPanel.SetActive(false);

        virusPlayer.SetActive(false);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            if (Guardado.instance != null && Guardado.instance.HasSavedGame())
            {
                continueButton.interactable = true;
                if (continueInfoText)
                {
                    continueInfoText.text = Guardado.instance.GetContinueDetails();
                    continueInfoText.alpha = 1f;
                }
            }
            else
            {
                continueButton.interactable = false;
                if (continueInfoText)
                {
                    continueInfoText.text = "Sin datos";
                    continueInfoText.alpha = 0.5f;
                }
            }
        }
    }

    public void Button_NewGame()
    {
        if (Guardado.instance) Guardado.instance.ResetAllProgress();
        NewGameFromMainMenu();
    }

    public void Button_Continue()
    {
        LoadRunAndStart();
    }

    void LoadRunAndStart()
    {
        contagionCoins = PlayerPrefs.GetInt("Run_Coins", 0);
        int savedMap = PlayerPrefs.GetInt("Run_Map", 0);

        PlayerPrefs.SetInt("CurrentMapIndex", savedMap);
        PlayerPrefs.Save();

        menuPanel.SetActive(false);
        gameUI.SetActive(false);
        dayOverPanel.SetActive(true);
        UpdateUI();
    }

    public void NewGameFromMainMenu()
    {
        ResetRunData();
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameUI.SetActive(false);
        dayOverPanel.SetActive(true);
        UpdateUI();
    }

    void ForceHardReset()
    {
        if (VirusRadiusController.instance) VirusRadiusController.instance.ResetUpgrade();
        if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.ResetUpgrade();
        if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.ResetUpgrade();
        if (TimeUpgradeController.instance) TimeUpgradeController.instance.ResetUpgrade();
        if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.ResetUpgrade();
    }

    void Update()
    {
        if (!isGameActive) return;
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();

        currentTimer -= Time.deltaTime;
        foreach (var t in timerTexts)
        {
            if (t != null) t.text = currentTimer.ToString("F1") + "s";
        }

        if (currentTimer <= 0) EndSessionDay();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (Guardado.instance)
        {
            int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
            Guardado.instance.SaveRunState(0, contagionCoins, currentMap);
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

        gameOverPanel.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (shinyPanel != null) shinyPanel.SetActive(false);

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

        PopulationManager popManager = Object.FindFirstObjectByType<PopulationManager>();
        if (popManager != null) popManager.SelectPrefab(zoneID);
    }

    void ResetRunData()
    {
        bool tieneHabilidadMeta = Guardado.instance != null && Guardado.instance.keepZonesUnlocked;
        if (!tieneHabilidadMeta)
        {
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

    public void StartSession()
    {
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (menuPanel) menuPanel.SetActive(false);

        Time.timeScale = 1f;

        if (Guardado.instance != null)
        {
            int numeroZonas = GetTotalUnlockedZones();
            if (Guardado.instance.coinsPerZoneDaily > 0)
                contagionCoins += numeroZonas * Guardado.instance.coinsPerZoneDaily;
        }

        if (AudioManager.instance != null) AudioManager.instance.SwitchToGameMusic();

        CleanUpScene();

        int savedMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        ActivateMap(savedMap);

        isGameActive = true;
        currentSessionInfected = 0;
        monedasGanadasSesion = 0;


        // Reset estadísticas de evolución entre fases
        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            PersonaInfeccion.evolucionesEntreFases[i] = 0;
        }
        for (int i = 0; i < PersonaInfeccion.evolucionesPorChoque.Length; i++)
        {
            PersonaInfeccion.evolucionesPorChoque[i] = 0;
        }

        currentTimer = gameDuration;

        PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
        if (pm != null) pm.ConfigureRound(0);

        gameUI.SetActive(true);
        virusPlayer.SetActive(true);
        if (virusMovementScript != null) virusMovementScript.enabled = true;
        UpdateUI();
    }

    void EndSessionDay()
    {
        isGameActive = false;
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        gameUI.SetActive(false);
        if (virusMovementScript != null) virusMovementScript.enabled = false;

        int mapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int zoneMultiplier = (mapIndex == 1) ? 2 : (mapIndex == 2) ? 3 : 1;
        int baseMultiplier = Guardado.instance != null ? Guardado.instance.coinMultiplier : 1;

        if (EndDayResultsPanel.instance != null)
        {
            EndDayResultsPanel.instance.ShowResults(
                monedasGanadasSesion,
                contagionCoins
            );
        }



        if (Guardado.instance != null) Guardado.instance.AddTotalData(currentSessionInfected);
    }

    public void OnEndDayResultsFinished(int earnings, int dummy)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        contagionCoins += earnings;

        virusPlayer.SetActive(false);
        if (AudioManager.instance != null) AudioManager.instance.SwitchToMenuMusic();

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        if (Guardado.instance) Guardado.instance.SaveRunState(0, contagionCoins, currentMap);
        dayOverPanel.SetActive(true);

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
    public void SoftRestartRun()
    {
        Debug.Log("Soft Restart ejecutado");

        // 1️⃣ Monedas a 0
        contagionCoins = 0;
        monedasGanadasSesion = 0;

        // 2️⃣ Volver a zona 0
        PlayerPrefs.SetInt("CurrentMapIndex", 0);
        PlayerPrefs.Save();
        ActivateMap(0);

        // 3️⃣ Resetear vida del planeta
        PlanetCrontrollator planet = Object.FindFirstObjectByType<PlanetCrontrollator>();
        if (planet != null)
            planet.ResetHealthToInitial();

        // 4️⃣ Resetear estado de partida
        isGameActive = false;
        currentSessionInfected = 0;
        currentTimer = gameDuration;

        // 5️⃣ Limpiar escena
        CleanUpScene();

        // 6️⃣ Volver a pantalla de inicio de día
        if (gameUI) gameUI.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (zonePanel) zonePanel.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(true);

        UpdateUI();
    }


    public void TogglePause()
    {
        if (pausePanel == null) return;
        bool estaPausado = pausePanel.activeSelf;
        if (estaPausado)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
            if (virusMovementScript != null) virusMovementScript.enabled = true;
        }
        else
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
            if (virusMovementScript != null) virusMovementScript.enabled = false;
        }
    }
    // --- AÑADIR ESTO A TU LevelManager.cs ---

    // --- AÑADE O MODIFICA ESTO EN LevelManager.cs ---

    public void NextMapTransition()
    {
        if (!isGameActive) return;

        // Buscamos el script de transición en la escena
        LevelTransitioner transitioner = Object.FindFirstObjectByType<LevelTransitioner>();

        if (transitioner != null)
        {
            // Si existe el script de las rotaciones, que él tome el control
            transitioner.StartLevelTransition();
        }
        else
        {
            // Si por alguna razón no está el script de giro, 
            // hace el cambio normal que tenías antes para no romper el juego
            StartCoroutine(WaitAndChangeMap());
        }
    }

    private IEnumerator WaitAndChangeMap()
    {
        isGameActive = false;
        yield return new WaitForSecondsRealtime(0.5f);

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int nextMap = currentMap + 1;

        if (nextMap < mapList.Length)
        {
            // 1. Cambiar índice y activar mapa físico
            ActivateMap(nextMap);

            // 2. IMPORTANTE: Esperar al final del frame para que Unity registre 
            // que los objetos nuevos están activos y el tag "SpawnArea" sea localizable.
            yield return new WaitForEndOfFrame();

            // 3. Resetear jugador
            if (virusPlayer != null)
            {
                ManagerAnimacionJugador animManager = virusPlayer.GetComponent<ManagerAnimacionJugador>();
                if (animManager != null) animManager.ResetearEstado();
                virusPlayer.SetActive(true);
            }

            // 4. Configurar población DESPUÉS de que el mapa está activo
            PopulationManager pm = Object.FindFirstObjectByType<PopulationManager>();
            if (pm != null)
            {
                pm.ConfigureRound(0);
            }

            currentSessionInfected = 0;
            currentTimer = gameDuration;
            isGameActive = true;
        }
        else
        {
            ReturnToMenu();
        }
    }

    public void MostrarPuntosVoladores(Vector3 posicionPersona, int puntosGanados)
    {
        AddCoins(puntosGanados); // Sumamos el dinero

        // --- ESTA LÍNEA TIENE QUE ESTAR ASÍ (Con los == null) ---
        if (prefabTextoPuntos == null || canvasPrincipal == null || marcadorDestinoUI == null)
        {
            // Si falta algo, avisamos pero NO petamos el juego
            Debug.LogWarning("Faltan referencias en LevelManager para el texto volador");
            return;
        }
        
        GameObject nuevoTexto = Instantiate(prefabTextoPuntos, canvasPrincipal.transform);
        FloatingScoreUI scriptVuelo = nuevoTexto.GetComponent<FloatingScoreUI>();
        
        if (scriptVuelo != null)
        {
            scriptVuelo.IniciarViaje(puntosGanados, posicionPersona, marcadorDestinoUI, canvasPrincipal);
        }
    }

    public void OpenSkillTreePanel()
    {
        Debug.Log("BOTON PULSADO - Abriendo Skill Tree");
        // Cerrar todos los paneles primero
        if (menuPanel) menuPanel.SetActive(false);
        if (gameUI) gameUI.SetActive(false);
        if (dayOverPanel) dayOverPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (shinyPanel) zonePanel.SetActive(false);

        // Abrir panel de habilidades
        if (zonePanel) shinyPanel.SetActive(true);

        Time.timeScale = 1f; // aseguramos que no esté pausado
    }



}