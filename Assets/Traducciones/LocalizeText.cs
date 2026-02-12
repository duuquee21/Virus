using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class LocalizeText : MonoBehaviour
{
    public string clave; // Aquí escribirás "btn_jugar", etc.
    private TextMeshProUGUI textoTMP;

    void Start()
    {
        textoTMP = GetComponent<TextMeshProUGUI>();
        
        // Nos suscribimos al evento de cambio de idioma
        if (LocalizationManager.instance != null)
        {
            LocalizationManager.instance.OnLanguageChanged += ActualizarTraduccion;
            ActualizarTraduccion();
        }
    }

    void OnDestroy()
    {
        if (LocalizationManager.instance != null)
        {
            LocalizationManager.instance.OnLanguageChanged -= ActualizarTraduccion;
        }
    }

    public void ActualizarTraduccion()
    {
        if (textoTMP != null && LocalizationManager.instance != null)
        {
            textoTMP.text = LocalizationManager.instance.GetTexto(clave);
        }
    }
}