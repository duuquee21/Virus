using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// 🎮 AÑADIDO: ISelectHandler y IDeselectHandler para que escuche al mando
public class CambioColorTexto : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Referencias")]
    public TextMeshProUGUI texto;

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorHover = Color.green;

    void Awake()
    {
        // Si no asignaste el texto en el inspector, intenta buscarlo en este objeto
        if (texto == null)
        {
            texto = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Establecer el color inicial
        if (texto != null)
        {
            texto.color = colorNormal;
        }
    }

    // 🖱️ --- RATÓN ---
    // Se ejecuta al pasar el ratón por encima
    public void OnPointerEnter(PointerEventData eventData)
    {
        CambiarColor(colorHover);
    }

    // Se ejecuta al quitar el ratón
    public void OnPointerExit(PointerEventData eventData)
    {
        CambiarColor(colorNormal);
    }

    // 🎮 --- MANDO ---
    // Se ejecuta cuando el cursor del mando selecciona este objeto
    public void OnSelect(BaseEventData eventData)
    {
        CambiarColor(colorHover);
    }

    // Se ejecuta cuando el cursor del mando se va a otro objeto
    public void OnDeselect(BaseEventData eventData)
    {
        CambiarColor(colorNormal);
    }

    // --- FUNCIÓN INTERNA ---
    private void CambiarColor(Color nuevoColor)
    {
        if (texto != null)
        {
            texto.color = nuevoColor;
        }
    }

    // Opcional: Para asegurar que el color vuelva al normal si el objeto se desactiva
    private void OnDisable()
    {
        if (texto != null)
        {
            texto.color = colorNormal;
        }
    }
}