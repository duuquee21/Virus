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
    public int nivelParedInfectiva = 0; // 0 = desactivada, 1-5 niveles
    public bool virusReboteActiva = false;

    [Header("Habilidad Especial")]
    public bool carambolaNormalActiva = false;
    public bool carambolaProActiva = false;
    public bool carambolaSupremaActiva = false;
    public bool destroyCoralOnInfectedImpact = false;
    public float probabilidadDuplicarChoque = 0f;

    [Header("Mejoras de Daño Individuales")]
    public int dañoExtraCirculo = 0;
    public int dañoExtraTriangulo = 0;
    public int dañoExtraCuadrado = 0;
    public int dañoExtraPentagono = 0;
    public int dañoExtraHexagono = 0;
    public int dañoExtraHabilidad = 0;


    [Header("Mejoras Permanentes")]
    public float extraBaseTime = 0f;

    [Header("Multiplicadores de Virus")]
    public float radiusMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public float infectSpeedMultiplier = 1.0f;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
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
        carambolaNormalActiva = false;
        carambolaProActiva = false;
        carambolaSupremaActiva = false;
        probabilidadDuplicarChoque = 0f;
        paredInfectivaActiva = false;
        nivelParedInfectiva = 0;
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
        PlayerPrefs.SetInt("CarambolaNormal", carambolaNormalActiva ? 1 : 0);
        PlayerPrefs.SetInt("CarambolaPro", carambolaProActiva ? 1 : 0);
        PlayerPrefs.SetInt("CarambolaSuprema", carambolaSupremaActiva ? 1 : 0);
        PlayerPrefs.SetInt("ParedInfectivaActiva", paredInfectivaActiva ? 1 : 0);
        PlayerPrefs.SetInt("NivelPared", nivelParedInfectiva);
        PlayerPrefs.SetInt("DmgCirculo", dañoExtraCirculo);
        PlayerPrefs.SetInt("DmgTriangulo", dañoExtraTriangulo);
        PlayerPrefs.SetInt("DmgCuadrado", dañoExtraCuadrado);
        PlayerPrefs.SetInt("DmgPentagono", dañoExtraPentagono);
        PlayerPrefs.SetInt("DmgHexagono", dañoExtraHexagono);
        PlayerPrefs.SetInt("DmgHabilidadGeneral", dañoExtraHabilidad);
        PlayerPrefs.SetFloat("ExtraBaseTime", extraBaseTime);
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
        carambolaNormalActiva = PlayerPrefs.GetInt("CarambolaNormal", 0) == 1;
        carambolaProActiva = PlayerPrefs.GetInt("CarambolaPro", 0) == 1;
        carambolaSupremaActiva = PlayerPrefs.GetInt("CarambolaSuprema", 0) == 1;
        paredInfectivaActiva = PlayerPrefs.GetInt("ParedInfectivaActiva", 0) == 1;
        nivelParedInfectiva = PlayerPrefs.GetInt("NivelPared", 0);
        probabilidadDuplicarChoque = PlayerPrefs.GetFloat("ProbDuplicar", 0f);
        extraBaseTime = PlayerPrefs.GetFloat("ExtraBaseTime", 0f);
    }

    // --- MÉTODOS PÚBLICOS DE ACTUALIZACIÓN ---
    public void AddTotalData(int val) { totalInfected += val; SaveData(); }
    public void SetRadiusMultiplier(float val) { radiusMultiplier = val; SaveData(); }
    public void SetSpeedMultiplier(float val) { speedMultiplier = val; SaveData(); }
    public void SetCoinMultiplier(int val) { coinMultiplier = val; SaveData(); }
    public void SetStartingCoins(int val) { startingCoins = val; SaveData(); }
    public void AddSpawnSpeedBonus(float val) { spawnSpeedBonus += val; SaveData(); }
    public void AddPopulationBonus(float val) { populationBonus += val; SaveData(); }
    public void ActivateZoneDiscount() { zoneDiscountActive = true; SaveData(); }
    public void SetZonePassiveIncome(int val) { coinsPerZoneDaily = val; SaveData(); }
    public void SetInfectSpeedMultiplier(float val) { infectSpeedMultiplier = val; SaveData(); }
    public void ActivateKeepZones() { keepZonesUnlocked = true; SaveData(); }
    public void ActivateKeepUpgrades() { keepUpgradesOnReset = true; SaveData(); }

    // Métodos de Daño
    public void ActivarDañoExtraCirculo() { dañoExtraCirculo = 1; SaveData(); }
    public void ActivarDañoExtraTriangulo() { dañoExtraTriangulo = 1; SaveData(); }
    public void ActivarDañoExtraCuadrado() { dañoExtraCuadrado = 1; SaveData(); }
    public void ActivarDañoExtraPentagono() { dañoExtraPentagono = 1; SaveData(); }
    public void ActivarDañoExtraHexagono() { dañoExtraHexagono = 1; SaveData(); }
    public void ActivarMejoraDaño() { dañoExtraHabilidad = 1; SaveData(); }

    // Métodos Carambola y Pared
    public void ActivarCarambolaNormal() { carambolaNormalActiva = true; SaveData(); }
    public void ReboteConCoral() { virusReboteActiva = true; SaveData(); }
    public void ActivarCarambolaPro() { carambolaProActiva = true; SaveData(); }
    public void ActivarCarambolaSuprema() { carambolaSupremaActiva = true; SaveData(); }
    public void ActivarParedInfectiva() { paredInfectivaActiva = true; SaveData(); }

    // --- REPARACIÓN DE ERRORES ESPECÍFICOS ---
    public void SetNivelParedInfectiva(int nivel) { nivelParedInfectiva = nivel; SaveData(); }
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



    public bool HasSavedGame() => PlayerPrefs.GetInt("Run_InProgress", 0) == 1;
    public void ResetAllProgress() { PlayerPrefs.DeleteAll(); HardResetVariables(); }
}