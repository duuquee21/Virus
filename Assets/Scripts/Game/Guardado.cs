using UnityEngine;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    [Header("Debug Herramientas")]
    public bool resetOnPlay = true;

    [Header("Datos de Juego")]
    public int totalInfected = 0;
    public int shinyDNA = 0;

    [Header("Permanentes del Árbol")]
    public int freeInitialUpgrade = -1;
    public int coinMultiplier = 1;
    public int startingCoins = 0;
    public float spawnSpeedBonus = 0f;
    public float populationBonus = 0f;
    public bool zoneDiscountActive = false;

    // Esta es la variable clave para el stock extra por zona
    public int extraShiniesPerRound = 0;

    public int coinsPerZoneDaily = 0;
    public int shinyPerZoneDaily = 0;
    public bool guaranteedShiny = false;
    public bool keepUpgradesOnReset = false;
    public bool keepZonesUnlocked = false;

    [Header("Multiplicadores")]
    public float radiusMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public float infectSpeedMultiplier = 1.0f;
    public float shinyCaptureMultiplier = 1.0f;

    [Header("Shiny Economy")]
    public int shinyValueSum = 1;
    public int shinyMultiplier = 1;

    [Header("Otros")]
    public int bonusDaysPermanent = 0;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (resetOnPlay)
        {
            Debug.Log("<color=red><b>[BORRADO TOTAL]:</b> Empezando partida limpia desde CERO.</color>");
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
        shinyDNA = 0;
        freeInitialUpgrade = -1;
        coinMultiplier = 1;
        startingCoins = 0;
        spawnSpeedBonus = 0f;
        populationBonus = 0f;
        zoneDiscountActive = false;
        extraShiniesPerRound = 0;
        coinsPerZoneDaily = 0;
        shinyPerZoneDaily = 0;
        guaranteedShiny = false;
        keepUpgradesOnReset = false;
        radiusMultiplier = 1.0f;
        speedMultiplier = 1.0f;
        infectSpeedMultiplier = 1.0f;
        shinyCaptureMultiplier = 1.0f;
        shinyValueSum = 1;
        shinyMultiplier = 1;
        bonusDaysPermanent = 0;
        keepZonesUnlocked = false;

        ClearRunState();
        SaveData();
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("TotalInfected", totalInfected);
        PlayerPrefs.SetInt("TotalShinyDNA", shinyDNA);
        PlayerPrefs.SetInt("FreeInitialUpgrade", freeInitialUpgrade);
        PlayerPrefs.SetInt("CoinMultiplier", coinMultiplier);
        PlayerPrefs.SetInt("StartingCoins", startingCoins);
        PlayerPrefs.SetFloat("SpawnSpeedBonus", spawnSpeedBonus);
        PlayerPrefs.SetFloat("PopulationBonus", populationBonus);
        PlayerPrefs.SetInt("BonusDaysPermanent", bonusDaysPermanent);
        PlayerPrefs.SetInt("ShinyValueSum", shinyValueSum);
        PlayerPrefs.SetInt("ShinyMultiplier", shinyMultiplier);
        PlayerPrefs.SetInt("ZoneDiscount", zoneDiscountActive ? 1 : 0);
        PlayerPrefs.SetInt("ExtraShinies", extraShiniesPerRound);
        PlayerPrefs.SetInt("CoinsPerZoneDaily", coinsPerZoneDaily);
        PlayerPrefs.SetInt("ShinyPerZoneDaily", shinyPerZoneDaily);
        PlayerPrefs.SetInt("GuaranteedShiny", guaranteedShiny ? 1 : 0);
        PlayerPrefs.SetInt("KeepUpgrades", keepUpgradesOnReset ? 1 : 0);
        PlayerPrefs.SetFloat("RadiusMult", radiusMultiplier);
        PlayerPrefs.SetFloat("SpeedMult", speedMultiplier);
        PlayerPrefs.SetFloat("InfectSpeedMult", infectSpeedMultiplier);
        PlayerPrefs.SetFloat("ShinyCaptureMult", shinyCaptureMultiplier);
        PlayerPrefs.SetInt("KeepZones", keepZonesUnlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
        shinyDNA = PlayerPrefs.GetInt("TotalShinyDNA", 0);
        freeInitialUpgrade = PlayerPrefs.GetInt("FreeInitialUpgrade", -1);
        coinMultiplier = PlayerPrefs.GetInt("CoinMultiplier", 1);
        startingCoins = PlayerPrefs.GetInt("StartingCoins", 0);
        spawnSpeedBonus = PlayerPrefs.GetFloat("SpawnSpeedBonus", 0f);
        populationBonus = PlayerPrefs.GetFloat("PopulationBonus", 0f);
        bonusDaysPermanent = PlayerPrefs.GetInt("BonusDaysPermanent", 0);
        shinyValueSum = PlayerPrefs.GetInt("ShinyValueSum", 1);
        shinyMultiplier = PlayerPrefs.GetInt("ShinyMultiplier", 1);
        zoneDiscountActive = PlayerPrefs.GetInt("ZoneDiscount", 0) == 1;
        extraShiniesPerRound = PlayerPrefs.GetInt("ExtraShinies", 0);
        coinsPerZoneDaily = PlayerPrefs.GetInt("CoinsPerZoneDaily", 0);
        shinyPerZoneDaily = PlayerPrefs.GetInt("ShinyPerZoneDaily", 0);
        guaranteedShiny = PlayerPrefs.GetInt("GuaranteedShiny", 0) == 1;
        keepUpgradesOnReset = PlayerPrefs.GetInt("KeepUpgrades", 0) == 1;
        radiusMultiplier = PlayerPrefs.GetFloat("RadiusMult", 1.0f);
        speedMultiplier = PlayerPrefs.GetFloat("SpeedMult", 1.0f);
        infectSpeedMultiplier = PlayerPrefs.GetFloat("InfectSpeedMult", 1.0f);
        shinyCaptureMultiplier = PlayerPrefs.GetFloat("ShinyCaptureMult", 1.0f);
        keepZonesUnlocked = PlayerPrefs.GetInt("KeepZones", 0) == 1;
    }

    // --- MÉTODOS PÚBLICOS ---

    public void AddExtraShinyLevel()
    {
        extraShiniesPerRound++; // Aumenta el nivel permanente
        SaveData();

        // Avisamos al LevelManager para que sume +1 al stock de la partida actual
        if (LevelManager.instance != null)
        {
            LevelManager.instance.ActualizarStockPorCompraHabilidad();
        }
    }

    public void ActivateKeepZones() { keepZonesUnlocked = true; SaveData(); }
    public void ActivateKeepUpgrades() { keepUpgradesOnReset = true; SaveData(); }
    public void AddShinyDNA(int val) { shinyDNA += val; SaveData(); }
    public void AddTotalData(int val) { totalInfected += val; SaveData(); }
    public int GetFinalShinyValue() => shinyValueSum * shinyMultiplier;
    public void SetRadiusMultiplier(float val) { radiusMultiplier = val; SaveData(); }
    public void SetSpeedMultiplier(float val) { speedMultiplier = val; SaveData(); }
    public void SetCoinMultiplier(int val) { coinMultiplier = val; SaveData(); }
    public void SetStartingCoins(int val) { startingCoins = val; SaveData(); }
    public void AddSpawnSpeedBonus(float val) { spawnSpeedBonus += val; SaveData(); }
    public void AddPopulationBonus(float val) { populationBonus += val; SaveData(); }
    public void AddBonusDays(int val) { bonusDaysPermanent += val; SaveData(); }
    public void ActivateZoneDiscount() { zoneDiscountActive = true; SaveData(); }
    public void SetZonePassiveIncome(int val) { coinsPerZoneDaily = val; SaveData(); }
    public void SetShinyPassiveIncome(int val) { shinyPerZoneDaily = val; SaveData(); }
    public void IncreaseShinyValueSum(int val) { shinyValueSum += val; SaveData(); }
    public void SetShinyMultiplier(int val) { shinyMultiplier = val; SaveData(); }
    public void ActivateGuaranteedShiny() { guaranteedShiny = true; SaveData(); }
    public void SetInfectSpeedMultiplier(float val) { infectSpeedMultiplier = val; SaveData(); }
    public void SetShinyCaptureMultiplier(float val) { shinyCaptureMultiplier = val; SaveData(); }

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

    // --- SISTEMA DE PERSISTENCIA DE RUN ---

    public void SaveRunState(int currentDay, int currentCoins, int currentMap)
    {
        PlayerPrefs.SetInt("RunInProgress", 1);
        PlayerPrefs.SetInt("RunDay", currentDay);
        PlayerPrefs.SetInt("RunCoins", currentCoins);
        PlayerPrefs.SetInt("RunMap", currentMap);
        PlayerPrefs.Save();
    }

    public void ClearRunState()
    {
        PlayerPrefs.SetInt("RunInProgress", 0);
        PlayerPrefs.Save();
    }

    public void AssignRandomInitialUpgrade()
    {
        if (freeInitialUpgrade != -1) return;
        freeInitialUpgrade = Random.Range(0, 5);
        SaveData();
        ApplyPermanentInitialUpgrade();
    }

    // Esto es un "puente" para que los scripts viejos no den error
    public void AddExtraShiny()
    {
        AddExtraShinyLevel();
    }
    public bool HasSavedGame() => PlayerPrefs.GetInt("RunInProgress", 0) == 1;
}