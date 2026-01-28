using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;

    [Header("Radio Upgrade")]
    public int radioBaseCost = 3;
    public TextMeshProUGUI radioLevelText;
    public TextMeshProUGUI radioCostText;

    [Header("Capacity Upgrade")]
    public int capacityBaseCost = 4;
    public TextMeshProUGUI capacityLevelText;
    public TextMeshProUGUI capacityCostText;

    [Header("Speed Upgrade")]
    public int speedBaseCost = 5;
    public TextMeshProUGUI speedLevelText;
    public TextMeshProUGUI speedCostText;

    [Header("Time Upgrade")]
    public int timeBaseCost = 6;
    public TextMeshProUGUI timeLevelText;
    public TextMeshProUGUI timeCostText;

    [Header("Infection Speed Upgrade")]
    public int infectBaseCost = 7;
    public TextMeshProUGUI infectLevelText;
    public TextMeshProUGUI infectCostText;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        int radioLevel = VirusRadiusController.instance.GetCurrentLevel();
        radioLevelText.text = "Nivel " + radioLevel;
        radioCostText.text = "Coste " + GetRadioCost(radioLevel);

        int capacityLevel = CapacityUpgradeController.instance.GetCurrentLevel();
        capacityLevelText.text = "Nivel " + capacityLevel;
        capacityCostText.text = "Coste " + GetCapacityCost(capacityLevel);

        int speedLevel = SpeedUpgradeController.instance.GetCurrentLevel();
        speedLevelText.text = "Nivel " + speedLevel;
        speedCostText.text = "Coste " + GetSpeedCost(speedLevel);

        int timeLevel = TimeUpgradeController.instance.GetCurrentLevel();
        timeLevelText.text = "Nivel " + timeLevel;
        timeCostText.text = "Coste " + GetTimeCost(timeLevel);

        int infectLevel = InfectionSpeedUpgradeController.instance.GetCurrentLevel();
        infectLevelText.text = "Nivel " + infectLevel;
        infectCostText.text = "Coste " + GetInfectCost(infectLevel);
    }

    int GetRadioCost(int level) => Mathf.RoundToInt(radioBaseCost * Mathf.Pow(2, level - 1));
    int GetCapacityCost(int level) => Mathf.RoundToInt(capacityBaseCost * Mathf.Pow(2, level - 1));
    int GetSpeedCost(int level) => Mathf.RoundToInt(speedBaseCost * Mathf.Pow(2, level - 1));
    int GetTimeCost(int level) => Mathf.RoundToInt(timeBaseCost * Mathf.Pow(2, level - 1));
    int GetInfectCost(int level) => Mathf.RoundToInt(infectBaseCost * Mathf.Pow(2, level - 1));

    public void BuyRadioUpgrade()
    {
        int level = VirusRadiusController.instance.GetCurrentLevel();
        TryBuy(GetRadioCost(level), () => VirusRadiusController.instance.UpgradeRadius());
    }

    public void BuyCapacityUpgrade()
    {
        int level = CapacityUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetCapacityCost(level), () => CapacityUpgradeController.instance.UpgradeCapacity());
    }

    public void BuySpeedUpgrade()
    {
        int level = SpeedUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetSpeedCost(level), () => SpeedUpgradeController.instance.UpgradeSpeed());
    }

    public void BuyTimeUpgrade()
    {
        int level = TimeUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetTimeCost(level), () => TimeUpgradeController.instance.UpgradeTime());
    }

    public void BuyInfectionUpgrade()
    {
        int level = InfectionSpeedUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetInfectCost(level), () => InfectionSpeedUpgradeController.instance.UpgradeInfectionSpeed());
    }

    void TryBuy(int cost, System.Action upgradeAction)
    {
        if (LevelManager.instance.contagionCoins < cost) return;

        LevelManager.instance.contagionCoins -= cost;
        upgradeAction.Invoke();
        LevelManager.instance.UpdateUI();
    }
}
