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

    void ApplyCapacity()
    {
        if (LevelManager.instance == null) return;

        int newCapacity = baseCapacity + (currentLevel - 1) * capacityStep;
        LevelManager.instance.maxInfectionsPerRound = newCapacity;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
