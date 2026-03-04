using UnityEngine;

public class TimeUpgradeController : MonoBehaviour
{
    public static TimeUpgradeController instance;

    [Header("Configuración de Tiempo")]
    [SerializeField] float baseTime = 10f;      // El valor inicial en el nivel 1
    [SerializeField] float timeIncrement = 2.5f; // Lo que se suma por nivel

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
        currentLevel++; // Sube 1 nivel
        ApplyTime();    // Esto calculará: (Nuevo Nivel - 1) * 2.5
    }

    void ApplyTime()
    {
        if (LevelManager.instance == null) return;

        // Fórmula lineal: El tiempo base + 2.5 por cada nivel extra ganado
        float calculatedTime = baseTime + ((currentLevel - 1) * 2.5f);

        LevelManager.instance.gameDuration = calculatedTime;
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

    public void SetLevel(int level)
    {
        // Eliminamos el límite del array, pero aseguramos que el nivel no sea menor a 1
        currentLevel = Mathf.Max(1, level);
        ApplyTime();
    }
}