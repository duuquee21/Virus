using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;

    [Header("Referencias UI")]
    public TextMeshProUGUI radioLevelText;
    public TextMeshProUGUI capacityLevelText;
    public TextMeshProUGUI speedLevelText;
    public TextMeshProUGUI timeLevelText;
    public TextMeshProUGUI infectLevelText;

    [Header("Tablas de Valores (Balance)")]
    // Esta es la tabla que el error dice que no encuentra
    public int[] capacityValues = { 10, 20, 30, 50, 75, 100 };

    // FUNCIÓN RECUPERADA: Devuelve el valor de capacidad según el nivel
    public int GetCapacityValueByTable(int level)
    {
        // Clamp asegura que si pides un nivel mayor al array, no de error y devuelva el último
        int index = Mathf.Clamp(level - 1, 0, capacityValues.Length - 1);
        return capacityValues[index];
    }
    void Awake() => instance = this;

    void Start() => RefreshAllUI();

    // Este método se llama desde el SkillNode al comprar algo
    // o al abrir el panel de mejoras.
    public void RefreshAllUI()
    {
        // Ahora currentLevel empezará siendo 1 por defecto
        UpdateLevelText(radioLevelText, VirusRadiusController.instance.GetCurrentLevel(), 5);
        UpdateLevelText(capacityLevelText, CapacityUpgradeController.instance.GetCurrentLevel(), 5);
        UpdateLevelText(speedLevelText, SpeedUpgradeController.instance.GetCurrentLevel(), 4);
        UpdateLevelText(timeLevelText, TimeUpgradeController.instance.GetCurrentLevel(), 5);
        UpdateLevelText(infectLevelText, InfectionSpeedUpgradeController.instance.GetCurrentLevel(), 4);
    }

    void UpdateLevelText(TextMeshProUGUI text, int currentLevel, int maxLevel)
    {
        if (text == null) return;

        if (currentLevel >= maxLevel)
            text.text = "Nivel FULL";
        else
            text.text = "Nivel " + currentLevel;
    }

    // Los botones del panel ahora pueden estar bloqueados o simplemente 
    // no tener función, ya que la mejora se compra en el Árbol.
}