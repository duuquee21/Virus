using UnityEngine;

public class SpeedUpgradeController : MonoBehaviour
{
    public static SpeedUpgradeController instance;

    [Header("Configuración de Velocidad")]
    [SerializeField] float baseSpeed = 5f;      // Velocidad inicial nivel 1
    [SerializeField] float speedIncrement = 0.25f; // Lo que se suma por cada upgrade

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
        if (Guardado.instance != null)
            currentLevel = Guardado.instance.speedLevel;
        else
            currentLevel = 1;

        ApplySpeed();
    }
    // Método para sumar un nivel (+0.5f a la velocidad)
    public void UpgradeSpeed()
    {
        currentLevel++;

        if (Guardado.instance != null)
        {
            Guardado.instance.speedLevel = currentLevel;
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
        float calculatedSpeed = baseSpeed + ((currentLevel - 1) * speedIncrement);

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