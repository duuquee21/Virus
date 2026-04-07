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
        Image buttonImage = GetComponent<Image>();

        // Esto te avisará en la consola si la textura no es legible
        if (buttonImage.sprite != null && buttonImage.sprite.texture != null)
        {
            try
            {
                buttonImage.sprite.texture.GetPixel(0, 0);
            }
            catch (System.Exception)
            {
                Debug.LogError("¡Error! La textura '" + buttonImage.sprite.name + "' no tiene activado 'Read/Write Enabled'.");
            }
        }

        buttonImage.alphaHitTestMinimumThreshold = alphaThreshold;
    }
}