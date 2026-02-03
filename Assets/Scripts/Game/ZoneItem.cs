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
    public TextMeshProUGUI statusText; 
    public TextMeshProUGUI nameText;   

    private bool isUnlocked = false;

    
    void OnEnable()
    {
      

        if (mapIndex == 0) isUnlocked = true;
        else isUnlocked = PlayerPrefs.GetInt("ZoneUnlocked_" + mapIndex, 0) == 1;

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

    void Update()
    {
        UpdateUI();
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
        int misMonedas = LevelManager.instance.contagionCoins;
        Debug.Log("¡COMPRANDO! Precio: " + finalPrice + " | Monedas antes: " + LevelManager.instance.contagionCoins);

        if (misMonedas >= finalPrice)
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
        }
    }

    void UpdateUI()
    {
        if (LevelManager.instance == null) return;

        int currentEquipped = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        int currentPrice = GetFinalCost(); 
        int misMonedas = LevelManager.instance.contagionCoins; 
        string bonusText = "";
        
        if (mapIndex == 1) bonusText = " (x2 Oro)";
        if (mapIndex == 2) bonusText = " (x3 Oro)";
        if (isUnlocked)
            
        {
            if (currentEquipped == mapIndex)
            {
                statusText.text = "EQUIPADO";
                myButton.interactable = false; 
                if(nameText) nameText.text = zoneName + bonusText;
            }
            else
            {
                statusText.text = "SELECCIONAR";
                myButton.interactable = true;
                if(nameText) nameText.text = zoneName + bonusText + " (" + currentPrice + ")";
            }
            if(nameText) nameText.text = zoneName; 
        }
        else
        {
            statusText.text = "COMPRAR";
            if(nameText) nameText.text = zoneName + " (" + currentPrice + ")" + " (" + bonusText + ")"; 

            // COMPARAMOS CON LAS MONEDAS REALES
            bool canAfford = misMonedas >= currentPrice; 
            myButton.interactable = canAfford;
        }
    }
}