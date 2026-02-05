using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneItem : MonoBehaviour
{
    [Header("Configuración")]
    public int mapIndex = 1;
    public int cost = 100;
    public string zoneName = "Parque Central";

    [Header("Referencias UI")]
    public Button myButton;
    public Image planetImage; // Arrastra aquí la imagen del planeta
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI nameText;

    [Header("Colores de Estado")]
    public Color luzColor = Color.white; // Brilla (seleccionable)
    public Color apagadoColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Oscuro (bloqueado/activo)

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
                // SI TIENE LA HABILIDAD: Simplemente cargamos lo que haya en el disco
                isUnlocked = yaCompradoEnPrefs;
            }
            else
            {
                // SI NO TIENE LA HABILIDAD: 
                // Solo se mantiene desbloqueado si ya lo compramos Y el LevelManager 
                // no ha borrado los datos todavía (es decir, seguimos en la misma partida).
                isUnlocked = yaCompradoEnPrefs;

                // OJO: Aquí NO debes poner PlayerPrefs.SetInt(..., 0) porque si lo haces,
                // borrarás la compra que el jugador hizo hace 1 minuto en la misma partida.
            }
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

            // Refrescar todos los botones de la escena para que cambien las luces
            ZoneItem[] todosLosScripts = Object.FindObjectsByType<ZoneItem>(FindObjectsSortMode.None);
            foreach (ZoneItem item in todosLosScripts)
            {
                item.UpdateUI();
            }
        }
    }

    // Estas funciones permiten que otros scripts (como BotonZonaOrbital) 
    // consulten el estado de este planeta.

    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    public bool CanAfford()
    {
        if (LevelManager.instance == null) return false;

        // Compara las monedas actuales con el coste (con o sin descuento)
        return LevelManager.instance.contagionCoins >= GetFinalCost();
    }

    public void UpdateUI()
    {
        if (LevelManager.instance == null) return;

        int currentEquipped = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        bool esElEquipado = (currentEquipped == mapIndex);

        if (isUnlocked)
        {
            if (esElEquipado)
            {
                // EQUIPADO: Sin luz, no se puede pulsar
                statusText.text = "ACTIVO";
                myButton.interactable = false;
                if (planetImage != null) planetImage.color = apagadoColor;
            }
            else
            {
                // COMPRADO: Con luz, se puede seleccionar
                statusText.text = "EQUIPAR";
                myButton.interactable = true;
                if (planetImage != null) planetImage.color = luzColor;
            }
        }
        else
        {
            // BLOQUEADO: Oscuro
            int currentPrice = GetFinalCost();
            statusText.text = "COMPRAR (" + currentPrice + ")";
            myButton.interactable = (LevelManager.instance.contagionCoins >= currentPrice);
            if (planetImage != null) planetImage.color = apagadoColor;
        }

        if (nameText != null) nameText.text = zoneName;
    }
}