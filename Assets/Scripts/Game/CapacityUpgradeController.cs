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
        if (Guardado.instance != null)
            currentLevel = Guardado.instance.capacityLevel;
        else
            currentLevel = 1;

        ApplyCapacity();
    }

    public void UpgradeCapacity()
    {
        currentLevel++;

        if (Guardado.instance != null)
        {
            Guardado.instance.capacityLevel = currentLevel;
            Guardado.instance.SaveData();
        }

        ApplyCapacity();
    }

    // NUEVO � para bonus inicial permanente
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplyCapacity();
    }

    void ApplyCapacity()
    {
        if (LevelManager.instance == null) return;
        int newCapacity = UpgradeManager.instance.GetCapacityValueByTable(currentLevel);
      

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
