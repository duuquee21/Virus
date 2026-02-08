using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneItem : MonoBehaviour
{
    [Header("Configuración")]
    public int mapIndex = 1;
    public int cost = 100;
    public string zoneName = "Parque Central";
    public string zoneBenefit = "Infección x1";

    [Header("Referencias UI")]
    public Button myButton;
    public Image planetImage;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI benefitText;

    [Header("Colores de Estado")]
    public Color luzColor = Color.white;
    public Color apagadoColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private bool isUnlocked = false;

    void OnEnable()
    {
        if (mapIndex == 0)
        {
            isUnlocked = true;
        }
        else
        {
            bool yaCompradoEnPrefs = PlayerPrefs.GetInt("ZoneUnlocked_" + mapIndex, 0) == 1;
            isUnlocked = yaCompradoEnPrefs;
        }
        UpdateUI();
    }

    int GetFinalCost()
    {
        if (Guardado.instance != null && Guardado.instance.zoneDiscountActive)
        {
            return cost / 2;
        }
        return cost;
    }

    public void OnClickButton()
    {
        if (isUnlocked) EquipZone();
        else TryBuyZone();
    }

    void TryBuyZone()
    {
        if (LevelManager.instance == null) return;

        int finalPrice = GetFinalCost();
        if (LevelManager.instance.contagionCoins >= finalPrice)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayBuyZone();

            LevelManager.instance.contagionCoins -= finalPrice;
            LevelManager.instance.UpdateUI();

            isUnlocked = true;
            PlayerPrefs.SetInt("ZoneUnlocked_" + mapIndex, 1);
            PlayerPrefs.Save();

            EquipZone();
        }
        else
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayError();
        }
    }

    void EquipZone()
    {
        if (LevelManager.instance != null)
        {
            LevelManager.instance.ActivateMap(mapIndex);

            PopulationManager popManager = Object.FindFirstObjectByType<PopulationManager>();
            if (popManager != null)
            {
                popManager.SetZonePrefab(mapIndex);
            }

            ZoneItem[] todosLosScripts = Object.FindObjectsByType<ZoneItem>(FindObjectsSortMode.None);
            foreach (ZoneItem item in todosLosScripts)
            {
                item.UpdateUI();
            }
        }
    }

    // --- NUEVOS MÉTODOS PARA CORREGIR LOS ERRORES ---

    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    public bool CanAfford()
    {
        if (LevelManager.instance == null) return false;
        return LevelManager.instance.contagionCoins >= GetFinalCost();
    }

    // ------------------------------------------------

    public void UpdateUI()
    {
        if (LevelManager.instance == null) return;

        int currentEquipped = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        bool esElEquipado = (currentEquipped == mapIndex);

        int currentPrice = GetFinalCost();
        bool puedeComprarlo = LevelManager.instance.contagionCoins >= currentPrice;

        int multiplicadorCalculado = mapIndex + 1;
        zoneBenefit = "Infección x" + multiplicadorCalculado;

        if (benefitText != null)
        {
            benefitText.text = zoneBenefit;
            benefitText.color = (multiplicadorCalculado >= 3) ? new Color(1f, 0.8f, 0f) : Color.white;
        }

        if (nameText != null) nameText.text = zoneName;

        if (isUnlocked)
        {
            if (esElEquipado)
            {
                statusText.text = "ACTIVO";
                myButton.interactable = false;
                if (planetImage != null) planetImage.color = apagadoColor;
            }
            else
            {
                statusText.text = "EQUIPAR";
                myButton.interactable = true;
                if (planetImage != null) planetImage.color = luzColor;
            }
        }
        else
        {
            statusText.text = "COMPRAR (" + currentPrice + ")";
            myButton.interactable = puedeComprarlo;

            if (planetImage != null)
            {
                planetImage.color = puedeComprarlo ? luzColor : apagadoColor;
            }
        }
    }
}