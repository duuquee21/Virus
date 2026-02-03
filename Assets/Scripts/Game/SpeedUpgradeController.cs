using UnityEngine;

public class SpeedUpgradeController : MonoBehaviour
{
    public static SpeedUpgradeController instance;

    // Usamos currentLevel (empezando en 1) como tienes en el resto del script
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

        // Pasamos la velocidad de la tabla al VirusMovement
        // VirusMovement se encargará de multiplicar esto por el bono del árbol
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

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, speedValues.Length);
        ApplySpeed();
    }

    // --- FUNCIÓN CORREGIDA ---
    public float GetFinalSpeed()
    {
        // Corregido: Usamos currentLevel - 1 para el índice
        int index = Mathf.Clamp(currentLevel - 1, 0, speedValues.Length - 1);
        float speedFromLevel = speedValues[index];

        float multiplier = (Guardado.instance != null) ? Guardado.instance.speedMultiplier : 1f;

        return speedFromLevel * multiplier;
    }
}