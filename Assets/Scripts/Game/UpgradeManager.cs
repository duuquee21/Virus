using UnityEngine;
using TMPro;
using System.Collections;

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

    bool isBlinking = false;

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
        TryBuy(GetRadioCost(level),
            () => VirusRadiusController.instance.UpgradeRadius(),
            radioCostText);
    }

    public void BuyCapacityUpgrade()
    {
        int level = CapacityUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetCapacityCost(level),
            () => CapacityUpgradeController.instance.UpgradeCapacity(),
            capacityCostText);
    }

    public void BuySpeedUpgrade()
    {
        int level = SpeedUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetSpeedCost(level),
            () => SpeedUpgradeController.instance.UpgradeSpeed(),
            speedCostText);
    }

    public void BuyTimeUpgrade()
    {
        int level = TimeUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetTimeCost(level),
            () => TimeUpgradeController.instance.UpgradeTime(),
            timeCostText);
    }

    public void BuyInfectionUpgrade()
    {
        int level = InfectionSpeedUpgradeController.instance.GetCurrentLevel();
        TryBuy(GetInfectCost(level),
            () => InfectionSpeedUpgradeController.instance.UpgradeInfectionSpeed(),
            infectCostText);
    }

    void TryBuy(int cost, System.Action upgradeAction, TextMeshProUGUI costText)
    {
        if (LevelManager.instance.contagionCoins < cost)
        {
            if (!isBlinking)
                StartCoroutine(BlinkRoutine(costText));

            return;
        }

        LevelManager.instance.contagionCoins -= cost;
        upgradeAction.Invoke();
        LevelManager.instance.UpdateUI();
    }

    IEnumerator BlinkRoutine(TextMeshProUGUI text)
    {
        isBlinking = true;
        Color normal = Color.white;

        for (int i = 0; i < 3; i++)
        {
            text.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            text.color = normal;
            yield return new WaitForSeconds(0.15f);
        }

        text.color = normal;
        isBlinking = false;
    }
}
