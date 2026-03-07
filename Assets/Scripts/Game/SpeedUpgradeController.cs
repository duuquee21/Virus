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

        if (Guardado.instance != null)
            Guardado.instance.AddSpeedMultiplier(speedIncrement);

        ApplySpeed();
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        ApplySpeed();
    }

    void ApplySpeed()
    {
        // velocidad base según nivel del upgrade
        float calculatedSpeed = baseSpeed + ((currentLevel - 1) * speedIncrement);

        // multiplicador de habilidades guardadas
        float skillMultiplier = (Guardado.instance != null) ? Guardado.instance.speedMultiplier : 1f;

        // velocidad final
        float finalSpeed = calculatedSpeed * skillMultiplier;

        // aplicamos al movimiento del virus
        if (VirusMovement.instance != null)
        {
            VirusMovement.instance.SetSpeed(finalSpeed);
        }
    }

    public int GetCurrentLevel() => currentLevel;

    public void ResetUpgrade()
    {
        currentLevel = 1;
        ApplySpeed();
    }
}