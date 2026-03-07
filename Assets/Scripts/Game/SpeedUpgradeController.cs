using UnityEngine;

public class SpeedUpgradeController : MonoBehaviour
{
    public static SpeedUpgradeController instance;

    [Header("Configuración de Velocidad")]
    [SerializeField] float baseSpeed = 60f;      // Velocidad inicial nivel 1
    [SerializeField] float speedIncrement = 1f; // Lo que se suma por cada upgrade

    private int currentLevel = 1;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        ApplySpeed();
    }

    // Método para sumar un nivel (+0.5f a la velocidad)
 

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplySpeed();
    }

    void ApplySpeed()
    {
        // 1. Calculamos la velocidad base puramente por nivel
        // Si nivel 1 = 60, nivel 2 = 61...
        float calculatedSpeed = baseSpeed + ((currentLevel - 1) * speedIncrement);

        // 2. Obtenemos el multiplicador (Si es un x2, x3, etc.)
        // Asegúrate de que en Guardado.instance.speedMultiplier el valor inicial sea 1f
        float skillMultiplier = (Guardado.instance != null) ? Guardado.instance.speedMultiplier : 1f;

        // 3. Enviamos el valor final al virus
        if (VirusMovement.instance != null)
        {
            // IMPORTANTE: Solo enviamos el cálculo final
            VirusMovement.instance.SetSpeed(calculatedSpeed * skillMultiplier);
        }
    }

    public void UpgradeSpeed()
    {
        currentLevel++;


        ApplySpeed();
    }

    public int GetCurrentLevel() => currentLevel;

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplySpeed();
    }

    public float GetCurrentSpeed()
    {
        float calculatedSpeed = baseSpeed + ((currentLevel - 1) * speedIncrement);
        float skillMultiplier = (Guardado.instance != null) ? Guardado.instance.speedMultiplier : 1f;
        return calculatedSpeed * skillMultiplier;
    }
}