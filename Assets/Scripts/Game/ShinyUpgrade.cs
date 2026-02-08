using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShinyUpgrade : MonoBehaviour
{
    [Header("Configuración de la Mejora")]
    public string upgradeName = "Mejora Épica";
    public int cost = 100; // Ahora representa monedas normales
    public int maxLevel = 5;

    // Clave única para guardar (ej: "DamageUpgrade", "SpeedUpgrade")
    public string saveKey = "MyUpgradeID";

    [Header("Referencias UI")]
    public Button buyButton;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI levelText;

    private int currentLevel = 0;

    void Start()
    {
        // Cargar nivel guardado
        currentLevel = PlayerPrefs.GetInt(saveKey, 0);
        UpdateButtonUI();
    }

    void Update()
    {
        // Comprobamos constantemente si podemos comprar con monedas normales
        if (LevelManager.instance != null)
        {
            // Cambiado: Ahora consulta contagionCoins en lugar de shinyDNA
            bool canAfford = LevelManager.instance.contagionCoins >= cost;
            bool notMaxed = currentLevel < maxLevel;

            buyButton.interactable = canAfford && notMaxed;
        }
    }

    public void BuyUpgrade()
    {
        if (LevelManager.instance == null) return;

        // 1. COMPROBAR DINERO (Monedas normales)
        if (LevelManager.instance.contagionCoins >= cost && currentLevel < maxLevel)
        {
            // 2. PAGAR (Restamos monedas normales)
            LevelManager.instance.contagionCoins -= cost;

            // 3. SUBIR NIVEL
            currentLevel++;
            PlayerPrefs.SetInt(saveKey, currentLevel);
            PlayerPrefs.Save();

            // 4. APLICAR EFECTO
            ApplyUpgradeEffect();

            // 5. ACTUALIZAR VISUALES
            UpdateButtonUI();

            // Refrescar toda la UI del juego
            LevelManager.instance.UpdateUI();
        }
    }

    void UpdateButtonUI()
    {
        if (currentLevel >= maxLevel)
        {
            costText.text = "MAX";
            levelText.text = "Lvl " + currentLevel;
        }
        else
        {
            // Cambiado el texto de "Shinies" a "Monedas" o solo el número
            costText.text = cost + " Monedas";
            levelText.text = "Lvl " + currentLevel;
        }
    }

    // --- AQUÍ ES DONDE DEFINES QUÉ HACE LA MEJORA ---
    void ApplyUpgradeEffect()
    {
        Debug.Log("¡Mejora " + upgradeName + " comprada! Nivel actual: " + currentLevel);

        // Ejemplo de uso:
        // if (saveKey == "StartCoins") { Guardado.instance.SetStartingCoins(50 * currentLevel); }
    }
}