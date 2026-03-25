using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BotonConfirmarZonaOrbital : MonoBehaviour
{
    [Header("Referencias UI")]
    public OrbitaSistemaUI orbitaSistema;
    public Button confirmarButton;
    public TextMeshProUGUI textoBoton;
    public GameObject zonePanel;
    public GameObject dayOverPanel;// El panel que queremos cerrar

    // Cooldown para prevenir doble clic
    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.3f; // 300ms

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
        // Chequear cooldown para prevenir doble clic
        if (Time.time - lastClickTime < CLICK_COOLDOWN)
            return;

        lastClickTime = Time.time;

        if (orbitaSistema == null) return;

        RectTransform planeta = orbitaSistema.GetPlanetaAlFrente();
        if (planeta == null) return;

        ZoneItem zone = planeta.GetComponent<ZoneItem>();
        if (zone == null) return;

        if (!zone.IsUnlocked())
        {
            zone.OnClickButton();
            ActualizarEstado();
            return;
        }

        bool isSameMap = zone.mapIndex == PlayerPrefs.GetInt("CurrentMapIndex", 0);

        if (isSameMap)
        {
            LevelManager.instance.ResumeSession();
        }
        else
        {
            LevelManager.instance.ActivateMap(zone.mapIndex);
            LevelManager.instance.StartSession();
        }

        if (zonePanel != null)
            zonePanel.SetActive(false);

        if (dayOverPanel != null)
            dayOverPanel.SetActive(false);
    }

}