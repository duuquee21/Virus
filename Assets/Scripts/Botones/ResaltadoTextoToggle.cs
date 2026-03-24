using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ResaltadoTextoToggle : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public TextMeshProUGUI textoAsociado;

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorIluminado = Color.yellow; // Color cuando el mando/ratón está encima

    void OnEnable()
    {
        if (textoAsociado != null)
        {
            textoAsociado.color = colorNormal;
        }
    }

    // 🎮 --- MANDO ---
    // Cuando el cursor del mando "pisa" este Toggle
    public void OnSelect(BaseEventData eventData)
    {
        CambiarColor(colorIluminado);
    }

    // Cuando el cursor del mando se va a otro lado
    public void OnDeselect(BaseEventData eventData)
    {
        CambiarColor(colorNormal);
    }

    // 🖱️ --- RATÓN ---
    // Cuando la flecha del ratón entra
    public void OnPointerEnter(PointerEventData eventData)
    {
        CambiarColor(colorIluminado);
    }

    // Cuando la flecha del ratón sale
    public void OnPointerExit(PointerEventData eventData)
    {
        CambiarColor(colorNormal);
    }

    // --- FUNCIÓN INTERNA ---
    private void CambiarColor(Color nuevoColor)
    {
        if (textoAsociado != null)
        {
            textoAsociado.color = nuevoColor;
        }
    }
}