using UnityEngine;

public class Guardado : MonoBehaviour
{
    public static Guardado instance;

    public int totalInfected = 0;
    public int shinyDNA = 0;

    // -------- PERMANENTES DEL ÁRBOL --------

    public int freeInitialUpgrade = -1;   // upgrade random inicial
    public int coinMultiplier = 1;        // x1, x2, x3, x4, x5
    public int startingCoins = 0;         // 0, 50, 100, etc

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

    // -------- ECONOMÍA BASE --------

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

    // -------- RANDOM UPGRADE INICIAL --------

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

    // -------- HABILIDADES ECONOMÍA --------

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

    // -------- SAVE / LOAD --------

    void SaveData()
    {
        PlayerPrefs.SetInt("TotalInfected", totalInfected);
        PlayerPrefs.SetInt("TotalShinyDNA", shinyDNA);

        PlayerPrefs.SetInt("FreeInitialUpgrade", freeInitialUpgrade);
        PlayerPrefs.SetInt("CoinMultiplier", coinMultiplier);
        PlayerPrefs.SetInt("StartingCoins", startingCoins);

        PlayerPrefs.Save();
    }

    void LoadData()
    {
        totalInfected = PlayerPrefs.GetInt("TotalInfected", 0);
        shinyDNA = PlayerPrefs.GetInt("TotalShinyDNA", 0);

        freeInitialUpgrade = PlayerPrefs.GetInt("FreeInitialUpgrade", -1);
        coinMultiplier = PlayerPrefs.GetInt("CoinMultiplier", 1);
        startingCoins = PlayerPrefs.GetInt("StartingCoins", 0);
    }
}
