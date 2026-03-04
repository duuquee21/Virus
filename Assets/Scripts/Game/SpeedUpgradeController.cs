using UnityEngine;

public class SpeedUpgradeController : MonoBehaviour
{
    public static SpeedUpgradeController instance;

    [Header("Configuración de Velocidad")]
    [SerializeField] float baseSpeed = 5f;      // Velocidad inicial nivel 1
    [SerializeField] float speedIncrement = 0.5f; // Lo que se suma por cada upgrade

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
    public void UpgradeSpeed()
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
        // 1. Calculamos la velocidad base del nivel actual
        // Si baseSpeed es 20 y nivel 1: 20 + (0 * 0.5) = 20
        float calculatedSpeed = baseSpeed + ((currentLevel - 1) * speedIncrement);

        // 2. Le enviamos esa nueva velocidad al script de movimiento
        if (VirusMovement.instance != null)
        {
            VirusMovement.instance.SetSpeed(calculatedSpeed);
        }
    }

    public int GetCurrentLevel() => currentLevel;

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplySpeed();
    }
}