using UnityEngine;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    [Header("Debug Herramientas")]
    public bool resetOnPlay = false;

    [Header("Datos Globales Acumulados")]
    public int totalInfected = 0;

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
    public float radiusMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public float infectSpeedMultiplier = 1.0f;

    [Header("Habilidad: bonus tiempo al subir fase por zona")]
    public float addTimeOnPhaseChance = 0f; // 0..1 (0.10 = 10%)
    public float doubleUpgradeChance = 0f; // 0..1
    public float randomSpawnPhaseChance = 0f; // 0..1 (0.05 = 5%)

    public float[] infectSpeedPerPhase = new float[5];

    public float[] probParedInfectiva = new float[6]; // Índices 1 a 5

    public bool hasExtraTimeUnlock; // true si el jugador compró/desbloqueó la habilidad

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
        PlayerPrefs.SetInt("CoinsHexagono", coinsExtraHexagono);
        PlayerPrefs.SetInt("CoinsPentagono", coinsExtraPentagono);
        PlayerPrefs.SetInt("CoinsCuadrado", coinsExtraCuadrado);
        PlayerPrefs.SetInt("CoinsTriangulo", coinsExtraTriangulo);
        PlayerPrefs.SetInt("CoinsCirculo", coinsExtraCirculo);
        PlayerPrefs.SetInt("ExtraTimeUnlock", hasExtraTimeUnlock ? 1 : 0);
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
    public void ActivarExtraTime()
    {
        hasExtraTimeUnlock = true;
        SaveData();
        Debug.Log("<color=cyan>Habilidad Tiempo Extra Desbloqueada Permanentemente</color>");
    }
    public void AddTotalData(int val) { totalInfected += val; SaveData(); }
    public void SetRadiusMultiplier(float val) { radiusMultiplier = val; SaveData(); }
    public void SetSpeedMultiplier(float val) { speedMultiplier = val; SaveData(); }
    public void SetCoinMultiplier(int val) { coinMultiplier = val; SaveData(); }
    public void AddCoinMultiplier(int extra){coinMultiplier += extra; SaveData();}
    public void AddStartingCoins(int extra){startingCoins += extra;SaveData();}
    public void SetStartingCoins(int val) { startingCoins = val; SaveData(); }
    public void AddSpawnSpeedBonus(float val) {spawnSpeedBonus += val;SaveData();}
    public void AddPopulationBonus(float val){ populationBonus += val;SaveData();}
    public void AddZonePassiveIncome(int extra){coinsPerZoneDaily += extra;SaveData();}
    public void AddRadiusMultiplier(float extra){radiusMultiplier += extra;SaveData();}
    public void ActivateZoneDiscount() { zoneDiscountActive = true; SaveData(); }
    public void SetZonePassiveIncome(int val) { coinsPerZoneDaily = val; SaveData(); }
    public void SetInfectSpeedMultiplier(float val) { infectSpeedMultiplier = val; SaveData(); }
    public void ActivateKeepZones() { keepZonesUnlocked = true; SaveData(); }
    public void ActivateKeepUpgrades() { keepUpgradesOnReset = true; SaveData(); }

    public void SetDoubleUpgradeChance(float chance)
    {
        doubleUpgradeChance = Mathf.Clamp01(chance);
        SaveData();
    }
    public void SetAddTimeOnPhaseChance(float chance)
    {
        addTimeOnPhaseChance = Mathf.Clamp01(chance);
        SaveData();
    }

    public void AddNivelParedInfectivaPorFigura(int fase)
    {
        if (fase >= 0 && fase < probParedInfectiva.Length)
        {
            // Aumenta el nivel de esa figura específica
            probParedInfectiva[fase] += 1f;
            SaveData();
            Debug.Log($"Mejorada Pared Infectiva Fase {fase}. Nivel actual: {probParedInfectiva[fase]}");
        }
    }
    // Métodos de Daño
    public void ActivarDañoExtraCirculo() { dañoExtraCirculo = 1; SaveData(); }
    public void ActivarDañoExtraTriangulo() { dañoExtraTriangulo = 1; SaveData(); }
    public void ActivarDañoExtraCuadrado() { dañoExtraCuadrado = 1; SaveData(); }
    public void ActivarDañoExtraPentagono() { dañoExtraPentagono = 1; SaveData(); }
    public void ActivarDañoExtraHexagono() { dañoExtraHexagono = 1; SaveData(); }
    public void ActivarMejoraDaño() { dañoExtraHabilidad = 1; SaveData(); }

    // Métodos Carambola y Pared

    public void ReboteConCoral() { virusReboteActiva = true; SaveData(); }
    public void SubirNivelCarambola()
    {
        if (nivelCarambola < 6)
        {
            nivelCarambola++;
            // Importante: Actualizamos el valor y llamamos a SaveData
            SaveData();
            Debug.Log("Carambola mejorada al nivel: " + nivelCarambola);
        }
    }
    public void ActivarParedInfectiva() { paredInfectivaActiva = true; SaveData(); }

    // Añade esto a Guardado.cs
    public void AddInfectSpeedPerPhase(int phaseIndex, float extraMultiplier)
    {
        if (phaseIndex >= 0 && phaseIndex < infectSpeedPerPhase.Length)
        {
            // Sumamos el extra al multiplicador actual de esa fase
            // Ejemplo: 1.0f + 0.1f = 1.1f
            infectSpeedPerPhase[phaseIndex] += extraMultiplier;
            SaveData();
            Debug.Log($"Fase {phaseIndex} actualizada. Nuevo Multiplicador: {infectSpeedPerPhase[phaseIndex]}");
        }
    }
    // --- NUEVOS MÉTODOS ACUMULATIVOS ---

    public void AddAddTimeOnPhaseChance(float extra)
    {
        addTimeOnPhaseChance = Mathf.Clamp01(addTimeOnPhaseChance + extra);
        SaveData();
    }

    public void AddDoubleUpgradeChance(float extra)
    {
        doubleUpgradeChance = Mathf.Clamp01(doubleUpgradeChance + extra);
        SaveData();
    }

    public void AddRandomSpawnPhaseChance(float amount)
    {
        // Sumamos la nueva cantidad al valor actual
        randomSpawnPhaseChance = Mathf.Clamp01(randomSpawnPhaseChance + amount);
        SaveData();
    }

    // --- REPARACIÓN DE ERRORES ESPECÍFICOS ---
    public void AddNivelParedInfectiva(int cantidad)
    {
        nivelParedInfectiva += cantidad; // Suma a lo que ya existe
        SaveData();
    }
    public void SetDuplicateProbability(float amount) { probabilidadDuplicarChoque = amount; SaveData(); }
    // Este método cubre el error de SetInfectionSpeedBonus
    public void SetInfectionSpeedBonus(float amount) { infectSpeedMultiplier = amount; SaveData(); }

    // --- SOLUCIÓN A LOS ERRORES DE REFERENCIA ---
    public void AddExtraBaseTime(float seconds)
    {
        extraBaseTime += seconds;
        SaveData();
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

    public void SaveRunState(int ignoredDay, int currentCoins, int currentMap)
    {
        PlayerPrefs.SetInt("Run_InProgress", 1);
        PlayerPrefs.SetInt("Run_Coins", currentCoins);
        PlayerPrefs.SetInt("Run_Map", currentMap);
        PlayerPrefs.Save();
    }

    public void ClearRunState()
    {
        PlayerPrefs.SetInt("Run_InProgress", 0);
        PlayerPrefs.Save();
    }

    public void SaveEvolutionData()
    {
        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            PlayerPrefs.SetInt("Run_Zona_" + i, PersonaInfeccion.evolucionesEntreFases[i]);
            PlayerPrefs.SetInt("Run_Choque_" + i, PersonaInfeccion.evolucionesPorChoque[i]);
            PlayerPrefs.SetInt("Run_Carambola_" + i, PersonaInfeccion.evolucionesCarambola[i]);
        }

        PlayerPrefs.Save();
    }

    public void LoadEvolutionData()
    {
        for (int i = 0; i < PersonaInfeccion.evolucionesEntreFases.Length; i++)
        {
            PersonaInfeccion.evolucionesEntreFases[i] =
                PlayerPrefs.GetInt("Run_Zona_" + i, 0);

            PersonaInfeccion.evolucionesPorChoque[i] =
                PlayerPrefs.GetInt("Run_Choque_" + i, 0);

            PersonaInfeccion.evolucionesCarambola[i] =
                PlayerPrefs.GetInt("Run_Carambola_" + i, 0);
        }
    }


    public void AddSpeedMultiplier(float extra)
    {
        speedMultiplier += extra;
        SaveData();
    }
    public bool HasSavedGame() => PlayerPrefs.GetInt("Run_InProgress", 0) == 1;
    public void ResetAllProgress() { PlayerPrefs.DeleteAll(); HardResetVariables(); }
}