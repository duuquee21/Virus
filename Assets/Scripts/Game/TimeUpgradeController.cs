using UnityEngine;

public class TimeUpgradeController : MonoBehaviour
{
    public static TimeUpgradeController instance;

    float[] timeValues = { 10f, 12.5f, 15f, 17.5f, 20f, 22.5f };

    int currentLevel = 1;

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
        if (currentLevel >= timeValues.Length) return;

        currentLevel++;
        ApplyTime();
    }

    void ApplyTime()
    {
        if (LevelManager.instance == null) return;

        int index = Mathf.Clamp(currentLevel - 1, 0, timeValues.Length - 1);
        LevelManager.instance.gameDuration = timeValues[index];
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplyTime();
    }

    // 🔥 usado por bonus permanente
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, timeValues.Length);
        ApplyTime();
    }
}
