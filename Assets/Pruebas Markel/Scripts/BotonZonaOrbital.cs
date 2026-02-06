using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BotonConfirmarZonaOrbital : MonoBehaviour
{
    [Header("Referencias")]
    public OrbitaSistemaUI orbitaSistema; 
    public Button confirmarButton;        
    public TextMeshProUGUI textoBoton;    

    void Update()
    {
        ActualizarEstado();
    }

    
    void ActualizarEstado()
    {
        if (orbitaSistema == null) return;

        RectTransform planeta = orbitaSistema.GetPlanetaAlFrente();
        if (planeta == null) return;

        
        ZoneItem zone = planeta.GetComponent<ZoneItem>();

        if (zone == null)
        {
            confirmarButton.interactable = false;
            textoBoton.text = "ERROR";
            return;
        }

        
        if (!zone.IsUnlocked())
        {
            bool puedeComprar = zone.CanAfford();
            textoBoton.text = "COMPRAR"; 
            confirmarButton.interactable = puedeComprar; 
        }
        
        else
        {
            textoBoton.text = "JUGAR";
            confirmarButton.interactable = true;
        }
    }

    
    public void OnClickConfirmar()
    {
        if (orbitaSistema == null) return;

        RectTransform planeta = orbitaSistema.GetPlanetaAlFrente();
        if (planeta == null) return;

        ZoneItem zone = planeta.GetComponent<ZoneItem>();
        if (zone == null) return;

        
        if (!zone.IsUnlocked())
        {
            
            zone.OnClickButton(); 
            
           
            ActualizarEstado(); 
        }
        else
        {
            
            
            
            LevelManager.instance.ActivateMap(zone.mapIndex);

            
            LevelManager.instance.StartSession();
        }
    }
}