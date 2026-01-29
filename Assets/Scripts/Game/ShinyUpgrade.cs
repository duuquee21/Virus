using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShinyUpgrade : MonoBehaviour
{
    [Header("Configuración de la Mejora")]
    public string upgradeName = "Mejora Épica";
    public int cost = 1; // Cuántos Shinies cuesta
    public int maxLevel = 5; // Nivel máximo
    
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
        // (Opcional) Comprobamos constantemente si podemos comprar para activar/desactivar botón
        if (Guardado.instance != null)
        {
            // Solo es interactuable si tenemos dinero Y no hemos llegado al máximo
            bool canAfford = Guardado.instance.shinyDNA >= cost;
            bool notMaxed = currentLevel < maxLevel;
            
            buyButton.interactable = canAfford && notMaxed;
        }
    }

    public void BuyUpgrade()
    {
        if (Guardado.instance == null) return;

        // 1. COMPROBAR DINERO
        if (Guardado.instance.shinyDNA >= cost && currentLevel < maxLevel)
        {
            // 2. PAGAR (Usamos números negativos para restar)
            Guardado.instance.AddShinyDNA(-cost);

            // 3. SUBIR NIVEL
            currentLevel++;
            PlayerPrefs.SetInt(saveKey, currentLevel);
            PlayerPrefs.Save();

            // 4. APLICAR EFECTO (Aquí pondrás tu lógica)
            ApplyUpgradeEffect();

            // 5. ACTUALIZAR VISUALES
            UpdateButtonUI();
            
            // Avisamos al LevelManager para que actualice el contador de monedas total
            if (LevelManager.instance != null) LevelManager.instance.UpdateUI();
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
            costText.text = cost + " Shinies";
            levelText.text = "Lvl " + currentLevel;
        }
    }

    // --- AQUÍ ES DONDE DEFINES QUÉ HACE LA MEJORA ---
    void ApplyUpgradeEffect()
    {
        Debug.Log("¡Mejora " + upgradeName + " comprada! Nivel actual: " + currentLevel);

        // EJEMPLO: Si esta mejora es para empezar con más monedas normales
        if (saveKey == "StartCoins")
        {
            
        }
        
        // EJEMPLO: Aumentar probabilidad de Shiny
        if (saveKey == "ShinyLuck")
        {
           
        }
    }
}