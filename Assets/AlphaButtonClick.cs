using UnityEngine;
using UnityEngine.UI;

// Esto asegura que el GameObject tenga un componente Image
[RequireComponent(typeof(Image))]
public class AlphaButtonClick : MonoBehaviour
{
    [Tooltip("El valor de opacidad mínimo para registrar el clic (De 0 a 1)")]
    [Range(0f, 1f)]
    public float alphaThreshold = 0.1f;

    void Start()
    {
        // Obtenemos el componente Image asociado a este botón
        Image buttonImage = GetComponent<Image>();

        // Le asignamos el umbral mínimo de alpha
        buttonImage.alphaHitTestMinimumThreshold = alphaThreshold;
    }
}