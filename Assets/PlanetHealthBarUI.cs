using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanetHealthBarUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image fillImage;
    public TextMeshProUGUI percentText;
    
    private Vector2 tamañoOriginal; // Guardar tamaño original del prefab
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            tamañoOriginal = rect.sizeDelta; // Guardar el tamaño original
        }
    }

    private void OnEnable()
    {
        // Restaurar el tamaño original cuando se active
        if (rect != null)
        {
            rect.sizeDelta = tamañoOriginal;
        }
    }

    public void Setup(string nombre, float porcentaje)
    {
        nameText.text = nombre;

        fillImage.fillAmount = porcentaje;

        int percentInt = Mathf.RoundToInt(porcentaje * 100f);
        percentText.text = percentInt + "%";
        
        // Asegurar que el tamaño se restaura después de Setup
        if (rect != null)
        {
            rect.sizeDelta = tamañoOriginal;
        }
    }
}