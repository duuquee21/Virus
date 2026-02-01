using UnityEngine;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    public int totalInfected = 0;
    public int shinyDNA = 10000000;

    // -------- PERMANENTES DEL ÁRBOL --------
    public int freeInitialUpgrade = -1;
    public int coinMultiplier = 1;
    public int startingCoins = 0;
    public float spawnSpeedBonus = 0f;
    public float populationBonus = 0f;

    [Header("Shiny Economy")]
    // Cambiamos shinyValueMultiplier por estas dos variables:
    public int shinyValueSum = 1;      // Lo que se suma (+1, +3...)
    public int shinyMultiplier = 1;    // El multiplicador base (x5, x7...)

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

        LoadData();
    }

    // ==============================
    // ECONOMÍA BASE
    // ==============================

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

    // ==============================
    // LÓGICA SHINY (MULTIPLICADORES BASE)
    // ==============================

    // Para habilidades de SUMA (+1, +3)
    public void IncreaseShinyValueSum(int amount)
    {
        shinyValueSum += amount;
        SaveData();
    }

    // Para habilidades de MULTIPLICADOR (x5, x7, x10)
    public void SetShinyMultiplier(int newMultiplier)
    {
        // Solo actualizamos si el nuevo multiplicador es mayor
        if (newMultiplier > shinyMultiplier)
        {
            shinyMultiplier = newMultiplier;
        }
        SaveData();
    }

    // Esta función la usará PersonaInfeccion para dar el ADN
    public int GetFinalShinyValue()
    {
        return shinyValueSum * shinyMultiplier;
    }

    // ==============================
    // RANDOM UPGRADE INICIAL
    // ==============================

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
            case 0: VirusRadiusController.instance.UpgradeRadius(); break;
            case 1: CapacityUpgradeController.instance.UpgradeCapacity(); break;
            case 2: SpeedUpgradeController.instance.UpgradeSpeed(); break;
            case 3: TimeUpgradeController.instance.UpgradeTime(); break;
            case 4: InfectionSpeedUpgradeController.instance.UpgradeInfectionSpeed(); break;
        }
    }

    // ==============================
    // HABILIDADES VARIAS
    // ==============================

    public void SetCoinMultiplier(int value)
    {
        if (value <= coinMultiplier) return;
        coinMultiplier = value;
        SaveData();
    }

    public void SetStartingCoins(int value)
    {
        if (value <= startingCoins) return;
        startingCoins = value;
        SaveData();
    }

    public void AddSpawnSpeedBonus(float amount)
    {
        spawnSpeedBonus += amount;
        SaveData();
    }

    public void AddPopulationBonus(float amount)
    {
        populationBonus += amount;
        SaveData();
    }

    public void AddBonusDays(int days)
    {
        bonusDaysPermanent += days;
        SaveData();
    }

    // ==============================
    // SAVE / LOAD (ACTUALIZADO)
    // ==============================

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

        // NUEVAS VARIABLES SHINY
        PlayerPrefs.SetInt("ShinyValueSum", shinyValueSum);
        PlayerPrefs.SetInt("ShinyMultiplier", shinyMultiplier);

        PlayerPrefs.Save();
    }

    void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
        shinyDNA = PlayerPrefs.GetInt("TotalShinyDNA", 0);
        freeInitialUpgrade = PlayerPrefs.GetInt("FreeInitialUpgrade", -1);
        coinMultiplier = PlayerPrefs.GetInt("CoinMultiplier", 1);
        startingCoins = PlayerPrefs.GetInt("StartingCoins", 0);
        spawnSpeedBonus = PlayerPrefs.GetFloat("SpawnSpeedBonus", 0f);
        populationBonus = PlayerPrefs.GetFloat("PopulationBonus", 0f);
        bonusDaysPermanent = PlayerPrefs.GetInt("BonusDaysPermanent", 0);

        // NUEVAS VARIABLES SHINY
        shinyValueSum = PlayerPrefs.GetInt("ShinyValueSum", 1); // Default 1
        shinyMultiplier = PlayerPrefs.GetInt("ShinyMultiplier", 1); // Default 1
    }
}