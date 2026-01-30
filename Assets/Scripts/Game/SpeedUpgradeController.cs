using UnityEngine;

public class SpeedUpgradeController : MonoBehaviour
{
    public static SpeedUpgradeController instance;

    int currentLevel = 1;

    // VALORES SEGÚN TU TABLA (80 = 100%)
    float[] speedValues =
    {
        80f,   // Nivel 1 (base)
        96f,   // 120%
        112f,  // 140%
        136f,  // 170%
        160f   // 200%
    };

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplySpeed();
    }

    public void UpgradeSpeed()
    {
        if (currentLevel < speedValues.Length)
        {
            currentLevel++;
            ApplySpeed();
        }
    }

    void ApplySpeed()
    {
        if (VirusMovement.instance == null) return;

        int index = Mathf.Clamp(currentLevel - 1, 0, speedValues.Length - 1);
        VirusMovement.instance.SetSpeed(speedValues[index]);
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplySpeed();
    }

    // Para bonus del árbol
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, speedValues.Length);
        ApplySpeed();
    }
}
