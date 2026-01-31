using UnityEngine;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    public int totalInfected = 0;
    public int shinyDNA = 10000000;

    // -------- PERMANENTES DEL ÁRBOL --------
    public int freeInitialUpgrade = -1;   // upgrade random inicial
    public int coinMultiplier = 1;        // x1, x2, x3, x4, x5
    public int startingCoins = 0;         // 0, 50, 100, 500, etc
    public float spawnSpeedBonus = 0f;    // 0.2 = -20%, 0.4 = -40%...
    public float populationBonus = 0f;    // 0.25 = +25%, 0.5 = +50%...

    // NUEVO: DIAS EXTRA PERMANENTES (+5, +10, etc)
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
    // HABILIDADES ECONOMÍA
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

    // ==============================
    // NUEVO: DIAS EXTRA
    // ==============================

    public void AddBonusDays(int days)
    {
        // Si quieres evitar repetir la misma mejora (ej: +5 dos veces), cámbialo:
        // if (days <= bonusDaysPermanent) return;  (pero esto compararía mal)
        // Lo correcto sería manejarlo por "nivel" en el árbol.
        bonusDaysPermanent += days;
        SaveData();
    }

    // ==============================
    // SAVE / LOAD
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

        // NUEVO
        PlayerPrefs.SetInt("BonusDaysPermanent", bonusDaysPermanent);

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

        // NUEVO
        bonusDaysPermanent = PlayerPrefs.GetInt("BonusDaysPermanent", 0);
    }
}
