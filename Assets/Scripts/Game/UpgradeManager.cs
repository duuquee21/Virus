using UnityEngine;
using TMPro;
using System.Collections;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;

    [Header("Radio Upgrade")]
    public TextMeshProUGUI radioLevelText;
    public TextMeshProUGUI radioCostText;

    [Header("Capacity Upgrade")]
    public TextMeshProUGUI capacityLevelText;
    public TextMeshProUGUI capacityCostText;

    [Header("Speed Upgrade")]
    public TextMeshProUGUI speedLevelText;
    public TextMeshProUGUI speedCostText;

    [Header("Time Upgrade")]
    public TextMeshProUGUI timeLevelText;
    public TextMeshProUGUI timeCostText;

    [Header("Infection Speed Upgrade")]
    public TextMeshProUGUI infectLevelText;
    public TextMeshProUGUI infectCostText;

    bool isBlinking = false;

    // ===============================
    // TABLAS DE BALANCE
    // ===============================

    int[] capacityBuyCosts = { 10, 50, 200, 1000, 5000 };
    int[] capacityValues = { 10, 20, 30, 50, 75, 100 };

    int[] radiusBuyCosts = { 20, 40, 200, 500, 1500 };
    int[] speedBuyCosts = { 50, 200, 1000, 2000 };
    int[] timeBuyCosts = { 50, 200, 500, 1000, 5000 };
    int[] infectBuyCosts = { 10, 100, 1000, 5000 };

    // ===============================
    // SINGLETON
    // ===============================

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplyInitialUpgradeBonus();
    }

    void Update()
    {
        UpdateCapacityUI();
        UpdateRadioUI();
        UpdateSpeedUI();
        UpdateTimeUI();
        UpdateInfectUI();
    }

    // ===============================
    // HELPERS FULL
    // ===============================

    bool IsCapacityMax(int level) => level > capacityBuyCosts.Length;
    bool IsRadiusMax(int level) => level > radiusBuyCosts.Length;
    bool IsSpeedMax(int level) => level > speedBuyCosts.Length;
    bool IsTimeMax(int level) => level > timeBuyCosts.Length;
    bool IsInfectMax(int level) => level > infectBuyCosts.Length;

    int GetCapacityBuyCost(int level) => capacityBuyCosts[level - 1];
    int GetRadiusBuyCost(int level) => radiusBuyCosts[level - 1];
    int GetSpeedBuyCost(int level) => speedBuyCosts[level - 1];
    int GetTimeBuyCost(int level) => timeBuyCosts[level - 1];
    int GetInfectBuyCost(int level) => infectBuyCosts[level - 1];

    // ===============================
    // VALOR REAL DE CAPACIDAD
    // ===============================

    public int GetCapacityValueByTable(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, capacityValues.Length - 1);
        return capacityValues[index];
    }

    // ===============================
    // UI
    // ===============================

    void UpdateCapacityUI()
    {
        int level = CapacityUpgradeController.instance.GetCurrentLevel();

        if (IsCapacityMax(level))
        {
            capacityLevelText.text = "Nivel FULL";
            capacityCostText.text = "MAX";
            return;
        }

        capacityLevelText.text = "Nivel " + level;
        capacityCostText.text = "Coste " + GetCapacityBuyCost(level);
    }

    void UpdateRadioUI()
    {
        int level = VirusRadiusController.instance.GetCurrentLevel();

        if (IsRadiusMax(level))
        {
            radioLevelText.text = "Nivel FULL";
            radioCostText.text = "MAX";
            return;
        }

        radioLevelText.text = "Nivel " + level;
        radioCostText.text = "Coste " + GetRadiusBuyCost(level);
    }

    void UpdateSpeedUI()
    {
        int level = SpeedUpgradeController.instance.GetCurrentLevel();

        if (IsSpeedMax(level))
        {
            speedLevelText.text = "Nivel FULL";
            speedCostText.text = "MAX";
            return;
        }

        speedLevelText.text = "Nivel " + level;
        speedCostText.text = "Coste " + GetSpeedBuyCost(level);
    }

    void UpdateTimeUI()
    {
        int level = TimeUpgradeController.instance.GetCurrentLevel();

        if (IsTimeMax(level))
        {
            timeLevelText.text = "Nivel FULL";
            timeCostText.text = "MAX";
            return;
        }

        timeLevelText.text = "Nivel " + level;
        timeCostText.text = "Coste " + GetTimeBuyCost(level);
    }

    void UpdateInfectUI()
    {
        int level = InfectionSpeedUpgradeController.instance.GetCurrentLevel();

        if (IsInfectMax(level))
        {
            infectLevelText.text = "Nivel FULL";
            infectCostText.text = "MAX";
            return;
        }

        infectLevelText.text = "Nivel " + level;
        infectCostText.text = "Coste " + GetInfectBuyCost(level);
    }

    // ===============================
    // COMPRAS
    // ===============================

    public void BuyCapacityUpgrade()
    {
        int level = CapacityUpgradeController.instance.GetCurrentLevel();
        if (IsCapacityMax(level)) return;

        TryBuy(GetCapacityBuyCost(level),
            () => CapacityUpgradeController.instance.UpgradeCapacity(),
            capacityCostText);
    }

    public void BuyRadioUpgrade()
    {
        int level = VirusRadiusController.instance.GetCurrentLevel();
        if (IsRadiusMax(level)) return;

        TryBuy(GetRadiusBuyCost(level),
            () => VirusRadiusController.instance.UpgradeRadius(),
            radioCostText);
    }

    public void BuySpeedUpgrade()
    {
        int level = SpeedUpgradeController.instance.GetCurrentLevel();
        if (IsSpeedMax(level)) return;

        TryBuy(GetSpeedBuyCost(level),
            () => SpeedUpgradeController.instance.UpgradeSpeed(),
            speedCostText);
    }

    public void BuyTimeUpgrade()
    {
        int level = TimeUpgradeController.instance.GetCurrentLevel();
        if (IsTimeMax(level)) return;

        TryBuy(GetTimeBuyCost(level),
            () => TimeUpgradeController.instance.UpgradeTime(),
            timeCostText);
    }

    public void BuyInfectionUpgrade()
    {
        int level = InfectionSpeedUpgradeController.instance.GetCurrentLevel();
        if (IsInfectMax(level)) return;

        TryBuy(GetInfectBuyCost(level),
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

    // ===============================
    // BONUS PERMANENTE DEL ÁRBOL
    // ===============================

    public void ApplyInitialUpgradeBonus()
    {
        if (Guardado.instance.freeInitialUpgrade == -1) return;

        switch (Guardado.instance.freeInitialUpgrade)
        {
            case 0: VirusRadiusController.instance.SetLevel(2); break;
            case 1: CapacityUpgradeController.instance.SetLevel(2); break;
            case 2: SpeedUpgradeController.instance.SetLevel(2); break;
            case 3: TimeUpgradeController.instance.SetLevel(2); break;
            case 4: InfectionSpeedUpgradeController.instance.SetLevel(2); break;
        }
    }
}
