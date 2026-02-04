using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BotonConfirmarZonaOrbital : MonoBehaviour
{
    public OrbitaSistemaUI orbitaSistema;
    public Button confirmarButton;
    public TextMeshProUGUI textoBoton;

    void Update()
    {
        ActualizarEstado();
    }

    void ActualizarEstado()
    {
        RectTransform planeta = orbitaSistema.GetPlanetaAlFrente();
        ZoneItem zone = planeta.GetComponent<ZoneItem>();

        if (zone == null)
        {
            confirmarButton.interactable = false;
            return;
        }

        // Zona NO comprada
        if (!zone.IsUnlocked())
        {
            bool puedeComprar = zone.CanAfford();

            textoBoton.text = "COMPRAR";
            confirmarButton.interactable = puedeComprar;
        }
        // Zona comprada
        else
        {
            textoBoton.text = "JUGAR";
            confirmarButton.interactable = true;
        }
    }

    public void OnClickConfirmar()
    {
        RectTransform planeta = orbitaSistema.GetPlanetaAlFrente();
        ZoneItem zone = planeta.GetComponent<ZoneItem>();

        if (zone == null) return;

        // Si no está comprada → comprar
        if (!zone.IsUnlocked())
        {
            zone.OnClickButton();   // Compra
        }
        else
        {
            // Selecciona zona
            LevelManager.instance.ActivateMap(zone.mapIndex);

            // Inicia partida
            LevelManager.instance.StartSession();
        }
    }
}
