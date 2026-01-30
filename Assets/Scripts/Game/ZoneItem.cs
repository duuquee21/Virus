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

    void Start()
    {
        
        if (mapIndex == 0) isUnlocked = true;
        else isUnlocked = PlayerPrefs.GetInt("ZoneUnlocked_" + mapIndex, 0) == 1;

        if (nameText != null) nameText.text = zoneName + " (" + cost + ")";
        
        UpdateUI();
    }

    void Update()
    {
        
        UpdateUI();
    }

    public void OnClickButton()
    {
        if (isUnlocked)
        {
            // SI YA LO TENGO -> LO EQUIPO
            EquipZone();
        }
        else
        {
            // SI NO LO TENGO -> INTENTO COMPRARLO
            TryBuyZone();
        }
    }

    void TryBuyZone()
    {
        if (Guardado.instance == null) return;

        // Verificamos si tiene suficiente gente infectada TOTAL
        if (Guardado.instance.totalInfected >= cost)
        {
            // 1. Restamos el precio (Gastar gente)
            // Nota: AddTotalData suma, así que pasamos el costo en negativo para restar
            Guardado.instance.AddTotalData(-cost); 

            // 2. Desbloqueamos
            isUnlocked = true;
            PlayerPrefs.SetInt("ZoneUnlocked_" + mapIndex, 1);
            PlayerPrefs.Save();

            // 3. Equipamos automáticamente al comprar
            EquipZone();
        }
        else
        {
            Debug.Log("¡No tienes suficientes infectados!");
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
        int currentEquipped = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        if (isUnlocked)
        {
            if (currentEquipped == mapIndex)
            {
                statusText.text = "EQUIPADO";
                myButton.interactable = false; // Ya lo tienes puesto
            }
            else
            {
                statusText.text = "SELECCIONAR";
                myButton.interactable = true;
            }
            // Si ya es tuyo, quitamos el precio del nombre
            if(nameText) nameText.text = zoneName; 
        }
        else
        {
            statusText.text = "COMPRAR";
            
            // Solo se ilumina si tienes dinero
            bool canAfford = Guardado.instance.totalInfected >= cost;
            myButton.interactable = canAfford;
        }
    }
}