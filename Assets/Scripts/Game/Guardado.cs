using UnityEngine;


public class Guardado : MonoBehaviour
{

    public static Guardado instance;

    [Header("Debug Herramientas")]
    public bool resetOnPlay = false;

    [Header("Datos Globales Acumulados")]
    public int totalInfected = 0;

    [Header("Run Save")]
    public float runTimer = 0f;
    public int runCoins = 0;
    public int runMapIndex = 0;
    public float runPlanetHealth = 0f;
    public bool runInProgress = false;

    [Header("Permanentes del Árbol (Habilidades)")]
    public int freeInitialUpgrade = -1;
    public int coinMultiplier = 1;
    public int startingCoins = 0;
    public float spawnSpeedBonus = 0f;
    public float populationBonus = 0f;
    public bool zoneDiscountActive = false;
    public int coinsPerZoneDaily = 0;
    public bool keepUpgradesOnReset = false;
    public bool keepZonesUnlocked = false;

    [Header("Habilidades de Entorno")]
    public bool paredInfectivaActiva = false;
    public int nivelParedInfectiva = 1; // 0 = desactivada, 1-5 niveles
    public bool virusReboteActiva = false;

    [Header("Habilidad Especial")]

    public bool destroyCoralOnInfectedImpact = false;
    public float probabilidadDuplicarChoque = 0f;
    public int nivelCarambola = -1; // 0 = desactivada, 1-5 niveles

    [Header("Mejoras de Daño Individuales")]
    public int dañoExtraCirculo = 0;
    public int dañoExtraTriangulo = 0;
    public int dañoExtraCuadrado = 0;
    public int dañoExtraPentagono = 0;
    public int dañoExtraHexagono = 0;
    public int dañoExtraHabilidad = 0;

    public int coinsExtraCirculo;
    public int coinsExtraTriangulo;
    public int coinsExtraCuadrado;
    public int coinsExtraPentagono;
    public int coinsExtraHexagono;
    [Header("Mejoras Permanentes")]
    public float extraBaseTime = 0f;

    [Header("Multiplicadores de Virus")]
    public float radiusMultiplier = 5.0f;
    public float speedMultiplier = 1.0f;
    public float infectSpeedMultiplier = 1.0f;

    [Header("Habilidad: bonus tiempo al subir fase por zona")]
    public float addTimeOnPhaseChance = 0f; // 0..1 (0.10 = 10%)
    public float doubleUpgradeChance = 0f; // 0..1
    public float randomSpawnPhaseChance = 0f; // 0..1 (0.05 = 5%)
    public int speedLevel = 1;
    public int radiusLevel = 1;
    public int capacityLevel = 1;
    public int timeLevel = 1;
    public int infectionSpeedLevel = 1;

    public float[] infectSpeedPerPhase = new float[5];

    public float[] probParedInfectiva = new float[6]; // Índices 1 a 5

    public bool hasExtraTimeUnlock; // true si el jugador compró/desbloqueó la habilidad

    public bool coralInfeciosoActivo = false;
    public int coralCapacity = 5;

    public bool hojaNegraData = false; // Ejemplo de variable para el sistema de Hoja Negra
    public float hojaSpawnRate;
    public int hojaFases;

    public bool agujeroNegroData = false; // Ejemplo de variable para el sistema de Hoja Negra
    public float agujeroSpawnRate;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        for (int i = 0; i < infectSpeedPerPhase.Length; i++)
        {
            infectSpeedPerPhase[i] = 1f;
        }
        DontDestroyOnLoad(gameObject);

