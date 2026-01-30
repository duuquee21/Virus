using UnityEngine;

public class CapacityUpgradeController : MonoBehaviour
{
    public static CapacityUpgradeController instance;

    public int baseCapacity = 5;
    public int capacityStep = 2;

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplyCapacity();
    }

    public void UpgradeCapacity()
    {
        currentLevel++;
        ApplyCapacity();
    }

    // NUEVO — para bonus inicial permanente
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplyCapacity();
    }

    void ApplyCapacity()
    {
        if (LevelManager.instance == null) return;
        int newCapacity = UpgradeManager.instance.GetCapacityValueByTable(currentLevel);
        LevelManager.instance.maxInfectionsPerRound = newCapacity;

    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplyCapacity();
    }
}
