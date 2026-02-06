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
    public TextMeshProUGUI stockText; // Arrastra un texto nuevo aquí en el Inspector

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

            // --- NUEVA LÓGICA ---
            // Buscamos el PopulationManager y le asignamos el prefab correspondiente al mapIndex
            PopulationManager popManager = Object.FindFirstObjectByType<PopulationManager>();
            if (popManager != null)
            {
                // Aquí asumo que: 
                // Si mapIndex es 0 (Zona 1) usa prefab 0
                // Si mapIndex es 1 (Zona 2) usa prefab 1... etc.
                popManager.SetZonePrefab(mapIndex);
            }

            // Refrescar todos los botones
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

        // --- BLOQUE: MOSTRAR STOCK ---
        if (stockText != null)
        {
            int restantes = LevelManager.instance.GetStockRestante(mapIndex);
            stockText.text = "Shinies: " + restantes;
            stockText.color = (restantes > 0) ? Color.yellow : Color.red;
        }

        // --- BLOQUE CORREGIDO: BENEFICIO DINÁMICO ---
        // Si mapIndex es 0 (Zona 1) -> x1
        // Si mapIndex es 1 (Zona 2) -> x2
        // Si mapIndex es 2 (Zona 3) -> x3
        int multiplicadorCalculado = mapIndex + 1;
        zoneBenefit = "Infección x" + multiplicadorCalculado;

        if (benefitText != null)
        {
            benefitText.text = zoneBenefit;
            // Opcional: Si es la zona más alta (x3), pon el texto en un color especial
            benefitText.color = (multiplicadorCalculado >= 3) ? new Color(1f, 0.8f, 0f) : Color.white;
        }

        // Actualizar nombre
        if (nameText != null) nameText.text = zoneName;

        // --- LÓGICA DE ESTADOS (DESBLOQUEO/EQUIPADO) ---
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