using UnityEngine;

public class InfectionSpeedUpgradeController : MonoBehaviour
{
    public static InfectionSpeedUpgradeController instance;

    [Header("Configuración de Infección")]
    public float baseInfectTime = 2f;
    public float reductionStep = 0.4f;
    [SerializeField] float minInfectTime = 0.2f; // Límite para que no sea instantáneo o negativo

    private int currentLevel = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ApplySpeed();
    }

    // Cada vez que se llama, el contagio es 0.4s más rápido
    public void UpgradeInfectionSpeed()
    {
        currentLevel++;
        ApplySpeed();
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplySpeed();
    }

    void ApplySpeed()
    {
        // Fórmula: Tiempo Base - (Niveles extra * reducción)
        float newTime = baseInfectTime - (currentLevel - 1) * reductionStep;

        // Aplicamos el límite mínimo de seguridad
        newTime = Mathf.Max(newTime, minInfectTime);

        PersonaInfeccion.globalInfectTime = newTime;

        Debug.Log($"Velocidad de Infección: {newTime}s (Nivel {currentLevel})");
    }

    public int GetCurrentLevel() => currentLevel;

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplySpeed();
    }
}