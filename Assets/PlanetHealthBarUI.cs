using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanetHealthBarUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image fillImage;
    public TextMeshProUGUI percentText;

    [Tooltip("Duración de la animación cuando la barra pasa de llena a la salud real.")]
    public float animationDuration = 0.8f;

    private Vector2 tamañoOriginal; // Guardar tamaño original del prefab
    private RectTransform rect;
    private Coroutine animCoroutine;

    // Posición y escala original del texto de porcentaje (para que siga a la barra sin deformarse)
    private Vector2 percentTextOriginalAnchoredPos;
    private Vector3 percentTextOriginalScale;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            tamañoOriginal = rect.sizeDelta; // Guardar el tamaño original
        }

        if (percentText != null)
        {
            percentTextOriginalAnchoredPos = percentText.rectTransform.anchoredPosition;
            percentTextOriginalScale = percentText.rectTransform.localScale;
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
        porcentaje = Mathf.Clamp01(porcentaje);

        // Reiniciar animación previa si existiera
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        // Asegurar que el tamaño se restablece después de Setup
        if (rect != null)
        {
            rect.sizeDelta = tamañoOriginal;
        }

        // Iniciamos la animación desde llena (1) hacia el porcentaje real
        animCoroutine = StartCoroutine(AnimateFillFromFull(porcentaje));
    }

    private IEnumerator AnimateFillFromFull(float target)
    {
        float start = 1f;
        float elapsed = 0f;

        fillImage.fillAmount = start;
        UpdatePercentText(start);

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Empieza rápido y termina más suave (ease-out)
            float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
            float value = Mathf.Lerp(start, target, easeOut);

            fillImage.fillAmount = value;
            UpdatePercentText(value);
            yield return null;
        }

        fillImage.fillAmount = target;
        UpdatePercentText(target);
        animCoroutine = null;
    }

    private void UpdatePercentText(float fill)
    {
        int percentInt = Mathf.RoundToInt(fill * 100f);
        percentText.text = percentInt + "%";

        // Hacer que el número siga al relleno de la barra (solo para Fill-method horizontal/vertical)
        if (percentText != null && fillImage != null && percentText.rectTransform != null && fillImage.rectTransform != null)
        {
            RectTransform fillRect = fillImage.rectTransform;
            Vector2 anchored = percentTextOriginalAnchoredPos;

            float width = fillRect.rect.width;
            float height = fillRect.rect.height;
            float padding = 10f;

            // Calcular el desplazamiento para que el texto quede junto al extremo derecho del relleno
            if (fillImage.type == Image.Type.Filled && fillImage.fillMethod == Image.FillMethod.Horizontal)
            {
                float x = -width / 2f + fill * width + padding;
                anchored.x = Mathf.Clamp(x, -width / 2f + padding, width / 2f - padding);
            }
            else if (fillImage.type == Image.Type.Filled && fillImage.fillMethod == Image.FillMethod.Vertical)
            {
                float y = -height / 2f + fill * height + padding;
                anchored.y = Mathf.Clamp(y, -height / 2f + padding, height / 2f - padding);
            }
            else
            {
                // Si no es Fill, se coloca a la derecha del porcentaje (modo general)
                float x = -width / 2f + fill * width + padding;
                anchored.x = Mathf.Clamp(x, -width / 2f + padding, width / 2f - padding);
            }

            percentText.rectTransform.anchoredPosition = anchored;
        }
    }
}