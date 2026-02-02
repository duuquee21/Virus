using UnityEngine;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    public int totalInfected = 0;
    public int shinyDNA = 10000000;

    [Header("Permanentes del Árbol")]
    public int freeInitialUpgrade = -1;
    public int coinMultiplier = 1;
    public int startingCoins = 0;
    public float spawnSpeedBonus = 0f;
    public float populationBonus = 0f;
    public bool zoneDiscountActive= false;

    [Header("Shiny Economy")]
    public int shinyValueSum = 1;      // +1, +3, etc.
    public int shinyMultiplier = 1;    // x5, x7, x10 (Sustituye, no acumula)

    [Header("Otros")]
    public int bonusDaysPermanent = 0;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        Debug.Log("<color=cyan>Editor: Reseteando PlayerPrefs para una prueba limpia.</color>");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
#endif

        LoadData();
    }

    // --- ECONOMÍA Y DATOS ---
    public void AddShinyDNA(int amountShiny)
    {
        shinyDNA += amountShiny;
        SaveData();
    }

    public void AddTotalData(int amount)
    {
        totalInfected += amount;
        SaveData();
    }

    // --- LÓGICA DE MULTIPLICADORES (SISTEMA BASE) ---
    public void IncreaseShinyValueSum(int amount)
    {
        shinyValueSum += amount;
        SaveData();
    }

    public void SetShinyMultiplier(int newMultiplier)
    {
        if (newMultiplier > shinyMultiplier)
        {
            shinyMultiplier = newMultiplier;
        }
        SaveData();
    }

    public int GetFinalShinyValue()
    {
        return shinyValueSum * shinyMultiplier;
    }

    // --- MEJORAS DEL ÁRBOL ---
    public void SetCoinMultiplier(int value) { if (value > coinMultiplier) coinMultiplier = value; SaveData(); }
    public void SetStartingCoins(int value) { if (value > startingCoins) startingCoins = value; SaveData(); }
    public void AddSpawnSpeedBonus(float amount) { spawnSpeedBonus += amount; SaveData(); }
    public void AddPopulationBonus(float amount) { populationBonus += amount; SaveData(); }
    public void AddBonusDays(int days) { bonusDaysPermanent += days; SaveData(); }
    public void ActivateZoneDiscount(){zoneDiscountActive = true; SaveData();}

    public void AssignRandomInitialUpgrade()
    {
        if (freeInitialUpgrade != -1) return;
        freeInitialUpgrade = Random.Range(0, 5);
        SaveData();
        ApplyPermanentInitialUpgrade();
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

    void SaveData()
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
        PlayerPrefs.SetInt("ZoneDiscount", zoneDiscountActive? 1:0);
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
        shinyDNA = PlayerPrefs.GetInt("TotalShinyDNA", 10000000);
        freeInitialUpgrade = PlayerPrefs.GetInt("FreeInitialUpgrade", -1);
        coinMultiplier = PlayerPrefs.GetInt("CoinMultiplier", 1);
        startingCoins = PlayerPrefs.GetInt("StartingCoins", 0);
        spawnSpeedBonus = PlayerPrefs.GetFloat("SpawnSpeedBonus", 0f);
        populationBonus = PlayerPrefs.GetFloat("PopulationBonus", 0f);
        bonusDaysPermanent = PlayerPrefs.GetInt("BonusDaysPermanent", 0);
        shinyValueSum = PlayerPrefs.GetInt("ShinyValueSum", 1);
        shinyMultiplier = PlayerPrefs.GetInt("ShinyMultiplier", 1);
        zoneDiscountActive = PlayerPrefs.GetInt("!ZoneDiscount", 0) == 1;
    }
}