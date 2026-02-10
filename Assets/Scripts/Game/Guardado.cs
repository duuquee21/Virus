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
    [Header("Habilidad Especial")]
    public bool carambolaNormalActiva = false;
    public bool carambolaProActiva = false;
    public bool carambolaSupremaActiva = false; // <--- NUEVA VARIABLE
                                                // Dentro de la clase Guardado
    public int dañoExtraHabilidad = 0; // 0 por defecto, 1 cuando se compre la mejora

    // Opcional: añade esto para que el sistema de habilidades lo active
    public void ActivarMejoraDaño()
    {
        dañoExtraHabilidad = 1;
        SaveData(); // Si tienes un método para guardar
    }





    // Recuerda añadir PlayerPrefs.GetFloat y SetFloat para "ProbCarambola" 
    // en tus métodos LoadData y SaveData como hiciste con la otra probabilidad.


    // --- NUEVA VARIABLE ---
    [Header("Habilidad Especial")]
    public float probabilidadDuplicarChoque = 0f;


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
        probabilidadDuplicarChoque = 0f; // Reset aquí también
        radiusMultiplier = 1.0f;
        speedMultiplier = 1.0f;
        infectSpeedMultiplier = 1.0f;

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

        // --- GUARDAR PROBABILIDAD ---
        PlayerPrefs.SetFloat("ProbDuplicar", probabilidadDuplicarChoque);

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

        // --- CARGAR PROBABILIDAD ---
        probabilidadDuplicarChoque = PlayerPrefs.GetFloat("ProbDuplicar", 0f);
    }

    // --- MÉTODOS PÚBLICOS ---
    public void ActivateKeepZones() { keepZonesUnlocked = true; SaveData(); }
    public void ActivateKeepUpgrades() { keepUpgradesOnReset = true; SaveData(); }
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



    public void ActivarCarambolaNormal()
    {
        carambolaNormalActiva = true;
        SaveData();
    }
    public void ActivarCarambolaPro()
    {
        carambolaProActiva = true;
        SaveData();
        Debug.Log("<color=cyan>Carambola PRO Activada: Inercia cinética habilitada.</color>");
    }
    public void ActivarCarambolaSuprema()
    {
        carambolaSupremaActiva = true;
        SaveData();
    }

    // --- CORRECCIÓN AQUÍ: AÑADIDO SaveData() ---
    public void SetDuplicateProbability(float amount)
    {
        probabilidadDuplicarChoque = amount;
        SaveData(); // ¡Esto es lo que faltaba!
        Debug.Log("<color=green>Probabilidad de Duplicación guardada:</color> " + amount);
    }
    public void ActivarParedInfectiva()
    {
        paredInfectivaActiva = true;
        SaveData();
    }

    public void ApplyPermanentInitialUpgrade()
    {
        if (freeInitialUpgrade == -1) return;
        switch (freeInitialUpgrade)
        {
            case 0: if (VirusRadiusController.instance) VirusRadiusController.instance.UpgradeRadius(); break;
            case 1: if (CapacityUpgradeController.instance) CapacityUpgradeController.instance.UpgradeCapacity(); break;
            case 2: if (SpeedUpgradeController.instance) SpeedUpgradeController.instance.UpgradeSpeed(); break;
            case 3: if (TimeUpgradeController.instance) TimeUpgradeController.instance.UpgradeTime(); break;
            case 4: if (InfectionSpeedUpgradeController.instance) InfectionSpeedUpgradeController.instance.UpgradeInfectionSpeed(); break;
        }
    }

    public void AssignRandomInitialUpgrade()
    {
        if (freeInitialUpgrade != -1) return;
        freeInitialUpgrade = Random.Range(0, 5);
        SaveData();
        ApplyPermanentInitialUpgrade();
    }

    public void SaveRunState(int ignoredDay, int currentCoins, int currentMap)
    {
        PlayerPrefs.SetInt("Run_InProgress", 1);
        PlayerPrefs.SetInt("Run_Day", 0);
        PlayerPrefs.SetInt("Run_Coins", currentCoins);
        PlayerPrefs.SetInt("Run_Map", currentMap);
        PlayerPrefs.Save();
    }

    public void ClearRunState()
    {
        PlayerPrefs.SetInt("Run_InProgress", 0);
        PlayerPrefs.Save();
    }

    public bool HasSavedGame() => PlayerPrefs.GetInt("Run_InProgress", 0) == 1;

    public string GetContinueDetails()
    {
        int coins = PlayerPrefs.GetInt("Run_Coins", 0);
        return "Modo Infinito - Monedas: " + coins;
    }
  


    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        HardResetVariables();
        SaveData();
    }
}