        if (resetOnPlay)
        {
            PlayerPrefs.DeleteAll();
            HardResetVariables();
        }
        else
        {
            LoadData();
        }
    }

    public void HardResetVariables()
    {
        nivelCarambola = -1; // Añade esta línea aquí
        totalInfected = 0;
        freeInitialUpgrade = -1;
        coinMultiplier = 1;
        startingCoins = 0;
        spawnSpeedBonus = 0f;
        populationBonus = 0f;
        zoneDiscountActive = false;
        coinsPerZoneDaily = 0;
        keepUpgradesOnReset = false;
        keepZonesUnlocked = false;
        hasExtraTimeUnlock = false; // <--- AÑADIDO
    probabilidadDuplicarChoque = 0f;
        paredInfectivaActiva = false;
        nivelParedInfectiva = 1;
        radiusMultiplier = 1.0f;
        speedMultiplier = 1.0f;
        infectSpeedMultiplier = 1.0f;
        speedLevel = 1;
        radiusLevel = 1;
        capacityLevel = 1;
        timeLevel = 1;
        infectionSpeedLevel = 1;
        dañoExtraCirculo = 0;
        dañoExtraTriangulo = 0;
        dañoExtraCuadrado = 0;
        dañoExtraPentagono = 0;
        dañoExtraHexagono = 0;
        dañoExtraHabilidad = 0;
        extraBaseTime = 0f;
        addTimeOnPhaseChance = 0f;
        doubleUpgradeChance = 0f;
        randomSpawnPhaseChance = 0f;
        coinsExtraHexagono = 0;
        coinsExtraPentagono = 0;
        coinsExtraCuadrado = 0;
        coinsExtraTriangulo = 0;
        coinsExtraCirculo = 0;

        coralInfeciosoActivo = false;
        coralCapacity = 5; // Valor base por defecto

        hojaNegraData = false; // Añadido

        hojaSpawnRate = 10f; // O el valor base que prefieras
        hojaFases = 1;
        agujeroNegroData = false;
        agujeroSpawnRate = 10f;

        for (int i = 0; i < infectSpeedPerPhase.Length; i++)
        {
            infectSpeedPerPhase[i] = 1f;
        }
        // Dentro de HardResetVariables()
        for (int i = 0; i < probParedInfectiva.Length; i++)
        {
            probParedInfectiva[i] = 0f;
        }
        ClearRunState();
        SaveData();
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("TotalInfected", totalInfected);
        PlayerPrefs.SetInt("CoinMultiplier", coinMultiplier);
        PlayerPrefs.SetInt("StartingCoins", startingCoins);
        PlayerPrefs.SetFloat("SpawnSpeedBonus", spawnSpeedBonus);
        PlayerPrefs.SetFloat("PopulationBonus", populationBonus);
        PlayerPrefs.SetInt("ZoneDiscount", zoneDiscountActive ? 1 : 0);
        PlayerPrefs.SetInt("CoinsPerZoneDaily", coinsPerZoneDaily);
        PlayerPrefs.SetInt("KeepUpgrades", keepUpgradesOnReset ? 1 : 0);
        PlayerPrefs.SetInt("KeepZones", keepZonesUnlocked ? 1 : 0);
        PlayerPrefs.SetFloat("RadiusMult", radiusMultiplier);
        PlayerPrefs.SetFloat("SpeedMult", speedMultiplier);
        PlayerPrefs.SetFloat("InfectSpeedMult", infectSpeedMultiplier);
        PlayerPrefs.SetFloat("ProbDuplicar", probabilidadDuplicarChoque);
        PlayerPrefs.SetInt("NivelCarambola", nivelCarambola);
        PlayerPrefs.SetInt("ParedInfectivaActiva", paredInfectivaActiva ? 1 : 0);
        PlayerPrefs.SetInt("NivelPared", nivelParedInfectiva);
        PlayerPrefs.SetInt("DmgCirculo", dañoExtraCirculo);
        PlayerPrefs.SetInt("DmgTriangulo", dañoExtraTriangulo);
        PlayerPrefs.SetInt("DmgCuadrado", dañoExtraCuadrado);
        PlayerPrefs.SetInt("DmgPentagono", dañoExtraPentagono);
        PlayerPrefs.SetInt("DmgHexagono", dañoExtraHexagono);
        PlayerPrefs.SetInt("DmgHabilidadGeneral", dañoExtraHabilidad);
        PlayerPrefs.SetFloat("ExtraBaseTime", extraBaseTime);
        PlayerPrefs.SetFloat("AddTimeOnPhaseChance", addTimeOnPhaseChance);
        PlayerPrefs.SetFloat("DoubleUpgradeChance", doubleUpgradeChance);
        PlayerPrefs.SetFloat("RandomSpawnPhaseChance", randomSpawnPhaseChance);
        PlayerPrefs.SetInt("SpeedLevel", speedLevel);
        PlayerPrefs.SetInt("RadiusLevel", radiusLevel);
        PlayerPrefs.SetInt("CapacityLevel", capacityLevel);
        PlayerPrefs.SetInt("TimeLevel", timeLevel);
        PlayerPrefs.SetInt("InfectionSpeedLevel", infectionSpeedLevel);
        PlayerPrefs.SetInt("CoinsHexagono", coinsExtraHexagono);
        PlayerPrefs.SetInt("CoinsPentagono", coinsExtraPentagono);
        PlayerPrefs.SetInt("CoinsCuadrado", coinsExtraCuadrado);
        PlayerPrefs.SetInt("CoinsTriangulo", coinsExtraTriangulo);
        PlayerPrefs.SetInt("CoinsCirculo", coinsExtraCirculo);
        PlayerPrefs.SetInt("ExtraTimeUnlock", hasExtraTimeUnlock ? 1 : 0);
        PlayerPrefs.SetInt("CoralInfeciosoActivo", coralInfeciosoActivo ? 1 : 0);
        PlayerPrefs.SetInt("CoralCapacity", coralCapacity);

        PlayerPrefs.SetInt("HojaNegraData", hojaNegraData ? 1 : 0);
        PlayerPrefs.SetFloat("HojaSpawnRate", hojaSpawnRate);
        PlayerPrefs.SetInt("HojaFases", hojaFases);
        PlayerPrefs.SetInt("AgujeroNegroData", agujeroNegroData ? 1 : 0);
        PlayerPrefs.SetFloat("AgujeroSpawnRate", agujeroSpawnRate);


        for (int i = 0; i < infectSpeedPerPhase.Length; i++)
        {
            PlayerPrefs.SetFloat("InfectSpeedPhase_" + i, infectSpeedPerPhase[i]);
        }
        // Al final de SaveData()
        for (int i = 0; i < probParedInfectiva.Length; i++)
        {
            PlayerPrefs.SetFloat("ProbParedInfectiva_" + i, probParedInfectiva[i]);
        }
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
        coinMultiplier = PlayerPrefs.GetInt("CoinMultiplier", 1);
        startingCoins = PlayerPrefs.GetInt("StartingCoins", 0);
        spawnSpeedBonus = PlayerPrefs.GetFloat("SpawnSpeedBonus", 0f);
        populationBonus = PlayerPrefs.GetFloat("PopulationBonus", 0f);
        zoneDiscountActive = PlayerPrefs.GetInt("ZoneDiscount", 0) == 1;
        coinsPerZoneDaily = PlayerPrefs.GetInt("CoinsPerZoneDaily", 0);
        keepUpgradesOnReset = PlayerPrefs.GetInt("KeepUpgrades", 0) == 1;
        keepZonesUnlocked = PlayerPrefs.GetInt("KeepZones", 0) == 1;
        radiusMultiplier = PlayerPrefs.GetFloat("RadiusMult", 1.0f);
        speedMultiplier = PlayerPrefs.GetFloat("SpeedMult", 1.0f);
        infectSpeedMultiplier = PlayerPrefs.GetFloat("InfectSpeedMult", 1.0f);
        dañoExtraCirculo = PlayerPrefs.GetInt("DmgCirculo", 0);
        dañoExtraTriangulo = PlayerPrefs.GetInt("DmgTriangulo", 0);
        dañoExtraCuadrado = PlayerPrefs.GetInt("DmgCuadrado", 0);
        dañoExtraPentagono = PlayerPrefs.GetInt("DmgPentagono", 0);
        dañoExtraHexagono = PlayerPrefs.GetInt("DmgHexagono", 0);
        dañoExtraHabilidad = PlayerPrefs.GetInt("DmgHabilidadGeneral", 0);
        nivelCarambola = PlayerPrefs.GetInt("NivelCarambola", -1);
        speedLevel = PlayerPrefs.GetInt("SpeedLevel", 1);
        radiusLevel = PlayerPrefs.GetInt("RadiusLevel", 1);
        capacityLevel = PlayerPrefs.GetInt("CapacityLevel", 1);
        timeLevel = PlayerPrefs.GetInt("TimeLevel", 1);
        infectionSpeedLevel = PlayerPrefs.GetInt("InfectionSpeedLevel", 1);

        paredInfectivaActiva = PlayerPrefs.GetInt("ParedInfectivaActiva", 0) == 1;
        nivelParedInfectiva = PlayerPrefs.GetInt("NivelPared", 0);
        probabilidadDuplicarChoque = PlayerPrefs.GetFloat("ProbDuplicar", 0f);
        extraBaseTime = PlayerPrefs.GetFloat("ExtraBaseTime", 0f);
        addTimeOnPhaseChance = PlayerPrefs.GetFloat("AddTimeOnPhaseChance", 0f);
        doubleUpgradeChance = PlayerPrefs.GetFloat("DoubleUpgradeChance", 0f);
        randomSpawnPhaseChance = PlayerPrefs.GetFloat("RandomSpawnPhaseChance", 0f);
        coinsExtraHexagono = PlayerPrefs.GetInt("CoinsHexagono", 0);
        coinsExtraPentagono = PlayerPrefs.GetInt("CoinsPentagono", 0);
        coinsExtraCuadrado = PlayerPrefs.GetInt("CoinsCuadrado", 0);
        coinsExtraTriangulo = PlayerPrefs.GetInt("CoinsTriangulo", 0);
        coinsExtraCirculo = PlayerPrefs.GetInt("CoinsCirculo", 0);
        hasExtraTimeUnlock = PlayerPrefs.GetInt("ExtraTimeUnlock", 0) == 1;
        coralInfeciosoActivo = PlayerPrefs.GetInt("CoralInfeciosoActivo", 0) == 1;
        coralCapacity = PlayerPrefs.GetInt("CoralCapacity", 5); // 5 como valor por defecto si no existe
        hojaNegraData = PlayerPrefs.GetInt("HojaNegraData", 0) == 1;
        hojaSpawnRate = PlayerPrefs.GetFloat("HojaSpawnRate", 10f);
        hojaFases = PlayerPrefs.GetInt("HojaFases", 1);
        agujeroNegroData = PlayerPrefs.GetInt("AgujeroNegroData", 0) == 1;
        agujeroSpawnRate = PlayerPrefs.GetFloat("AgujeroSpawnRate", 10f);

        for (int i = 0; i < infectSpeedPerPhase.Length; i++)
        {
            infectSpeedPerPhase[i] = PlayerPrefs.GetFloat("InfectSpeedPhase_" + i, 1f);
        }
        // Al final de LoadData()
        for (int i = 0; i < probParedInfectiva.Length; i++)
        {
            probParedInfectiva[i] = PlayerPrefs.GetFloat("ProbParedInfectiva_" + i, 0f);
        }
    }

    // --- MÉTODOS PÚBLICOS DE ACTUALIZACIÓN ---
    public void AddCoinMultiplier(int extra) { coinMultiplier += extra; }
    public void AddStartingCoins(int extra) { startingCoins += extra; }
    public void AddSpawnSpeedBonus(float val) { spawnSpeedBonus += val; }
    public void AddPopulationBonus(float val) { populationBonus += val; }
    public void AddZonePassiveIncome(int extra) { coinsPerZoneDaily += extra; }
    public void AddRadiusMultiplier(float extra) { radiusMultiplier += extra; }
    public void ActivateZoneDiscount() { zoneDiscountActive = true; }
    public void SetZonePassiveIncome(int val) { coinsPerZoneDaily = val; }
    public void SetInfectSpeedMultiplier(float val) { infectSpeedMultiplier = val; }
    public void ActivateKeepZones() { keepZonesUnlocked = true; }
    public void ActivateKeepUpgrades() { keepUpgradesOnReset = true; }

    public void SetDoubleUpgradeChance(float chance)
    {
        doubleUpgradeChance = Mathf.Clamp01(chance);
    }

    public void SetAddTimeOnPhaseChance(float chance)
    {
        addTimeOnPhaseChance = Mathf.Clamp01(chance);
    }

    public void AddNivelParedInfectivaPorFigura(int fase)
    {
        if (fase >= 0 && fase < probParedInfectiva.Length)
        {
            probParedInfectiva[fase] += 1f;
            Debug.Log($"Mejorada Pared Infectiva Fase {fase}. Nivel actual: {probParedInfectiva[fase]}");
        }
    }

    public void ActivarDañoExtraCirculo() { dañoExtraCirculo = 1; }
    public void ActivarDañoExtraTriangulo() { dañoExtraTriangulo = 1; }
    public void ActivarDañoExtraCuadrado() { dañoExtraCuadrado = 1; }
    public void ActivarDañoExtraPentagono() { dañoExtraPentagono = 1; }
    public void ActivarDañoExtraHexagono() { dañoExtraHexagono = 1; }
    public void ActivarMejoraDaño() { dañoExtraHabilidad = 1; }

    public void ActivarAgujeroNegro()
    {
        agujeroNegroData = true;
        Debug.Log("<color=purple>Agujero Negro activado permanentemente</color>");
    }

    public void MejorarSpawnHojaNegra(float extraSpawn, int extraFases)
    {
        hojaSpawnRate += extraSpawn;
    }

    public void MejorarDmgHojaNegra(float extraSpawn, int extraFases)
    {
        hojaFases += extraFases;
    }

    public void MejorarSpawnAgujeroNegro(float extraSpawn)
    {
        agujeroSpawnRate += extraSpawn;
    }

    public void ActivarHojaNegra()
    {
        hojaNegraData = true;
        Debug.Log("<color=black><b>Hojanegra activada permanentemente</b></color>");
    }

    public void ActivarCoralInfeccioso()
    {
        coralInfeciosoActivo = true;
    }

    public void MejorarCapacidadCoral(int extra)
    {
        coralCapacity += extra;
    }

    public void ReboteConCoral() { virusReboteActiva = true; }

    public void SubirNivelCarambola()
    {
        if (nivelCarambola < 6)
        {
            nivelCarambola++;
            Debug.Log("Carambola mejorada al nivel: " + nivelCarambola);
        }
    }

    public void ActivarParedInfectiva() { paredInfectivaActiva = true; }

    public void AddInfectSpeedPerPhase(int phaseIndex, float extraMultiplier)
    {
        if (phaseIndex >= 0 && phaseIndex < infectSpeedPerPhase.Length)
        {
            infectSpeedPerPhase[phaseIndex] += extraMultiplier;
            Debug.Log($"Fase {phaseIndex} actualizada. Nuevo Multiplicador: {infectSpeedPerPhase[phaseIndex]}");
        }
    }

    public void AddAddTimeOnPhaseChance(float extra)
    {
        addTimeOnPhaseChance = Mathf.Clamp01(addTimeOnPhaseChance + extra);
    }

    public void AddDoubleUpgradeChance(float extra)
    {
        doubleUpgradeChance = Mathf.Clamp01(doubleUpgradeChance + extra);
    }

    public void AddRandomSpawnPhaseChance(float amount)
    {
        randomSpawnPhaseChance = Mathf.Clamp01(randomSpawnPhaseChance + amount);
    }

    public void AddNivelParedInfectiva(int cantidad)
    {
        nivelParedInfectiva += cantidad;
    }

    public void SetDuplicateProbability(float amount) { probabilidadDuplicarChoque = amount; }
    public void SetInfectionSpeedBonus(float amount) { infectSpeedMultiplier = amount; }

    public void AddExtraBaseTime(float seconds)
    {
        extraBaseTime += seconds;
    }

    public void AddSpeedMultiplier(float extra)
    {
        speedMultiplier += extra;
    }
    // Este método aplica físicamente la mejora gratuita al empezar una partida
    public void ApplyPermanentInitialUpgrade()
    {
        if (freeInitialUpgrade == -1) return;

        Debug.Log("<color=green>Aplicando Mejora Inicial Permanente:</color> Tipo " + freeInitialUpgrade);

        switch (freeInitialUpgrade)
        {
            case 0: if (VirusRadiusController.instance) VirusRadiusController.instance.UpgradeRadius(); break;
            case 1: if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.UpgradeCapacity(); break;
            case 2: if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.UpgradeSpeed(); break;
            case 3: if (TimeUpgradeController.instance) TimeUpgradeController.instance.UpgradeTime(); break;
            case 4: if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.UpgradeInfectionSpeed(); break;
        }
    }

    // Este método devuelve el texto que sale en el botón de "Continuar" del menú
    public string GetContinueDetails()
    {
        // Recuperamos las monedas de la partida en curso guardadas en PlayerPrefs
        int coins = PlayerPrefs.GetInt("Run_Coins", 0);
        return "Modo Infinito - Monedas: " + coins;
    }

    // --- SISTEMA DE PARTIDA ---
    public void AssignRandomInitialUpgrade()
    {
        if (freeInitialUpgrade != -1) return;
        freeInitialUpgrade = Random.Range(0, 5);
        SaveData();
    }

    public void SaveRunState(float timer, int currentCoins, int currentMap, float planetHealth)
    {
        PlayerPrefs.SetInt("Run_InProgress", 1);

        PlayerPrefs.SetFloat("Run_Timer", timer);
        PlayerPrefs.SetInt("Run_Coins", currentCoins);
        PlayerPrefs.SetInt("Run_Map", currentMap);
        PlayerPrefs.SetFloat("Run_PlanetHealth", planetHealth);

        PlayerPrefs.Save();
    }
    public void ClearRunState()
    {
        PlayerPrefs.SetInt("Run_InProgress", 0);
        PlayerPrefs.Save();
    }
    public void SaveEvolutionData()
    {
        int fases = PersonaInfeccion.dañoZonaPorFase.Length;

        for (int i = 0; i < fases; i++)
        {
            PlayerPrefs.SetInt("Run_Zona_" + i, PersonaInfeccion.evolucionesEntreFases[i]);
            PlayerPrefs.SetInt("Run_Choque_" + i, PersonaInfeccion.evolucionesPorChoque[i]);
            PlayerPrefs.SetInt("Run_Carambola_" + i, PersonaInfeccion.evolucionesCarambola[i]);

            PlayerPrefs.SetFloat("Run_DmgZona_" + i, PersonaInfeccion.dañoZonaPorFase[i]);
            PlayerPrefs.SetFloat("Run_DmgChoque_" + i, PersonaInfeccion.dañoChoquePorFase[i]);
            PlayerPrefs.SetFloat("Run_DmgCarambola_" + i, PersonaInfeccion.dañoCarambolaPorFase[i]);

            PlayerPrefs.SetInt("Run_Golpes_" + i, PersonaInfeccion.golpesAlPlanetaPorFase[i]);
        }

        PlayerPrefs.SetFloat("Run_DmgTotalZona", PersonaInfeccion.dañoTotalZona);

        PlayerPrefs.Save();
    }
    public void LoadEvolutionData()
    {
        int fases = PersonaInfeccion.evolucionesEntreFases.Length;

        for (int i = 0; i < fases; i++)
        {
            PersonaInfeccion.evolucionesEntreFases[i] =
                PlayerPrefs.GetInt("Run_Zona_" + i, 0);

            PersonaInfeccion.evolucionesPorChoque[i] =
                PlayerPrefs.GetInt("Run_Choque_" + i, 0);

            PersonaInfeccion.evolucionesCarambola[i] =
                PlayerPrefs.GetInt("Run_Carambola_" + i, 0);

            PersonaInfeccion.dañoZonaPorFase[i] =
                PlayerPrefs.GetFloat("Run_DmgZona_" + i, 0);

            PersonaInfeccion.dañoChoquePorFase[i] =
                PlayerPrefs.GetFloat("Run_DmgChoque_" + i, 0);

            PersonaInfeccion.dañoCarambolaPorFase[i] =
                PlayerPrefs.GetFloat("Run_DmgCarambola_" + i, 0);

            PersonaInfeccion.golpesAlPlanetaPorFase[i] =
                PlayerPrefs.GetInt("Run_Golpes_" + i, 0);
        }

        PersonaInfeccion.dañoTotalZona =
            PlayerPrefs.GetFloat("Run_DmgTotalZona", 0);
    }

    public void AddTotalData(int val)
    {
        totalInfected += val;
    }

    public void SetRadiusMultiplier(float val)
    {
        radiusMultiplier = val;
    }

    public void SetSpeedMultiplier(float val)
    {
        speedMultiplier = val;
    }

    public void SetCoinMultiplier(int val)
    {
        coinMultiplier = val;
    }

    public void SetStartingCoins(int val)
    {
        startingCoins = val;
    }
    public bool HasSavedGame() => PlayerPrefs.GetInt("Run_InProgress", 0) == 1;
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        SkillNode.ClearRuntimeState();

        HardResetVariables();

        SkillNode[] nodes = FindObjectsOfType<SkillNode>(true);
        foreach (SkillNode node in nodes)
        {
            node.ResetNodeState();
        }

        SkillTreeLinesUI lines = FindFirstObjectByType<SkillTreeLinesUI>();
        if (lines != null)
        {
            lines.ResetAllLinesVisuals();
            lines.RefreshAllLinesFromNodes();
        }
    }

    // ========== GUARDADO AL CERRAR LA APLICACIÓN ==========
    void OnApplicationQuit()
    {
        // Guardar TODO antes de que se cierre la app
        SaveData();
        SaveEvolutionData();
        
        // También guardamos el estado de todos los nodos del árbol
        SkillNode[] nodes = FindObjectsOfType<SkillNode>(true);
        foreach (SkillNode node in nodes)
        {
            node.SaveNodeState();
        }
        
        PlayerPrefs.Save();
        Debug.Log("<color=yellow>[Guardado]</color> Datos guardados antes de cerrar aplicación");
    }
}