using UnityEngine;

public class InfectionSpeedUpgradeController : MonoBehaviour
{
    public static InfectionSpeedUpgradeController instance;

    [Header("Tiempo base de contagio (nivel 1)")]
    public float baseInfectTime = 2f;

    [Header("Reducción por nivel")]
    public float reductionStep = 0.4f;

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplySpeed();
    }

    public void UpgradeInfectionSpeed()
    {
        currentLevel++;
        ApplySpeed();
    }

    // NUEVO — para bonus inicial permanente
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplySpeed();
    }

    void ApplySpeed()
    {
        float newTime = baseInfectTime - (currentLevel - 1) * reductionStep;
        if (newTime < 0.2f) newTime = 0.2f;

        PersonaInfeccion.globalInfectTime = newTime;
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
}
