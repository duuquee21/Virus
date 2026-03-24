using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CambioColorTexto : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    // Se ejecuta al pasar el ratón por encima
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (texto != null)
        {
            texto.color = colorHover;
        }
    }

    // Se ejecuta al quitar el ratón
    public void OnPointerExit(PointerEventData eventData)
    {
        if (texto != null)
        {
            texto.color = colorNormal;
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