using UnityEngine;

public class TimeUpgradeController : MonoBehaviour
{
    public static TimeUpgradeController instance;

    [Header("Tiempo base por día")]
    public float baseTime = 20f;

    [Header("Incremento por nivel")]
    public float timeStep = 5f;

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplyTime();
    }

    public void UpgradeTime()
    {
        currentLevel++;
        ApplyTime();
    }

    void ApplyTime()
    {
        if (LevelManager.instance == null) return;

        float newTime = baseTime + (currentLevel - 1) * timeStep;
        LevelManager.instance.gameDuration = newTime;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
