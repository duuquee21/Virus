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

        // Si la zona no está desbloqueada, el botón sirve para COMPRAR
        if (!zone.IsUnlocked())
        {
            bool puedeComprar = zone.CanAfford();
            textoBoton.text = "COMPRAR";
            confirmarButton.interactable = puedeComprar;
        }
        // Si ya está desbloqueada, el botón sirve para JUGAR
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
            // Llama a la compra en ZoneItem (que ya limpiamos de Shinies)
            zone.OnClickButton();
            ActualizarEstado();
        }
        else
        {
            // Activa el mapa seleccionado
            LevelManager.instance.ActivateMap(zone.mapIndex);

            // Inicia la sesión de juego normal
            LevelManager.instance.StartSession();
        }
    }
}