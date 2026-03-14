using UnityEngine;

public class InfectionSpeedUpgradeController : MonoBehaviour
{
    public static InfectionSpeedUpgradeController instance;

    [Header("Configuraci�n de Infecci�n")]
    public float baseInfectTime = 2f;
    public float reductionStep = 0.4f;
    [SerializeField] float minInfectTime = 0.2f; // L�mite para que no sea instant�neo o negativo

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (Guardado.instance != null)
            currentLevel = Guardado.instance.infectionSpeedLevel;
        else
            currentLevel = 1;

        ApplySpeed();
    }

    // Cada vez que se llama, el contagio es 0.4s más rápido
    public void UpgradeInfectionSpeed()
    {
        currentLevel++;

        float newTime = baseInfectTime - (currentLevel - 1) * reductionStep;
        newTime = Mathf.Max(newTime, minInfectTime);

        if (Guardado.instance != null)
        {
            Guardado.instance.SetInfectionSpeedBonus(baseInfectTime / newTime);
            Guardado.instance.infectionSpeedLevel = currentLevel;
            Guardado.instance.SaveData();
        }

        ApplySpeed();
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplySpeed();
    }

    void ApplySpeed()
    {
        // F�rmula: Tiempo Base - (Niveles extra * reducci�n)
        float newTime = baseInfectTime - (currentLevel - 1) * reductionStep;

        // Aplicamos el l�mite m�nimo de seguridad
        newTime = Mathf.Max(newTime, minInfectTime);

        PersonaInfeccion.globalInfectTime = newTime;

        Debug.Log($"Velocidad de Infecci�n: {newTime}s (Nivel {currentLevel})");
    }

    public int GetCurrentLevel() => currentLevel;

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplySpeed();
    }
}