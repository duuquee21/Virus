using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneItem : MonoBehaviour
{
    [Header("Configuración")]
    public int mapIndex = 1;
    public int cost = 100;
    public string zoneName = "Parque Central";
    public string zoneBenefit = "Infección x1"; // Texto para el multiplicador

    [Header("Referencias UI")]
    public Button myButton;
    public Image planetImage; // Arrastra aquí la imagen del planeta
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI benefitText; // Texto UI para el beneficio

    [Header("Colores de Estado")]
    public Color luzColor = Color.white; // Brilla (seleccionable o comprable)
    public Color apagadoColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Oscuro (bloqueado y sin dinero)

    private bool isUnlocked = false;

    void OnEnable()
    {
        // 1. La zona inicial siempre libre
        if (mapIndex == 0)
        {
            isUnlocked = true;
        }
        else
        {
            // Miramos si la zona está marcada como comprada en el disco
            bool yaCompradoEnPrefs = PlayerPrefs.GetInt("ZoneUnlocked_" + mapIndex, 0) == 1;

            // Miramos si el jugador compró la habilidad permanente en el árbol
            bool tieneHabilidadMeta = Guardado.instance != null && Guardado.instance.keepZonesUnlocked;

            if (tieneHabilidadMeta)
            {
                isUnlocked = yaCompradoEnPrefs;
            }
            else
            {
                isUnlocked = yaCompradoEnPrefs;
            }
        }
        UpdateUI();
    }

    // FUNCIÓN CORREGIDA: Ahora todas las rutas devuelven un valor
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

            // Refrescar todos los botones de la escena para que cambien las luces
            ZoneItem[] todosLosScripts = Object.FindObjectsByType<ZoneItem>(FindObjectsSortMode.None);
            foreach (ZoneItem item in todosLosScripts)
            {
                item.UpdateUI();
            }
        }
    }

    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    public bool CanAfford()
    {
        if (LevelManager.instance == null) return false;
        return LevelManager.instance.contagionCoins >= GetFinalCost();
    }

    public void UpdateUI()
    {
        if (LevelManager.instance == null) return;

        int currentEquipped = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        bool esElEquipado = (currentEquipped == mapIndex);

        int currentPrice = GetFinalCost();
        bool puedeComprarlo = LevelManager.instance.contagionCoins >= currentPrice;

        // Actualizar textos
        if (nameText != null) nameText.text = zoneName;
        if (benefitText != null) benefitText.text = zoneBenefit;

        if (isUnlocked)
        {
            if (esElEquipado)
            {
                // EQUIPADO: Se ve oscuro para indicar que ya está puesto
                statusText.text = "ACTIVO";
                myButton.interactable = false;
                if (planetImage != null) planetImage.color = apagadoColor;
            }
            else
            {
                // DESBLOQUEADO PERO NO EQUIPADO: Brilla para que lo elijas
                statusText.text = "EQUIPAR";
                myButton.interactable = true;
                if (planetImage != null) planetImage.color = luzColor;
            }
        }
        else
        {
            // BLOQUEADO: Brilla solo si tienes el dinero para comprarlo
            statusText.text = "COMPRAR (" + currentPrice + ")";
            myButton.interactable = puedeComprarlo;

            if (planetImage != null)
            {
                planetImage.color = puedeComprarlo ? luzColor : apagadoColor;
            }
        }
    }
}