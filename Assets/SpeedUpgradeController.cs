using UnityEngine;

public class SpeedUpgradeController : MonoBehaviour
{
    public static SpeedUpgradeController instance;

    public float baseSpeed = 80f;
    public float speedStep = 20f;

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplySpeed(); // ahora sí es seguro
    }

    public void UpgradeSpeed()
    {
        currentLevel++;
        ApplySpeed();
    }

    void ApplySpeed()
    {
        if (VirusMovement.instance == null) return;

        float newSpeed = baseSpeed + (currentLevel - 1) * speedStep;
        VirusMovement.instance.SetSpeed(newSpeed);
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
